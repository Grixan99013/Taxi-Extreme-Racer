using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Обёртка для сериализации словаря прогресса через JsonUtility.
/// </summary>
[Serializable]
internal class QuestSaveData
{
    public List<QuestProgress> progressList = new List<QuestProgress>();

    /// <summary>"за всю игру" заработок, нужен для EarnTotalMoney (не путать с балансом, который можно тратить).</summary>
    public int lifetimeEarnings;
}

/// <summary>
/// Синглтон, управляющий всеми заданиями игры.
///
/// КАК ПОДКЛЮЧИТЬ НОВОЕ ЗАДАНИЕ:
///   1. Project window -> Create -> TaxiGame -> Quest, настроить поля.
///   2. Перетащить созданный ассет в массив allQuests на объекте QuestManager в сцене.
///   Больше ничего менять не нужно — прогресс посчитается автоматически по conditionType.
///
/// КАК ЭТО РАБОТАЕТ:
///   QuestManager подписывается на события CheckpointCounter (через статические C#-события,
///   см. вызовы ReportOrderDelivered / ReportShiftEnded) и обновляет currentValue
///   у всех заданий, чьё conditionType соответствует произошедшему событию.
///   Когда currentValue достигает targetValue, статус задания меняется на ReadyToClaim.
///   Деньги и опыт начисляются ТОЛЬКО при явном вызове ClaimReward(quest) —
///   то есть только когда игрок нажимает кнопку "Получить" в гараже.
/// </summary>
public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    private const string SAVE_KEY = "QuestProgressData";

    [Header("Список всех заданий игры")]
    [SerializeField] private List<QuestData> allQuests = new List<QuestData>();

    // questId -> прогресс
    private Dictionary<string, QuestProgress> progressById = new Dictionary<string, QuestProgress>();

    // Сколько денег заработано за текущую смену (сбрасывается при старте смены)
    private int currentShiftEarnings = 0;

    // Сколько денег заработано за всю игру (накопительно, не сбрасывается)
    private int lifetimeEarnings = 0;

    /// <summary>Вызывается при любом изменении прогресса любого задания — UI подписывается, чтобы обновить список.</summary>
    public event Action OnQuestsUpdated;

    /// <summary>Вызывается, когда задание становится готовым к получению награды (для всплывающих уведомлений в UI).</summary>
    public event Action<QuestData> OnQuestReadyToClaim;

    // -----------------------------------------------------------------------
    // Инициализация
    // -----------------------------------------------------------------------

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadProgress();
            EnsureProgressExistsForAllQuests();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        SaveProgress();
    }

    /// <summary>
    /// Гарантирует, что для каждого QuestData в allQuests есть запись прогресса.
    /// Нужно для случая, когда дизайнер добавил новое задание после того, как у игрока
    /// уже было сохранение — старое сохранение не должно "сломать" новый список.
    /// </summary>
    private void EnsureProgressExistsForAllQuests()
    {
        foreach (var quest in allQuests)
        {
            if (quest == null || string.IsNullOrEmpty(quest.questId)) continue;

            if (!progressById.ContainsKey(quest.questId))
            {
                progressById[quest.questId] = new QuestProgress(quest.questId);
            }
        }
    }

    // -----------------------------------------------------------------------
    // Публичный API для UI (гараж)
    // -----------------------------------------------------------------------

    /// <summary>Список всех заданий вместе с их текущим прогрессом — основной источник данных для UI гаража.</summary>
    public List<(QuestData data, QuestProgress progress)> GetAllQuestsWithProgress()
    {
        var result = new List<(QuestData, QuestProgress)>();
        foreach (var quest in allQuests)
        {
            if (quest == null || string.IsNullOrEmpty(quest.questId)) continue;
            result.Add((quest, GetOrCreateProgress(quest.questId)));
        }
        return result;
    }

    /// <summary>
    /// Забирает награду за выполненное задание. Вызывается по нажатию кнопки "Получить" в гараже.
    /// Возвращает true, если награда была успешно начислена.
    /// </summary>
    public bool ClaimReward(QuestData quest)
    {
        if (quest == null) return false;

        var progress = GetOrCreateProgress(quest.questId);
        if (progress.status != QuestStatus.ReadyToClaim) return false;

        PlayerManager.Instance?.AddBalance(quest.moneyReward);
        PlayerManager.Instance?.AddExp(quest.expReward);

        if (quest.isRepeatable)
        {
            // Повторяемое задание — сбрасываем прогресс, чтобы можно было выполнить снова
            progress.currentValue = 0;
            progress.status = QuestStatus.InProgress;
        }
        else
        {
            progress.status = QuestStatus.Claimed;
        }

        SaveProgress();
        OnQuestsUpdated?.Invoke();

        Debug.Log($"[QuestManager] Награда получена: '{quest.title}' -> +{quest.moneyReward}$, +{quest.expReward} EXP");
        return true;
    }

    // -----------------------------------------------------------------------
    // События игры — вызываются извне (CheckpointCounter, Timer)
    // -----------------------------------------------------------------------

    /// <summary>
    /// Вызывается при доставке пассажира. Обновляет все задания, связанные с заказами:
    /// DeliverPassengers, SingleOrderRating, HighRatedOrdersCount, EarnTotalMoney, EarnMoneyPerShift.
    /// </summary>
    public void ReportOrderDelivered(int rating, int money)
    {
        currentShiftEarnings += money;
        lifetimeEarnings += money;

        foreach (var quest in allQuests)
        {
            if (quest == null) continue;
            var progress = GetOrCreateProgress(quest.questId);
            if (progress.status != QuestStatus.InProgress) continue;

            switch (quest.conditionType)
            {
                case QuestConditionType.DeliverPassengers:
                    IncrementProgress(quest, progress, 1);
                    break;

                case QuestConditionType.SingleOrderRating:
                    // Разовое условие: если оценка за ЭТОТ заказ достаточна — выполняем сразу
                    if (rating >= quest.ratingThreshold)
                    {
                        SetProgress(quest, progress, quest.targetValue);
                    }
                    break;

                case QuestConditionType.HighRatedOrdersCount:
                    if (rating >= quest.ratingThreshold)
                    {
                        IncrementProgress(quest, progress, 1);
                    }
                    break;

                case QuestConditionType.EarnTotalMoney:
                    SetProgress(quest, progress, lifetimeEarnings);
                    break;

                case QuestConditionType.EarnMoneyPerShift:
                    SetProgress(quest, progress, currentShiftEarnings);
                    break;
            }
        }

        SaveProgress();
        OnQuestsUpdated?.Invoke();
    }

    /// <summary>
    /// Вызывается в начале новой смены (когда таймер запускается). Сбрасывает счётчик
    /// заработка за смену, чтобы EarnMoneyPerShift считался корректно с нуля.
    /// </summary>
    public void ReportShiftStarted()
    {
        currentShiftEarnings = 0;
    }

    /// <summary>
    /// Вызывается, когда смена завершается (таймер дошёл до нуля). Обновляет задания
    /// CompleteShifts и финально фиксирует EarnMoneyPerShift.
    /// </summary>
    public void ReportShiftEnded()
    {
        foreach (var quest in allQuests)
        {
            if (quest == null) continue;
            var progress = GetOrCreateProgress(quest.questId);
            if (progress.status != QuestStatus.InProgress) continue;

            if (quest.conditionType == QuestConditionType.CompleteShifts)
            {
                IncrementProgress(quest, progress, 1);
            }
            else if (quest.conditionType == QuestConditionType.EarnMoneyPerShift)
            {
                // Финальная фиксация на случай, если смена закончилась без события доставки после последнего заработка
                SetProgress(quest, progress, currentShiftEarnings);
            }
        }

        SaveProgress();
        OnQuestsUpdated?.Invoke();
    }

    // -----------------------------------------------------------------------
    // Внутренняя логика прогресса
    // -----------------------------------------------------------------------

    private void IncrementProgress(QuestData quest, QuestProgress progress, int amount)
    {
        SetProgress(quest, progress, progress.currentValue + amount);
    }

    private void SetProgress(QuestData quest, QuestProgress progress, int newValue)
    {
        progress.currentValue = Mathf.Clamp(newValue, 0, quest.targetValue);

        if (progress.currentValue >= quest.targetValue && progress.status == QuestStatus.InProgress)
        {
            progress.status = QuestStatus.ReadyToClaim;
            OnQuestReadyToClaim?.Invoke(quest);
        }
    }

    private QuestProgress GetOrCreateProgress(string questId)
    {
        if (!progressById.TryGetValue(questId, out var progress))
        {
            progress = new QuestProgress(questId);
            progressById[questId] = progress;
        }
        return progress;
    }

    // -----------------------------------------------------------------------
    // Сохранение / загрузка
    // -----------------------------------------------------------------------

    private void SaveProgress()
    {
        var saveData = new QuestSaveData
        {
            progressList = progressById.Values.ToList(),
            lifetimeEarnings = lifetimeEarnings
        };

        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadProgress()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

        string json = PlayerPrefs.GetString(SAVE_KEY);
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var saveData = JsonUtility.FromJson<QuestSaveData>(json);
            if (saveData?.progressList != null)
            {
                foreach (var progress in saveData.progressList)
                {
                    progressById[progress.questId] = progress;
                }
                lifetimeEarnings = saveData.lifetimeEarnings;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[QuestManager] Не удалось загрузить прогресс заданий: {e.Message}");
        }
    }

    /// <summary>Полный сброс прогресса всех заданий (для отладки/кнопки "Сбросить прогресс").</summary>
    public void ResetAllProgress()
    {
        progressById.Clear();
        lifetimeEarnings = 0;
        currentShiftEarnings = 0;
        EnsureProgressExistsForAllQuests();
        SaveProgress();
        OnQuestsUpdated?.Invoke();
    }
}
