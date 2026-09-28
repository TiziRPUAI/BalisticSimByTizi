using UnityEngine;

/// <summary>
/// Escucha CannonController.ShotFired para saber cuándo empezar a perseguir al proyectil, y
/// SimulationManager.ShotFinished para saber cuándo volver a la vista general. No modifica ni
/// referencia la física del proyectil, solo lee su Transform.
/// </summary>
public class CameraDirector : MonoBehaviour
{
    [Header("Cámaras")]
    [SerializeField] private Camera overviewCamera;
    [SerializeField] private Camera chaseCamera;
    [SerializeField] private CannonController cannon;

    [Header("Seguimiento")]
    [SerializeField] private Vector3 chaseOffset = new Vector3(0f, 3f, -8f);
    [SerializeField, Range(1f, 20f)] private float followSpeed = 8f;
    [SerializeField, Range(1f, 20f)] private float lookSpeed = 10f;

    private Transform target;

    private void Awake() => SetChaseActive(false);

    private void Start()
    {
        // Se suscribe en Start (no en Awake/OnEnable) para garantizar que CannonController y
        // SimulationManager.Instance ya terminaron su propio Awake.
        if (cannon != null) cannon.ShotFired += OnShotFired;
        if (SimulationManager.Instance != null) SimulationManager.Instance.ShotFinished += OnShotFinished;
    }

    private void OnDestroy()
    {
        if (cannon != null) cannon.ShotFired -= OnShotFired;
        if (SimulationManager.Instance != null) SimulationManager.Instance.ShotFinished -= OnShotFinished;
    }

    private void OnShotFired(Transform projectile)
    {
        target = projectile;
        SetChaseActive(true);

        // Arranca ya pegada al proyectil, sin el "vuelo" del lerp desde la posición anterior.
        chaseCamera.transform.position = target.position + chaseOffset;
    }

    private void OnShotFinished(ShotReport _)
    {
        target = null;
        SetChaseActive(false);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + chaseOffset;
        chaseCamera.transform.position = Vector3.Lerp(
            chaseCamera.transform.position, desiredPosition, followSpeed * Time.deltaTime);

        Quaternion desiredRotation = Quaternion.LookRotation(target.position - chaseCamera.transform.position, Vector3.up);
        chaseCamera.transform.rotation = Quaternion.Slerp(
            chaseCamera.transform.rotation, desiredRotation, lookSpeed * Time.deltaTime);
    }

    private void SetChaseActive(bool active)
    {
        chaseCamera.gameObject.SetActive(active);
        overviewCamera.gameObject.SetActive(!active);
    }
}
