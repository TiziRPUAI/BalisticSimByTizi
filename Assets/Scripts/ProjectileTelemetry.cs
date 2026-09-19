using UnityEngine;

[System.Serializable]
public struct ImpactData
{
    public float flightTime;
    public Vector3 point;
    public float relativeSpeed;
    public float impulse;
    public float horizontalDistance;
    public string hitObjectName;
    public bool hitStructure;
}

[RequireComponent(typeof(Rigidbody), typeof(Collider))]
public class ProjectileTelemetry : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maxFlightTime = 20f;
    [SerializeField] private float killHeight = -30f;

    private Rigidbody body;
    private Vector3 launchOrigin;
    private float launchTime;
    private bool initialized;
    private bool hasImpacted;

    private void Awake() => body = GetComponent<Rigidbody>();

    public void Initialize(Vector3 origin)
    {
        launchOrigin = origin;
        launchTime = Time.fixedTime;
        initialized = true;
    }

    private void FixedUpdate()
    {
        if (!initialized) return;

        bool timedOut = !hasImpacted && Time.fixedTime - launchTime > maxFlightTime;
        if (!timedOut && body.position.y > killHeight) return;

        if (!hasImpacted)
        {
            hasImpacted = true;
            SimulationManager.Instance.ReportMiss();
        }
        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!initialized || hasImpacted) return;
        hasImpacted = true;

        ContactPoint contact = collision.GetContact(0);

        Vector3 flatDelta = contact.point - launchOrigin;
        flatDelta.y = 0f;

        ImpactData data = new ImpactData
        {
            flightTime = Time.fixedTime - launchTime,
            point = contact.point,
            relativeSpeed = collision.relativeVelocity.magnitude,
            impulse = collision.impulse.magnitude,
            horizontalDistance = flatDelta.magnitude,
            hitObjectName = collision.gameObject.name,
            hitStructure = collision.collider.GetComponentInParent<StructureTarget>() != null
        };

        SimulationManager.Instance.ReportImpact(data, body);
    }
}
