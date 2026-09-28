using System;
using System.Globalization;
using TMPro;
using UnityEngine;

/// <summary>
/// Va en el prefab de la tarjeta del historial. HistoryUIController llama a Bind()
/// una vez por instancia, con el registro que le toca mostrar.
/// </summary>
public class SimulationRecordItemUI : MonoBehaviour
{
    [SerializeField] private TMP_Text dateText;
    [SerializeField] private TMP_Text shotText;
    [SerializeField] private TMP_Text resultText;

    public void Bind(SimulationRecord record)
    {
        if (DateTime.TryParse(record.timestampUtc, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out DateTime utc))
        {
            dateText.text = $"{utc.ToLocalTime():dd/MM HH:mm}  |  Azimut {record.azimuthDeg:+0;-0;0}°";
        }
        else
        {
            dateText.text = record.timestampUtc;
        }

        shotText.text = $"Ángulo {record.angleDeg:F0}°  |  {record.force:F0} N·s  |  {record.mass:F1} kg";

        string outcome = record.isHit ? "Impacto" : "Fallo";
        resultText.text = $"{outcome}  |  {record.horizontalDistance:F1} m  |  {record.affectedObjectsCount} piezas";
    }
}