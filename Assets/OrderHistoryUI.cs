using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI-контроллер панели "История заказов" в гараже.
///
/// НАСТРОЙКА В UNITY:
///   1. openHistoryButton — кнопка в гараже, открывающая панель истории.
///   2. historyPanel — сама панель (GameObject), показывается/скрывается.
///   3. closeButton — кнопка закрытия (опционально).
///   4. contentParent — Content контейнера ScrollView, куда добавляются строки истории.
///   5. historyRowPrefab — префаб строки. Может быть как просто текстовый объект
///      (тогда используется simpleTextOnly режим), так и содержать компонент
///      OrderHistoryRowUI для более тонкой настройки — оба варианта поддерживаются.
/// </summary>
public class OrderHistoryUI : MonoBehaviour
{
    [Header("Открытие / закрытие панели")]
    public Button openHistoryButton;
    public GameObject historyPanel;
    public Button closeButton;

    [Header("Список истории")]
    public Transform contentParent;
    public GameObject historyRowPrefab;

    [Tooltip("Если в historyRowPrefab нет компонента OrderHistoryRowUI, скрипт попробует " +
             "найти TextMeshProUGUI прямо на префабе и просто выставить туда текст записи.")]
    public bool fallbackToPlainText = true;

    private readonly List<GameObject> spawnedRows = new List<GameObject>();
    private bool isPanelOpen = false;

    private void Start()
    {
        if (openHistoryButton != null)
            openHistoryButton.onClick.AddListener(OpenPanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (historyPanel != null)
            historyPanel.SetActive(false);

        if (OrderHistoryManager.Instance != null)
            OrderHistoryManager.Instance.OnHistoryChanged += RefreshList;
    }

    private void OnDestroy()
    {
        if (OrderHistoryManager.Instance != null)
            OrderHistoryManager.Instance.OnHistoryChanged -= RefreshList;

        if (isPanelOpen)
            FindObjectOfType<CarCameraController>()?.SetCameraRotationBlocked(false);
    }

    public void OpenPanel()
    {
        if (historyPanel != null)
            historyPanel.SetActive(true);

        if (!isPanelOpen)
        {
            isPanelOpen = true;
            FindObjectOfType<CarCameraController>()?.SetCameraRotationBlocked(true);
        }

        RefreshList();
    }

    public void ClosePanel()
    {
        if (historyPanel != null)
            historyPanel.SetActive(false);

        if (isPanelOpen)
        {
            isPanelOpen = false;
            FindObjectOfType<CarCameraController>()?.SetCameraRotationBlocked(false);
        }
    }

    public void RefreshList()
    {
        ClearRows();

        if (OrderHistoryManager.Instance == null || contentParent == null || historyRowPrefab == null)
            return;

        List<OrderHistoryEntry> entries = OrderHistoryManager.Instance.GetHistory();

        foreach (var entry in entries)
        {
            GameObject rowObj = Instantiate(historyRowPrefab, contentParent);
            spawnedRows.Add(rowObj);

            OrderHistoryRowUI row = rowObj.GetComponent<OrderHistoryRowUI>();
            if (row != null)
            {
                row.Setup(entry);
            }
            else if (fallbackToPlainText)
            {
                var text = rowObj.GetComponentInChildren<TextMeshProUGUI>();
                if (text != null)
                {
                    text.text = entry.ToDisplayString();
                }
            }
        }
    }

    private void ClearRows()
    {
        foreach (var row in spawnedRows)
        {
            if (row != null) Destroy(row);
        }
        spawnedRows.Clear();
    }
}
