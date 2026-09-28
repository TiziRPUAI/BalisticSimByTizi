using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class TrajectoryPreview : MonoBehaviour
{
    [SerializeField] private CannonController cannon;
    [Tooltip("Capas que cortan la línea: Ground y Structure.")]
    [SerializeField] private LayerMask hitMask;
    [SerializeField, Min(0.01f)] private float timeStep = 0.05f;
    [SerializeField, Min(2)] private int maxPoints = 160;

    private LineRenderer line;
    private Vector3[] buffer;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
        line.useWorldSpace = true;
        buffer = new Vector3[maxPoints];
    }

    private void LateUpdate()
    {
        SimulationManager sim = SimulationManager.Instance;
        bool visible = sim != null && sim.CanFire;
        line.enabled = visible;
        if (!visible) return;

        Vector3 origin = cannon.MuzzlePosition;
        Vector3 velocity = cannon.GetLaunchVelocity();
        Vector3 gravity = Physics.gravity;

        int count = 0;
        buffer[count++] = origin;

        while (count < maxPoints)
        {
            float t = count * timeStep;

            // p(t) = p0 + v0·t + ½·g·t²  (misma trayectoria que la física, sin rozamiento del aire)
            Vector3 next = origin + velocity * t + 0.5f * gravity * t * t;

            if (Physics.Linecast(buffer[count - 1], next, out RaycastHit hit, hitMask, QueryTriggerInteraction.Ignore))
            {
                buffer[count++] = hit.point;
                break;
            }

            buffer[count++] = next;
        }

        line.positionCount = count;
        for (int i = 0; i < count; i++) line.SetPosition(i, buffer[i]);
    }
}