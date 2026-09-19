using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum SimulationState { Initializing, Ready, Flying, Settling, Finished }

[System.Serializable]
public struct ShotParameters
{
    public float angleDeg;
    public float force;
    public float mass;
    public float launchSpeed;
}

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance { get; private set; }

    [Header("Asentamiento")]
    [SerializeField, Min(0f)] private float minSettleTime = 1.5f;
    [SerializeField, Min(0.1f)] private float maxSettleTime = 8f;

    [Header("Puntuación")]
    [SerializeField, Min(1f)] private float referenceEnergy = 2000f;
    [SerializeField, Min(0.01f)] private float minEfficiency = 0.5f;
    [SerializeField, Min(0.01f)] private float maxEfficiency = 2f;

    [Header("UI")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private GameObject reportPanel;
    [SerializeField] private TMP_Text reportText;
    [SerializeField] private Button retryButton;

    private StructureTarget[] pieces = new StructureTarget[0];
    private ShotParameters shot;
    private ImpactData impact;
    private bool hasImpact;

    public SimulationState State { get; private set; } = SimulationState.Initializing;
    public bool CanFire => State == SimulationState.Ready;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        reportPanel.SetActive(false);
        retryButton.onClick.AddListener(Restart);
    }

    private IEnumerator Start()
    {
        pieces = FindObjectsByType<StructureTarget>(FindObjectsSortMode.None);
        foreach (StructureTarget piece in pieces) piece.KnockedDown += OnPieceKnockedDown;

        SetState(SimulationState.Initializing);
        yield return new WaitUntil(AllPiecesArmed);
        SetState(SimulationState.Ready);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (retryButton != null) retryButton.onClick.RemoveListener(Restart);

        foreach (StructureTarget piece in pieces)
        {
            if (piece != null) piece.KnockedDown -= OnPieceKnockedDown;
        }
    }

    public void BeginShot(ShotParameters parameters)
    {
        shot = parameters;
        hasImpact = false;
        SetState(SimulationState.Flying);
    }

    public void ReportImpact(ImpactData data, Rigidbody projectileBody)
    {
        if (State != SimulationState.Flying) return;

        impact = data;
        hasImpact = true;
        SetState(SimulationState.Settling);
        StartCoroutine(SettleRoutine(projectileBody));
    }

    public void ReportMiss()
    {
        if (State != SimulationState.Flying) return;

        hasImpact = false;
        FinishShot();
    }

    private IEnumerator SettleRoutine(Rigidbody projectileBody)
    {
        float elapsed = 0f;
        while (elapsed < maxSettleTime)
        {
            elapsed += Time.deltaTime;
            if (elapsed >= minSettleTime && IsEverythingAtRest(projectileBody)) break;
            yield return null;
        }

        FinishShot();
    }

    private bool IsEverythingAtRest(Rigidbody projectileBody)
    {
        if (projectileBody != null && !projectileBody.IsSleeping()) return false;

        foreach (StructureTarget piece in pieces)
        {
            if (!piece.IsAtRest) return false;
        }
        return true;
    }

    private bool AllPiecesArmed()
    {
        foreach (StructureTarget piece in pieces)
        {
            if (!piece.IsArmed) return false;
        }
        return true;
    }

    private void FinishShot()
    {
        SetState(SimulationState.Finished);
        CountKnocked(out int knocked, out int damagePoints);

        // E = ½·m·v0²  (energía gastada en el disparo)
        float launchEnergy = 0.5f * shot.mass * shot.launchSpeed * shot.launchSpeed;

        // Menos energía que la de referencia → multiplicador > 1 (acotado)
        float efficiency = Mathf.Clamp(referenceEnergy / launchEnergy, minEfficiency, maxEfficiency);
        int score = Mathf.RoundToInt(damagePoints * efficiency);

        reportText.text = BuildReport(knocked, damagePoints, launchEnergy, efficiency, score);
        reportPanel.SetActive(true);
    }

    private string BuildReport(int knocked, int damagePoints, float energy, float efficiency, int score)
    {
        int total = pieces.Length;
        float percent = total > 0 ? 100f * knocked / total : 0f;

        StringBuilder sb = new StringBuilder();
        sb.AppendLine("<b>REPORTE DE TIRO</b>");
        sb.AppendLine();

        sb.AppendLine("<b>Disparo</b>");
        sb.AppendLine($"Ángulo: {shot.angleDeg:F1}°  |  Impulso: {shot.force:F0} N·s  |  Masa: {shot.mass:F1} kg");
        sb.AppendLine($"Velocidad inicial: {shot.launchSpeed:F1} m/s  |  Energía: {energy:F0} J");
        sb.AppendLine();

        sb.AppendLine("<b>Telemetría de impacto</b>");
        if (hasImpact)
        {
            sb.AppendLine($"Objeto impactado: {impact.hitObjectName} ({(impact.hitStructure ? "estructura" : "suelo/otro")})");
            sb.AppendLine($"Tiempo de vuelo: {impact.flightTime:F2} s");
            sb.AppendLine($"Punto de impacto: {impact.point:F1}");
            sb.AppendLine($"Distancia horizontal: {impact.horizontalDistance:F1} m");
            sb.AppendLine($"Velocidad relativa: {impact.relativeSpeed:F1} m/s");
            sb.AppendLine($"Impulso de colisión: {impact.impulse:F1} N·s");
        }
        else
        {
            sb.AppendLine("Sin impacto (proyectil fuera de límites o tiempo excedido).");
        }
        sb.AppendLine();

        sb.AppendLine("<b>Daño</b>");
        sb.AppendLine($"Piezas derribadas: {knocked}/{total} ({percent:F0}%)");
        sb.AppendLine($"Puntos por daño: {damagePoints}");
        sb.AppendLine();

        sb.AppendLine("<b>Puntuación</b>");
        sb.AppendLine($"Multiplicador de eficiencia: x{efficiency:F2}");
        sb.Append($"<size=130%><b>TOTAL: {score}</b></size>");
        return sb.ToString();
    }

    private void CountKnocked(out int knocked, out int points)
    {
        knocked = 0;
        points = 0;
        foreach (StructureTarget piece in pieces)
        {
            if (!piece.IsKnockedDown) continue;
            knocked++;
            points += piece.PointValue;
        }
    }

    private void OnPieceKnockedDown(StructureTarget _) => RefreshStatus();

    private void SetState(SimulationState newState)
    {
        State = newState;
        RefreshStatus();
    }

    private void RefreshStatus()
    {
        if (statusText == null) return;

        string label = State switch
        {
            SimulationState.Initializing => "Estabilizando estructura...",
            SimulationState.Ready => "Listo para disparar",
            SimulationState.Flying => "Proyectil en vuelo",
            SimulationState.Settling => "Esperando asentamiento...",
            _ => "Intento finalizado"
        };

        CountKnocked(out int knocked, out _);
        statusText.text = $"{label}  |  Derribadas: {knocked}/{pieces.Length}";
    }

    private void Restart() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
}
