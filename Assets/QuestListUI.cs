using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// UI-контроллер панели "Задания" в гараже.
///
/// НАСТРОЙКА В UNITY:
///   1. openQuestsButton — кнопка в гараже, открывающая панель заданий.
///   2. questsPanel — сама панель (GameObject), которая показывается/скрывается.
///   3. closeButton — кнопка закрытия панели (опционально).
///   4. contentParent — Transform контейнера ScrollView -> Viewport -> Content,
///      куда будут добавляться строки заданий.
///   5. questRowPrefab — префаб одной строки задания. Должен содержать на себе
///      компонент QuestRowUI (см. ниже) — этот скрипт сам найдёт его через GetComponent.
///
/// Список перерисовывается каждый раз при открытии панели и при любом
/// изменении прогресса (подписка на QuestManager.OnQuestsUpdated), поэтому
/// открытая панель всегда показывает актуальные данные.
/// </summary>
public class QuestListUI : MonoBehaviour
{
    [Header("Открытие / закрытие панели")]
    public Button openQuestsButton;
    public GameObject questsPanel;
    public Button closeButton;

    [Header("Список заданий")]
    public Transform contentParent;
    public GameObject questRowPrefab;

    private readonly List<GameObject> spawnedRows = new List<GameObject>();
    private bool isPanelOpen = false;

    private void Start()
    {
        if (openQuestsButton != null)
            openQuestsButton.onClick.AddListener(OpenPanel);

        if (closeButton != null)
            closeButton.onClick.AddListener(ClosePanel);

        if (questsPanel != null)
            questsPanel.SetActive(false);

        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestsUpdated += RefreshList;
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
            QuestManager.Instance.OnQuestsUpdated -= RefreshList;

        if (isPanelOpen)
            FindObjectOfType<CarCameraController>()?.SetCameraRotationBlocked(false);
    }

    public void OpenPanel()
    {
        if (questsPanel != null)
            questsPanel.SetActive(true);

        if (!isPanelOpen)
        {
            isPanelOpen = true;
            FindObjectOfType<CarCameraController>()?.SetCameraRotationBlocked(true);
        }

        RefreshList();
    }

    public void ClosePanel()
    {
        if (questsPanel != null)
            questsPanel.SetActive(false);

        if (isPanelOpen)
        {
            isPanelOpen = false;
            FindObjectOfType<CarCameraController>()?.SetCameraRotationBlocked(false);
        }
    }

    /// <summary>
    /// Перестраивает список заданий с нуля на основе актуальных данных QuestManager.
    /// Задания со статусом Claimed (награда уже получена, неповторяемые) в список не попадают —
    /// для повторяемых заданий это не актуально, т.к. ClaimReward сбрасывает их обратно в InProgress.
    /// </summary>
    public void RefreshList()
    {
        ClearRows();

        if (QuestManager.Instance == null || contentParent == null || questRowPrefab == null)
            return;

        var quests = QuestManager.Instance.GetAllQuestsWithProgress();

        foreach (var (data, progress) in quests)
        {
            if (progress.status == QuestStatus.Claimed)
                continue;

            GameObject rowObj = Instantiate(questRowPrefab, contentParent);
            spawnedRows.Add(rowObj);

            QuestRowUI row = rowObj.GetComponent<QuestRowUI>();
            if (row != null)
            {
                row.Setup(data, progress);
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
