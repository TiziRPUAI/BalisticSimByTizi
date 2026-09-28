using UnityEngine;

/// <summary>
/// Traduce cada ShotReport de SimulationManager a un SimulationRecord y lo persiste en UGS.
/// Es el único componente que conecta la física (SimulationManager) con la red (UGSServiceManager),
/// y lo hace a través de un evento de C#, no de una referencia directa entre ambos.
/// </summary>
public class CloudSaveBridge : MonoBehaviour
{
    private void Start()
    {
        // Se suscribe en Start (no en Awake/OnEnable) para garantizar que SimulationManager.Instance
        // ya quedó asignado en su propio Awake, sin depender del orden de ejecución de scripts.
        if (SimulationManager.Instance != null)
            SimulationManager.Instance.ShotFinished += OnShotFinished;
        else
            Debug.LogWarning("[CloudSaveBridge] No se encontró un SimulationManager en la escena.");
    }

    private void OnDestroy()
    {
        if (SimulationManager.Instance != null)
            SimulationManager.Instance.ShotFinished -= OnShotFinished;
    }

    private async void OnShotFinished(ShotReport report)
    {
        if (UGSServiceManager.Instance == null)
        {
            Debug.LogWarning("[CloudSaveBridge] No se encontró un UGSServiceManager; el disparo no se guardó.");
            return;
        }

        SimulationRecord record = new SimulationRecord(report);
        await UGSServiceManager.Instance.AddToHistoryAsync(record);
    }
}
