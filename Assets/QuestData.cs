using UnityEngine;

/// <summary>
/// Тип условия выполнения задания.
/// Каждый тип соответствует своему счётчику прогресса в QuestManager.
/// </summary>
public enum QuestConditionType
{
    /// <summary>Довезти N пассажиров (любых, накопительно за всю игру).</summary>
    DeliverPassengers,

    /// <summary>Получить оценку >= targetValue хотя бы за ОДИН заказ.</summary>
    SingleOrderRating,

    /// <summary>Получить оценку >= ratingThreshold за N заказов (накопительно, считает только "идеальные" заказы).</summary>
    HighRatedOrdersCount,

    /// <summary>Заработать N денег суммарно за всю игру (накопительно, не сбрасывается).</summary>
    EarnTotalMoney,

    /// <summary>Заработать N денег за одну смену (сбрасывается в начале каждой смены).</summary>
    EarnMoneyPerShift,

    /// <summary>Завершить N смен (таймер дошёл до конца).</summary>
    CompleteShifts,
}

/// <summary>
/// Уровень сложности задания. Влияет только на отображение — реальная награда
/// задаётся явно через expReward/moneyReward, чтобы дизайнер мог точно
/// настроить баланс, но сложность помогает быстро сортировать/подсвечивать в UI.
/// </summary>
public enum QuestDifficulty
{
    Easy,
    Medium,
    Hard,
    Epic
}

/// <summary>
/// Описание одного задания (квеста). Создаётся как ассет:
/// Project window -> Create -> TaxiGame -> Quest
///
/// Примеры из ТЗ:
///   - "Довези 5 пассажиров"        -> DeliverPassengers,    targetValue = 5
///   - "Получи 5 звёзд за 1 заказ"  -> SingleOrderRating,     targetValue = 5
///   - "Получи 5 звёзд 10 раз"      -> HighRatedOrdersCount,  targetValue = 10, ratingThreshold = 5
///   - "Заработай 500 за смену"     -> EarnMoneyPerShift,     targetValue = 500
/// </summary>
[CreateAssetMenu(fileName = "NewQuest", menuName = "TaxiGame/Quest", order = 1)]
public class QuestData : ScriptableObject
{
    [Header("Идентификация")]
    [Tooltip("Уникальный ID задания. Используется для сохранения прогресса — НЕ меняйте после релиза, иначе прогресс игроков потеряется.")]
    public string questId;

    [Header("Отображение")]
    public string title;
    [TextArea(2, 4)]
    public string description;
    public QuestDifficulty difficulty = QuestDifficulty.Easy;
    public Sprite icon;

    [Header("Условие выполнения")]
    public QuestConditionType conditionType;

    [Tooltip("Целевое значение: количество пассажиров/заказов/денег, в зависимости от conditionType.")]
    public int targetValue = 1;

    [Tooltip("Используется только для HighRatedOrdersCount и SingleOrderRating: минимальная оценка, которая считается успешной (обычно 5).")]
    [Range(1, 5)]
    public int ratingThreshold = 5;

    [Header("Награда")]
    public int expReward = 10;
    public int moneyReward = 50;

    [Header("Повторяемость")]
    [Tooltip("Если включено — после получения награды задание сбрасывается и может быть выполнено снова (прогресс начинается с нуля).")]
    public bool isRepeatable = false;
}
