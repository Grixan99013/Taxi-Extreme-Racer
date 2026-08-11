using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Контроллер панели окончания смены.
/// Назначить на объект панели (тот же, что triggerPanel в Timer.cs).
/// Вызывается Timer.cs через метод Show() в момент истечения таймера.
/// </summary>
public class ShiftEndPanel : MonoBehaviour
{
    [Header("Статистика смены")]
    [Tooltip("Текст: Заработано за смену")]
    public TextMeshProUGUI earnedText;

    [Tooltip("Текст: Клиентов обслужено")]
    public TextMeshProUGUI clientsText;

    [Tooltip("Текст: Средняя оценка за смену")]
    public TextMeshProUGUI avgRatingText;

    [Header("Блок ремонта (показывается только при повреждениях)")]
    [Tooltip("Родительский объект блока — скрывается, если машина целая")]
    public GameObject repairBlock;

    [Tooltip("Текст с суммой списания за ремонт")]
    public TextMeshProUGUI repairCostText;

    [Tooltip("Текст: итоговый баланс после вычета ремонта")]
    public TextMeshProUGUI finalBalanceText;

    [Header("Кнопка")]
    public Button toGarageButton;

    [Header("Настройки сцены гаража")]
    [Tooltip("Название сцены гаража, куда перейти по кнопке")]
    public string garageSceneName = "Garage";

    // ────────────────────────────────────────────────
    // Внутренние данные, заполняются в Show()
    // ────────────────────────────────────────────────
    private int shiftEarned;
    private int shiftClients;
    private float shiftAvgRating;
    private int repairCost;

    private void Awake()
    {
        // Кнопка назначается здесь, если не указана в инспекторе
        if (toGarageButton != null)
        {
            toGarageButton.onClick.AddListener(OnGarageButtonClicked);
        }
    }

    /// <summary>
    /// Вызывается из Timer.cs вместо простого SetActive(true).
    /// Собирает статистику смены, списывает деньги за ремонт и заполняет UI.
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);
        CollectShiftStats();
        DeductRepairCost();
        RefreshUI();
    }

    // ────────────────────────────────────────────────
    // Сбор статистики
    // ────────────────────────────────────────────────

    private void CollectShiftStats()
    {
        // Получаем все записи истории заказов
        List<OrderHistoryEntry> allEntries =
            OrderHistoryManager.Instance != null
                ? OrderHistoryManager.Instance.GetHistory()
                : new List<OrderHistoryEntry>();

        // Берём только заказы текущей смены, фильтруя по индексу в истории.
        // ShiftStartTracker.ShiftOrderStartIndex фиксируется в момент старта таймера
        // и хранит, сколько записей уже было в истории до начала смены.
        // Это надёжнее фильтрации по timestampUnix, которая зависит от часового пояса
        // устройства и может давать сбои при быстром тестировании.

        int shiftStartIndex = ShiftStartTracker.ShiftOrderStartIndex;

        List<OrderHistoryEntry> shiftEntries;
        if (shiftStartIndex >= 0 && OrderHistoryManager.Instance != null)
        {
            // Заказы только этой смены — начиная с зафиксированного индекса
            shiftEntries = OrderHistoryManager.Instance.GetHistoryFromIndex(shiftStartIndex);
        }
        else
        {
            // Fallback: индекс не зафиксирован — берём всю историю
            shiftEntries = allEntries;
        }

        shiftClients = shiftEntries.Count(e => e.wasSuccessful);

        shiftEarned = shiftEntries
            .Where(e => e.wasSuccessful)
            .Sum(e => e.money);

        shiftAvgRating = shiftClients > 0
            ? (float)shiftEntries.Where(e => e.wasSuccessful).Sum(e => e.rating) / shiftClients
            : 0f;
    }

    // ────────────────────────────────────────────────
    // Ремонт
    // ────────────────────────────────────────────────

    private void DeductRepairCost()
    {
        CarDamageController car = FindObjectOfType<CarDamageController>();

        if (car == null || car.CurrentHP >= car.maxHP)
        {
            repairCost = 0;
            return;
        }

        // Полная стоимость восстановления до 100 HP
        repairCost = car.GetFullRepairCost();

        if (repairCost <= 0) return;

        // Списываем деньги (можно уйти в минус — штраф за разбитое авто)
        PlayerManager.Instance?.AddBalance(-repairCost);
        PlayerManager.Instance?.SaveBalance();

        Debug.Log($"[ShiftEnd] Ремонт авто: -{repairCost}$. Осталось: {PlayerManager.Instance?.Balance}$");
    }

    // ────────────────────────────────────────────────
    // Обновление UI
    // ────────────────────────────────────────────────

    private void RefreshUI()
    {
        if (earnedText != null)
            earnedText.text = $"За смену заработано: {shiftEarned}$";

        if (clientsText != null)
            clientsText.text = $"Клиентов обслужено: {shiftClients}";

        if (avgRatingText != null)
        {
            string ratingStr = shiftClients > 0
                ? shiftAvgRating.ToString("F1")
                : "—";
            avgRatingText.text = $"Средняя оценка: {ratingStr}";
        }

        // Блок ремонта
        if (repairBlock != null)
            repairBlock.SetActive(repairCost > 0);

        if (repairCostText != null && repairCost > 0)
            repairCostText.text = $"Ремонт авто: -{repairCost}$";

        if (finalBalanceText != null && PlayerManager.Instance != null)
            finalBalanceText.text = $"Итого на счёте: {PlayerManager.Instance.Balance}$";
    }

    // ────────────────────────────────────────────────
    // Кнопка «В гараж»
    // ────────────────────────────────────────────────

    private void OnGarageButtonClicked()
    {
        // Сохраняем всё перед переходом
        PlayerManager.Instance?.SaveAllData();

        // Восстанавливаем время (оно было остановлено Timer.cs)
        Time.timeScale = 1f;

        SceneManager.LoadScene(garageSceneName);
    }
}
