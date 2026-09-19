using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class StructureTarget : MonoBehaviour
{
    [Header("Estabilidad inicial")]
    [SerializeField, Min(0f)] private float armDelay = 1f;
    [SerializeField] private bool startAsleep = true;
    [SerializeField, Min(1)] private int solverIterations = 20;
    [SerializeField, Min(1)] private int solverVelocityIterations = 8;
    [SerializeField, Min(0.1f)] private float maxDepenetrationVelocity = 1f;

    [Header("Límites de rotura (se aplican tras armDelay)")]
    [SerializeField, Min(0f)] private float breakForce = 2000f;
    [SerializeField, Min(0f)] private float breakTorque = 2000f;

    [Header("Criterios de derribo")]
    [SerializeField, Min(0f)] private float displacementThreshold = 0.5f;
    [SerializeField, Range(1f, 180f)] private float tiltThreshold = 35f;
    [SerializeField] private float fallLimitY = -20f;
    [SerializeField, Min(0)] private int pointValue = 100;
    [SerializeField, Min(0f)] private float impactBreakImpulse = 40f;
    [SerializeField, Range(0f, 1f)] private float impulseTransmission = 0.8f;
    private Rigidbody body;
    private Joint[] joints;
    private Vector3 baselinePosition;
    private Quaternion baselineRotation;

    public bool IsArmed { get; private set; }
    public bool IsKnockedDown { get; private set; }
    public int PointValue => pointValue;
    public bool IsAtRest => body.IsSleeping() || body.position.y < fallLimitY;

    public event System.Action<StructureTarget> KnockedDown;

    private void Awake()
    {
        body = GetComponent<Rigidbody>();
        joints = GetComponents<Joint>();

        body.solverIterations = solverIterations;
        body.solverVelocityIterations = solverVelocityIterations;
        body.maxDepenetrationVelocity = maxDepenetrationVelocity;

        foreach (Joint joint in joints)
        {
            joint.breakForce = Mathf.Infinity;
            joint.breakTorque = Mathf.Infinity;
        }
    }

    private IEnumerator Start()
    {
        if (startAsleep) body.Sleep();

        yield return new WaitForSeconds(armDelay);

        foreach (Joint joint in joints)
        {
            if (joint == null) continue;
            joint.breakForce = breakForce;
            joint.breakTorque = breakTorque;
        }

        baselinePosition = body.position;
        baselineRotation = body.rotation;
        IsArmed = true;
    }

    private void FixedUpdate()
    {
        if (!IsArmed || IsKnockedDown) return;

        bool displaced = (body.position - baselinePosition).sqrMagnitude > displacementThreshold * displacementThreshold;
        bool tilted = Quaternion.Angle(body.rotation, baselineRotation) > tiltThreshold;

        if (displaced || tilted) MarkKnockedDown();
    }

    private void OnJointBreak(float force) => MarkKnockedDown();

    private void OnCollisionEnter(Collision collision) => ReceiveImpulse(collision.impulse.magnitude);

    public void ReceiveImpulse(float impulse)
    {
        if (!IsArmed || IsKnockedDown || impulse < impactBreakImpulse) return;

        MarkKnockedDown();

        // Cada pieza rota pasa a su vecino de abajo una fracción del impulso recibido
        float transmitted = impulse * impulseTransmission;
        foreach (Joint joint in joints)
        {
            if (joint == null) continue;
            if (joint.connectedBody != null && joint.connectedBody.TryGetComponent(out StructureTarget neighbor))
                neighbor.ReceiveImpulse(transmitted);
            Destroy(joint);
        }
    }

    private void MarkKnockedDown()
    {
        if (IsKnockedDown) return;
        IsKnockedDown = true;
        KnockedDown?.Invoke(this);
    }
}
