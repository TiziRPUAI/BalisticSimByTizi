using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CannonController : MonoBehaviour
{
    [Header("Escena")]
    [SerializeField] private Transform barrelPivot;
    [SerializeField] private Transform muzzle;
    [SerializeField] private GameObject projectilePrefab;

    [Header("UI - Controles")]
    [SerializeField] private Slider angleSlider;
    [SerializeField] private Slider forceSlider;
    [SerializeField] private Slider massSlider;
    [SerializeField] private Button fireButton;

    [Header("UI - Etiquetas")]
    [SerializeField] private TMP_Text angleLabel;
    [SerializeField] private TMP_Text forceLabel;
    [SerializeField] private TMP_Text massLabel;
    [SerializeField] private TMP_Text launchSpeedLabel;

    [Header("Rangos y valores iniciales")]
    [SerializeField] private Vector2 angleRange = new Vector2(5f, 85f);
    [SerializeField] private Vector2 forceRange = new Vector2(20f, 400f);
    [SerializeField] private Vector2 massRange = new Vector2(1f, 20f);
    [SerializeField] private float initialAngle = 35f;
    [SerializeField] private float initialForce = 150f;
    [SerializeField] private float initialMass = 5f;

    private void Awake()
    {
        ConfigureSlider(angleSlider, angleRange, initialAngle);
        ConfigureSlider(forceSlider, forceRange, initialForce);
        ConfigureSlider(massSlider, massRange, initialMass);
        RefreshView();
    }

    private void OnEnable()
    {
        angleSlider.onValueChanged.AddListener(OnSliderChanged);
        forceSlider.onValueChanged.AddListener(OnSliderChanged);
        massSlider.onValueChanged.AddListener(OnSliderChanged);
        fireButton.onClick.AddListener(Fire);
    }

    private void OnDisable()
    {
        angleSlider.onValueChanged.RemoveListener(OnSliderChanged);
        forceSlider.onValueChanged.RemoveListener(OnSliderChanged);
        massSlider.onValueChanged.RemoveListener(OnSliderChanged);
        fireButton.onClick.RemoveListener(Fire);
    }

    private void Update()
    {
        SimulationManager sim = SimulationManager.Instance;
        fireButton.interactable = sim != null && sim.CanFire;
    }

    public void Fire()
    {
        SimulationManager sim = SimulationManager.Instance;
        if (sim == null || !sim.CanFire) return;

        float angle = angleSlider.value;
        float force = forceSlider.value;
        float mass = massSlider.value;
        Vector3 direction = GetLaunchDirection(angle);

        GameObject instance = Instantiate(projectilePrefab, muzzle.position, Quaternion.LookRotation(direction));
        if (!instance.TryGetComponent(out Rigidbody body) || !instance.TryGetComponent(out ProjectileTelemetry telemetry))
        {
            Debug.LogError("El prefab del proyectil necesita Rigidbody, Collider y ProjectileTelemetry.", projectilePrefab);
            Destroy(instance);
            return;
        }

        ShotParameters shot = new ShotParameters
        {
            angleDeg = angle,
            force = force,
            mass = mass,
            launchSpeed = force / mass
        };

        body.mass = mass;
        body.AddForce(direction * force, ForceMode.Impulse);
        telemetry.Initialize(muzzle.position);
        sim.BeginShot(shot);
    }

    private Vector3 GetLaunchDirection(float angleDeg)
    {
        Vector3 flatForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        float radians = angleDeg * Mathf.Deg2Rad;

        // d = cos(θ)·horizontal + sin(θ)·up  → vector unitario (horizontal ⟂ up)
        return flatForward * Mathf.Cos(radians) + Vector3.up * Mathf.Sin(radians);
    }

    private void OnSliderChanged(float _) => RefreshView();

    private void RefreshView()
    {
        float angle = angleSlider.value;
        float force = forceSlider.value;
        float mass = massSlider.value;

        angleLabel.text = $"Ángulo: {angle:F1}°";
        forceLabel.text = $"Fuerza (impulso): {force:F0} N·s";
        massLabel.text = $"Masa: {mass:F1} kg";

        // ForceMode.Impulse → Δv = J / m
        launchSpeedLabel.text = $"Velocidad inicial: {force / mass:F1} m/s";

        barrelPivot.localRotation = Quaternion.Euler(-angle, 0f, 0f);
    }

    private static void ConfigureSlider(Slider slider, Vector2 range, float initial)
    {
        slider.wholeNumbers = true;
        slider.minValue = range.x;
        slider.maxValue = range.y;
        slider.SetValueWithoutNotify(Mathf.Clamp(initial, range.x, range.y));
    }
}
