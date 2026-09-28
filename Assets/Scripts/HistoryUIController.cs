using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HistoryUIController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject historyPanel;
    [SerializeField] private Button openHistoryButton;
    [SerializeField] private Button closeHistoryButton;
    [SerializeField] private GameObject loadingIndicator;
    [SerializeField] private TMP_Text emptyStateText;

    [Header("Lista")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private SimulationRecordItemUI itemPrefab;

    private readonly List<SimulationRecordItemUI> spawnedItems = new List<SimulationRecordItemUI>();

    private void Awake()
    {
        historyPanel.SetActive(false);
        loadingIndicator.SetActive(false);
        if (emptyStateText != null) emptyStateText.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        openHistoryButton.onClick.AddListener(OpenHistory);
        closeHistoryButton.onClick.AddListener(CloseHistory);
    }

    private void OnDisable()
    {
        openHistoryButton.onClick.RemoveListener(OpenHistory);
        closeHistoryButton.onClick.RemoveListener(CloseHistory);
    }

    private async void OpenHistory()
    {
        historyPanel.SetActive(true);
        loadingIndicator.SetActive(true);
        if (emptyStateText != null) emptyStateText.gameObject.SetActive(false);
        ClearItems();

        List<SimulationRecord> history = UGSServiceManager.Instance != null
            ? await UGSServiceManager.Instance.LoadHistoryAsync()
            : new List<SimulationRecord>();

        // El panel pudo cerrarse mientras esperábamos la respuesta de red.
        if (this == null || !historyPanel.activeSelf) return;

        loadingIndicator.SetActive(false);
        PopulateList(history);
    }

    private void CloseHistory() => historyPanel.SetActive(false);

    private void PopulateList(List<SimulationRecord> history)
    {
        if (emptyStateText != null) emptyStateText.gameObject.SetActive(history.Count == 0);

        // El más reciente primero.
        for (int i = history.Count - 1; i >= 0; i--)
        {
            SimulationRecordItemUI item = Instantiate(itemPrefab, contentParent);
            item.Bind(history[i]);
            spawnedItems.Add(item);
        }
    }

    private void ClearItems()
    {
        foreach (SimulationRecordItemUI item in spawnedItems)
        {
            if (item != null) Destroy(item.gameObject);
        }
        spawnedItems.Clear();
    }
}
