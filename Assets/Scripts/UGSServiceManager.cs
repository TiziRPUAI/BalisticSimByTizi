using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

/// <summary>
/// Único punto de contacto con UGS. Nadie más en el proyecto referencia Unity.Services.*:
/// eso es lo que mantiene la física y la UI desacopladas de la nube.
/// </summary>
public class UGSServiceManager : MonoBehaviour
{
    // Guardar TODO el historial bajo una sola clave evita superar el límite de claves de
    // Cloud Save si se juegan muchos intentos.
    private const string HistoryKey = "ShotHistory";

    public static UGSServiceManager Instance { get; private set; }

    private Task initializeTask;

    public bool IsSignedIn => AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        initializeTask = InitializeAsync();
    }

    public async Task InitializeAsync()
    {
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();

            // SignInAnonymouslyAsync reutiliza el token cacheado en PlayerPrefs si ya existe,
            // así que el mismo jugador conserva su historial entre sesiones de Play.
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            Debug.Log($"[UGS] Autenticado. PlayerId: {AuthenticationService.Instance.PlayerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] Error al inicializar/autenticar: {e.Message}");
        }
    }

    /// <summary>
    /// Descarga el historial actual, agrega el registro nuevo y sube la lista completa a la
    /// misma clave, para no sobreescribir ni perder los disparos anteriores.
    /// </summary>
    public async Task AddToHistoryAsync(SimulationRecord record)
    {
        await EnsureReadyAsync();

        try
        {
            SimulationHistoryData history = await LoadHistoryInternalAsync();
            history.records.Add(record);

            var payload = new Dictionary<string, object> { { HistoryKey, JsonUtility.ToJson(history) } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(payload);

            Debug.Log($"[UGS] Historial guardado. Total de disparos: {history.records.Count}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] Error al guardar el historial: {e.Message}");
        }
    }

    public async Task<List<SimulationRecord>> LoadHistoryAsync()
    {
        await EnsureReadyAsync();

        SimulationHistoryData history = await LoadHistoryInternalAsync();
        history.records.Sort((a, b) => string.CompareOrdinal(a.timestampUtc, b.timestampUtc));
        return history.records;
    }

    private async Task<SimulationHistoryData> LoadHistoryInternalAsync()
    {
        try
        {
            var keys = new HashSet<string> { HistoryKey };
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (result.TryGetValue(HistoryKey, out var item))
            {
                string json = item.Value.GetAs<string>();
                SimulationHistoryData parsed = JsonUtility.FromJson<SimulationHistoryData>(json);
                if (parsed != null) return parsed;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] Error al leer el historial: {e.Message}");
        }

        return new SimulationHistoryData();
    }

    private Task EnsureReadyAsync() => initializeTask ??= InitializeAsync();
}