using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Одна запись истории заказов.
/// [Serializable], чтобы JsonUtility мог сохранить список через PlayerPrefs.
/// </summary>
[Serializable]
public class OrderHistoryEntry
{
    public int rating;
    public int money;

    /// <summary>
    /// Явный флаг того, был ли заказ доставлен успешно.
    /// Хранится отдельным полем (а не выводится из rating==1 && money==0),
    /// чтобы текст в истории не зависел от случайного совпадения значений —
    /// например, если в будущем добавится штраф, дающий 0$ за успешный заказ.
    /// </summary>
    public bool wasSuccessful;

    /// <summary>Unix-время выполнения заказа (для сортировки/отображения даты при желании).</summary>
    public long timestampUnix;

    public OrderHistoryEntry(int rating, int money, bool wasSuccessful)
    {
        this.rating = rating;
        this.money = money;
        this.wasSuccessful = wasSuccessful;
        this.timestampUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
    }

    /// <summary>
    /// Текст в формате, который просил пользователь:
    /// успешный заказ  -> "Ты выполнил заказ, пассажир оценил на {оценка}, {деньги}$"
    /// проваленный заказ -> отдельная формулировка, без слова "выполнил"
    /// </summary>
    public string ToDisplayString()
    {
        if (wasSuccessful)
        {
            return $"Ты выполнил заказ, пассажир оценил на {rating}, {money}$";
        }

        return $"Ты провалил заказ, пассажир оценил поездку на {rating}";
    }
}

/// <summary>
/// Обёртка для сериализации списка через JsonUtility (он не умеет сериализовать
/// голый List<T> на верхнем уровне, нужен оборачивающий класс).
/// </summary>
[Serializable]
internal class OrderHistorySaveData
{
    public List<OrderHistoryEntry> entries = new List<OrderHistoryEntry>();
}

/// <summary>
/// Синглтон, хранящий историю выполненных заказов за всё время игры.
/// Переживает смену сцен (DontDestroyOnLoad), сохраняется в PlayerPrefs в формате JSON.
///
/// Использование:
///   OrderHistoryManager.Instance.AddEntry(rating, money);   // вызывается из CheckpointCounter при доставке
///   OrderHistoryManager.Instance.GetHistory();               // для отображения списка в гараже (новые сверху)
/// </summary>
public class OrderHistoryManager : MonoBehaviour
{
    public static OrderHistoryManager Instance { get; private set; }

    private const string SAVE_KEY = "OrderHistoryData";

    [Tooltip("Максимальное количество хранимых записей. Старые записи удаляются, чтобы PlayerPrefs не разрастался бесконечно.")]
    [SerializeField] private int maxEntries = 200;

    private List<OrderHistoryEntry> history = new List<OrderHistoryEntry>();

    /// <summary>Срабатывает при добавлении новой записи — UI может подписаться, чтобы обновиться "вживую".</summary>
    public event Action OnHistoryChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadHistory();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnApplicationQuit()
    {
        SaveHistory();
    }

    /// <summary>
    /// Добавляет запись о заказе и сохраняет историю.
    /// wasSuccessful = true для доставленного пассажира, false для проваленного заказа
    /// (используется CheckpointCounter.PassengerFailed) — по умолчанию true,
    /// чтобы не ломать уже существующие вызовы без этого параметра.
    /// </summary>
    public void AddEntry(int rating, int money, bool wasSuccessful = true)
    {
        var entry = new OrderHistoryEntry(rating, money, wasSuccessful);
        history.Add(entry);

        // Обрезаем старые записи сверху списка, если превысили лимит
        while (history.Count > maxEntries)
        {
            history.RemoveAt(0);
        }

        SaveHistory();
        OnHistoryChanged?.Invoke();
    }

    /// <summary>
    /// Возвращает историю заказов, новые записи первыми (удобно для отображения списком сверху вниз).
    /// </summary>
    public List<OrderHistoryEntry> GetHistory()
    {
        return history.AsEnumerable().Reverse().ToList();
    }

    /// <summary>
    /// Общее количество записей в истории (включая все прошлые смены).
    /// Используется ShiftStartTracker для фиксации индекса начала смены.
    /// </summary>
    public int GetTotalCount() => history.Count;

    /// <summary>
    /// Возвращает только записи начиная с указанного индекса (заказы текущей смены).
    /// startIndex — значение из ShiftStartTracker.ShiftOrderStartIndex.
    /// </summary>
    public List<OrderHistoryEntry> GetHistoryFromIndex(int startIndex)
    {
        if (startIndex < 0 || startIndex >= history.Count)
            return new List<OrderHistoryEntry>();
        return history.GetRange(startIndex, history.Count - startIndex);
    }

    public void ClearHistory()
    {
        history.Clear();
        SaveHistory();
        OnHistoryChanged?.Invoke();
    }

    private void SaveHistory()
    {
        var saveData = new OrderHistorySaveData { entries = history };
        string json = JsonUtility.ToJson(saveData);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
    }

    private void LoadHistory()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY)) return;

        string json = PlayerPrefs.GetString(SAVE_KEY);
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var saveData = JsonUtility.FromJson<OrderHistorySaveData>(json);
            if (saveData?.entries != null)
            {
                history = saveData.entries;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[OrderHistoryManager] Не удалось загрузить историю заказов: {e.Message}");
        }
    }
}
