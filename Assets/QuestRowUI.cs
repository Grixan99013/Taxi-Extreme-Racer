using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Компонент одной строки задания. Вешается на префаб questRowPrefab.
///
/// НАСТРОЙКА В UNITY (поля можно оставить пустыми, если конкретный элемент не нужен в дизайне):
///   titleText        — название задания
///   descriptionText  — описание
///   progressText     — текст вида "3 / 5"
///   progressBar      — Slider/Image (type=Filled), показывающий прогресс визуально
///   rewardText       — текст вида "+50$  +10 EXP"
///   iconImage        — иконка задания (QuestData.icon)
///   claimButton      — кнопка "Получить", видна только когда задание выполнено (ReadyToClaim)
///   claimedLabel      — необязательная подпись "Получено" (на практике почти не используется,
///                        т.к. QuestListUI убирает выполненные неповторяемые задания из списка целиком)
/// </summary>
public class QuestRowUI : MonoBehaviour
{
    [Header("Текстовые поля")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI progressText;
    public TextMeshProUGUI rewardText;
    public Image iconImage;

    [Header("Прогресс")]
    [Tooltip("Slider, у которого будет выставляться value от 0 до 1 в соответствии с прогрессом.")]
    public Slider progressBar;

    [Header("Кнопка получения награды")]
    public Button claimButton;
    [Tooltip("Необязательно: подпись, которая показывается вместо кнопки, когда награда уже получена.")]
    public GameObject claimedLabel;

    private QuestData questData;
    private QuestProgress questProgress;

    public void Setup(QuestData data, QuestProgress progress)
    {
        questData = data;
        questProgress = progress;

        if (titleText != null) titleText.text = data.title;
        if (descriptionText != null) descriptionText.text = data.description;
        if (iconImage != null && data.icon != null) iconImage.sprite = data.icon;

        if (rewardText != null)
            rewardText.text = $"+{data.moneyReward}$  +{data.expReward} EXP";

        if (progressText != null)
            progressText.text = $"{progress.currentValue} / {data.targetValue}";

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = 1f;
            progressBar.value = data.targetValue > 0
                ? (float)progress.currentValue / data.targetValue
                : 0f;
        }

        UpdateClaimButtonState();

        if (claimButton != null)
        {
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(OnClaimClicked);
        }
    }

    /// <summary>
    /// Кнопка "Получить" видна только когда условие задания выполнено (ReadyToClaim).
    /// Пока прогресс не достиг цели (InProgress) — кнопка полностью скрыта, а не просто неактивна.
    /// Claimed сюда обычно не попадает: QuestListUI отфильтровывает такие задания ещё до Setup(),
    /// но проверка оставлена на случай прямого использования этого компонента.
    /// </summary>
    private void UpdateClaimButtonState()
    {
        bool readyToClaim = questProgress.status == QuestStatus.ReadyToClaim;
        bool alreadyClaimed = questProgress.status == QuestStatus.Claimed;

        if (claimButton != null)
        {
            claimButton.gameObject.SetActive(readyToClaim);
            claimButton.interactable = readyToClaim;
        }

        if (claimedLabel != null)
        {
            claimedLabel.SetActive(alreadyClaimed);
        }
    }

    private void OnClaimClicked()
    {
        if (QuestManager.Instance == null || questData == null) return;

        bool success = QuestManager.Instance.ClaimReward(questData);
        if (success)
        {
            // QuestManager сам разошлёт OnQuestsUpdated, что заставит QuestListUI
            // перестроить весь список — эта строка будет пересоздана с актуальным статусом.
            UpdateClaimButtonState();
        }
    }
}
