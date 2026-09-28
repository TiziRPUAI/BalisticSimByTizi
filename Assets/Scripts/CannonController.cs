using System;
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
    [SerializeField] private Slider azimuthSlider;
    [SerializeField] private Slider forceSlider;
    [SerializeField] private Slider massSlider;
    [SerializeField] private Button fireButton;

    [Header("UI - Etiquetas")]
    [SerializeField] private TMP_Text angleLabel;
    [SerializeField] private TMP_Text azimuthLabel;
    [SerializeField] private TMP_Text forceLabel;
    [SerializeField] private TMP_Text massLabel;
    [SerializeField] private TMP_Text launchSpeedLabel;

    [Header("Rangos y valores iniciales")]
    [SerializeField] private Vector2 angleRange = new Vector2(5f, 85f);
    [SerializeField] private Vector2 azimuthRange = new Vector2(-60f, 60f);
    [SerializeField] private Vector2 forceRange = new Vector2(20f, 400f);
    [SerializeField] private Vector2 massRange = new Vector2(1f, 20f);
    [SerializeField] private float initialAngle = 35f;
    [SerializeField] private float initialAzimuth = 0f;
    [SerializeField] private float initialForce = 150f;
    [SerializeField] private float initialMass = 5f;

    private float baseYaw;

    private float AzimuthValue => azimuthSlider != null ? azimuthSlider.value : 0f;

    // Datos que necesita la línea de trayectoria.
    public Vector3 MuzzlePosition => muzzle.position;

    /// <summary>
    /// Se dispara justo después de instanciar el proyectil, con su Transform. CannonController
    /// no sabe quién lo escucha; por ejemplo, una cámara que lo persigue en vuelo.
    /// </summary>
    public event Action<Transform> ShotFired;

    // ForceMode.Impulse → Δv = J / m
    public Vector3 GetLaunchVelocity() =>
        GetLaunchDirection(angleSlider.value) * (forceSlider.value / massSlider.value);

    private void Awake()
    {
        baseYaw = transform.eulerAngles.y;

        ConfigureSlider(angleSlider, angleRange, initialAngle);
        if (azimuthSlider != null) ConfigureSlider(azimuthSlider, azimuthRange, initialAzimuth);
        ConfigureSlider(forceSlider, forceRange, initialForce);
        ConfigureSlider(massSlider, massRange, initialMass);
        RefreshView();
    }

    private void OnEnable()
    {
        angleSlider.onValueChanged.AddListener(OnSliderChanged);
        if (azimuthSlider != null) azimuthSlider.onValueChanged.AddListener(OnSliderChanged);
        forceSlider.onValueChanged.AddListener(OnSliderChanged);
        massSlider.onValueChanged.AddListener(OnSliderChanged);
        fireButton.onClick.AddListener(Fire);
    }

    private void OnDisable()
    {
        angleSlider.onValueChanged.RemoveListener(OnSliderChanged);
        if (azimuthSlider != null) azimuthSlider.onValueChanged.RemoveListener(OnSliderChanged);
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
            azimuthDeg = AzimuthValue,
            force = force,
            mass = mass,
            launchSpeed = force / mass
        };

        body.mass = mass;
        body.AddForce(direction * force, ForceMode.Impulse);
        telemetry.Initialize(muzzle.position);
        ShotFired?.Invoke(instance.transform);
        sim.BeginShot(shot);
    }

    private Vector3 GetLaunchDirection(float angleDeg)
    {
        // El azimut ya rota todo el cañón en Y, así que forward horizontal apunta al blanco elegido.
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

        // Azimut positivo = giro a la derecha (+X). Se suma al yaw original del cañón.
        float azimuth = AzimuthValue;
        if (azimuthLabel != null) azimuthLabel.text = $"Azimut: {azimuth:+0;-0;0}°";
        transform.rotation = Quaternion.Euler(0f, baseYaw + azimuth, 0f);
    }

    private static void ConfigureSlider(Slider slider, Vector2 range, float initial)
    {
        slider.wholeNumbers = true;
        slider.minValue = range.x;
        slider.maxValue = range.y;
        slider.SetValueWithoutNotify(Mathf.Clamp(initial, range.x, range.y));
    }
}