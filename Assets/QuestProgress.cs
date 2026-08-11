using System;

/// <summary>
/// Статус задания с точки зрения игрока.
/// </summary>
public enum QuestStatus
{
    /// <summary>Прогресс ещё не достиг цели.</summary>
    InProgress,

    /// <summary>Условие выполнено, но награда ещё не забрана игроком.</summary>
    ReadyToClaim,

    /// <summary>Награда уже получена (для неповторяемых заданий — финальное состояние).</summary>
    Claimed
}

/// <summary>
/// Рантайм-обёртка над QuestData: хранит текущий прогресс и статус конкретного задания.
/// Один QuestProgress всегда соответствует одному QuestData по questId.
/// </summary>
[Serializable]
public class QuestProgress
{
    public string questId;
    public int currentValue;
    public QuestStatus status;

    public QuestProgress(string questId)
    {
        this.questId = questId;
        this.currentValue = 0;
        this.status = QuestStatus.InProgress;
    }
}
