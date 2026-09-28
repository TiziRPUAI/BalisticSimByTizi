using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.Core;
using UnityEngine;

/// <summary>
/// Único punto de contacto con UGS. Cada disparo se guarda en su propia clave ("Shot_0001",
/// "Shot_0002", ...) para que se vea individualmente en el Dashboard, más una clave "ShotCount"
/// que lleva la cuenta. Con el límite de 2000 claves por jugador de Cloud Save, esto es seguro
/// para cualquier cantidad razonable de disparos.
/// </summary>
public class UGSServiceManager : MonoBehaviour
{
    private const string CountKey = "ShotCount";
    private const string ShotKeyPrefix = "Shot_";

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
    /// Guarda el disparo en su propia clave ("Shot_000N") y actualiza el contador, en un
    /// único SaveAsync. No sobreescribe ni toca los disparos anteriores.
    /// </summary>
    public async Task AddToHistoryAsync(SimulationRecord record)
    {
        await EnsureReadyAsync();

        try
        {
            int nextIndex = await GetShotCountAsync() + 1;
            string key = BuildShotKey(nextIndex);

            var payload = new Dictionary<string, object>
            {
                { key, JsonUtility.ToJson(record) },
                { CountKey, nextIndex.ToString() }
            };
            await CloudSaveService.Instance.Data.Player.SaveAsync(payload);

            Debug.Log($"[UGS] Disparo guardado en '{key}'. Total de disparos: {nextIndex}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] Error al guardar el disparo: {e.Message}");
        }
    }

    /// <summary>
    /// Lee el contador y baja todas las claves Shot_0001..Shot_000N en una sola llamada.
    /// </summary>
    public async Task<List<SimulationRecord>> LoadHistoryAsync()
    {
        await EnsureReadyAsync();

        List<SimulationRecord> records = new List<SimulationRecord>();

        try
        {
            int count = await GetShotCountAsync();
            if (count == 0) return records;

            var keys = new HashSet<string>();
            for (int i = 1; i <= count; i++) keys.Add(BuildShotKey(i));

            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            foreach (var pair in result)
            {
                SimulationRecord record = JsonUtility.FromJson<SimulationRecord>(pair.Value.Value.GetAs<string>());
                if (record != null) records.Add(record);
            }

            records.Sort((a, b) => string.CompareOrdinal(a.timestampUtc, b.timestampUtc));
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] Error al leer el historial: {e.Message}");
        }

        return records;
    }

    private async Task<int> GetShotCountAsync()
    {
        try
        {
            var keys = new HashSet<string> { CountKey };
            var result = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

            if (result.TryGetValue(CountKey, out var item) &&
                int.TryParse(item.Value.GetAs<string>(), out int count))
            {
                return count;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[UGS] Error al leer el contador de disparos: {e.Message}");
        }

        return 0;
    }

    private static string BuildShotKey(int index) => $"{ShotKeyPrefix}{index:0000}";

    private Task EnsureReadyAsync() => initializeTask ??= InitializeAsync();
}