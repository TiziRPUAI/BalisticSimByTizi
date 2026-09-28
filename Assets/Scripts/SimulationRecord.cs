using System;
using System.Collections.Generic;

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
        force = report.shot.force;
        mass = report.shot.mass;
        isHit = report.hasImpact;
        horizontalDistance = report.horizontalDistance;
        affectedObjectsCount = report.knockedDown;
    }
}

/// <summary>
/// JsonUtility no serializa una lista en el nivel superior (JsonUtility.ToJson(list) falla en
/// silencio). Esta clase envoltorio (wrapper) es el objeto contenedor que sí puede serializar,
/// y es lo que se sube y baja completo bajo una única clave de Cloud Save.
/// </summary>
[Serializable]
public class SimulationHistoryData
{
    public List<SimulationRecord> records = new List<SimulationRecord>();
}
