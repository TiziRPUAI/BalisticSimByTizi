using System;

/// <summary>
/// Un disparo ya finalizado, listo para guardarse en UGS Cloud Save. Solo usa tipos simples:
/// JsonUtility no serializa DateTime de forma fiable, así que la fecha se guarda como texto ISO 8601.
/// </summary>
[Serializable]
public class SimulationRecord
{
    public string id;
    public string timestampUtc;
    public float angleDeg;
    public float azimuthDeg;
    public float force;
    public float mass;
    public bool isHit;
    public float horizontalDistance;
    public int affectedObjectsCount;

    // Constructor vacío requerido por JsonUtility al deserializar.
    public SimulationRecord() { }

    public SimulationRecord(ShotReport report)
    {
        id = Guid.NewGuid().ToString();
        timestampUtc = DateTime.UtcNow.ToString("o");
        angleDeg = report.shot.angleDeg;
        azimuthDeg = report.shot.azimuthDeg;
        force = report.shot.force;
        mass = report.shot.mass;
        isHit = report.hasImpact;
        horizontalDistance = report.horizontalDistance;
        affectedObjectsCount = report.knockedDown;
    }
}