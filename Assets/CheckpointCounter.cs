using UnityEngine;
using TMPro;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

public class CheckpointCounter : MonoBehaviour
{
    public TextMeshProUGUI currentCheckText;
    public int currentCheckpointCount = 0;

    public List<Checkpoint> greenCheckpoints = new List<Checkpoint>();
    public List<Checkpoint> purpleCheckpoints = new List<Checkpoint>();

    [Header("Passenger System")]
    public Slider passengerMeter;
    public Image sliderFill;
    public Image passengerAvatar;
    public Sprite[] passengerSprites;
    public float[] depletionRates = { 0.3f, 0.15f, 0.2f };
    public float[] collisionPenalties = { 0f, 0.3f, 0.1f };

    [Header("Rating System")]
    public TextMeshProUGUI ratingText;

    [Header("Мини-игра чаевых")]
    [Tooltip("Ссылка на компонент мини-игры (полоска с зелёной зоной), показываемой при доставке пассажира.")]
    public PickupMinigame pickupMinigame;

    [Tooltip("Сколько секунд индикатор идёт через всю полоску, по типу пассажира (индекс = passengerType).")]
    public float[] minigameTravelTimeByType = { 1.5f, 1.2f, 1.8f };

    [Tooltip("Ширина зелёной зоны при оценке 5/5 (максимум, доля 0..1).")]
    public float minigameZoneWidthMax = 0.35f;

    [Tooltip("Ширина зелёной зоны при оценке 1/5 (минимум, доля 0..1).")]
    public float minigameZoneWidthMin = 0.06f;

    [Range(0f, 1f)]
    [Tooltip("Доля от стоимости заказа, начисляемая как чаевые при успешной мини-игре.")]
    public float tipPercentage = 0.3f;
    
    private float currentMeterValue = 1f;
    private int currentPassengerType = -1;
    private bool hasPassenger = false;
    private CarDamageController carDamageController;

    [Header("Passenger Reactions")]
    [Tooltip("Система реплик пассажира. Назначить PassengerReactionSystem из сцены.")]
    public PassengerReactionSystem passengerReactionSystem;

    [Header("Notification System")]
    public GameObject notificationPanel;
    public Image notificationAvatar;
    public TextMeshProUGUI notificationText;
    public float notificationDuration = 5f;

    [Header("Checkpoint Spawn Settings")]
    public float minSpawnDelay = 5f;
    public float maxSpawnDelay = 10f;
    private bool isSpawningCheckpoints = false;

    private Coroutine currentNotification;

    private void Start()
    {
        FindAllCheckpoints();
        UpdateRatingUI();
        
        if (notificationPanel != null)
        {
            notificationPanel.SetActive(false);
        }

        if (currentCheckText != null)
        {
            currentCheckText.fontSize = 26f;
            currentCheckText.text = currentCheckpointCount.ToString();
        }
        
        HidePassengerUI();
        
        carDamageController = FindObjectOfType<CarDamageController>();
        if (carDamageController != null)
        {
            carDamageController.OnCarCollision += HandleCarCollision;
        }

        StartCoroutine(SpawnCheckpointsWithDelay());
    }

    private void OnDestroy()
    {
        if (carDamageController != null)
        {
            carDamageController.OnCarCollision -= HandleCarCollision;
        }
    }

    private void Update()
    {
        if (hasPassenger)
        {
            UpdatePassengerMeter();
            sliderFill.color = Color.Lerp(Color.red, Color.green, currentMeterValue);
        }
    }

    private void UpdatePassengerMeter()
    {
        currentMeterValue -= depletionRates[currentPassengerType] * Time.deltaTime;
        passengerMeter.value = currentMeterValue;
        
        if (currentMeterValue <= 0)
        {
            PassengerFailed();
        }
    }

    private void HandleCarCollision()
    {
        if (!hasPassenger) return;
        
        currentMeterValue -= collisionPenalties[currentPassengerType];
        passengerMeter.value = currentMeterValue;
        
        if (currentMeterValue <= 0)
        {
            PassengerFailed();
        }
    }

    private void PassengerFailed()
    {
        hasPassenger = false;
        HidePassengerUI();
        
        // Клиент ставит минимальную оценку 1 при сбое
        int rating = 1;
        PlayerManager.Instance?.AddRating(rating);
        UpdateRatingUI();

        // Записываем в историю заказов (деньги не начисляются при провале)
        OrderHistoryManager.Instance?.AddEntry(rating, 0, wasSuccessful: false);

        // Уведомляем систему реплик о провале
        passengerReactionSystem?.OnPassengerFailed(
            currentPassengerType,
            passengerSprites != null && currentPassengerType < passengerSprites.Length
                ? passengerSprites[currentPassengerType] : null);

        // Показываем уведомление
        ShowNotification(passengerSprites[currentPassengerType], 
                       $"Пассажир недоволен! Оценка: {rating}");

        if (!isSpawningCheckpoints)
        {
            StartCoroutine(SpawnCheckpointsWithDelay());
        }

        foreach (var purpleCheck in purpleCheckpoints.Where(c => c.gameObject.activeSelf))
        {
            purpleCheck.gameObject.SetActive(false);
        }
        
        foreach (var greenCheck in greenCheckpoints.Where(c => !c.isReached))
        {
            greenCheck.gameObject.SetActive(true);
        }
    }
    private void ShowPassengerUI(int type)
    {
        passengerMeter.gameObject.SetActive(true);
        passengerAvatar.gameObject.SetActive(true);
        passengerAvatar.sprite = passengerSprites[type];
        currentMeterValue = 1f;
        passengerMeter.value = 1f; 
    }

    private void HidePassengerUI()
    {
        passengerMeter.gameObject.SetActive(false);
        passengerAvatar.gameObject.SetActive(false);
    }

    private void FindAllCheckpoints()
    {
        greenCheckpoints = FindObjectsOfType<Checkpoint>()
            .Where(c => c.CompareTag("GreenCheckpoint"))
            .ToList();

        purpleCheckpoints = FindObjectsOfType<Checkpoint>()
            .Where(c => c.CompareTag("PurpleCheckpoint"))
            .ToList();

        // Деактивируем ВСЕ чекпоинты при старте
        foreach (var checkpoint in purpleCheckpoints)
        {
            checkpoint.gameObject.SetActive(false);
        }
        
        foreach (var checkpoint in greenCheckpoints)
        {
            checkpoint.gameObject.SetActive(false);
        }
    }

    public void OnCheckpointReached(Checkpoint checkpoint)
    {
        if (checkpoint == null || (hasPassenger && checkpoint.CompareTag("GreenCheckpoint"))) 
        {
            return;
        }

        checkpoint.isReached = true;
        checkpoint.gameObject.SetActive(false);

        if (checkpoint.CompareTag("GreenCheckpoint"))
        {
            currentPassengerType = checkpoint.passengerType;
            hasPassenger = true;
            ShowPassengerUI(currentPassengerType);

            passengerReactionSystem?.OnPassengerPickedUp(
                currentPassengerType,
                passengerSprites != null && currentPassengerType < passengerSprites.Length
                    ? passengerSprites[currentPassengerType] : null);

            HandleGreenCheckpoint(checkpoint);
        }
        else if (checkpoint.CompareTag("PurpleCheckpoint"))
        {
            if (hasPassenger)
            {
                StartCoroutine(DeliverPassengerRoutine());
            }
            
            HandlePurpleCheckpoint(checkpoint);
        }
    }

    /// <summary>
    /// Запускает мини-игру чаевых (если она настроена) и после получения результата
    /// начисляет награду пассажиру. Если pickupMinigame не назначен в инспекторе —
    /// награда начисляется сразу же, без мини-игры (старое поведение сохраняется как fallback).
    /// </summary>
    private IEnumerator DeliverPassengerRoutine()
    {
        hasPassenger = false;
        HidePassengerUI();

        // Сохраняем тип пассажира локально — currentPassengerType может быть изменён,
        // если за время ожидания мини-игры подберём нового пассажира (на практике это
        // не должно произойти за доли секунды мини-игры, но это надёжнее).
        int passengerType = currentPassengerType;

        int baseRating = CalculatePassengerRating();
        int reward = GetBaseReward(passengerType);

        bool tipEarned = false;

        if (pickupMinigame != null)
        {
            bool minigameDone = false;
            bool minigameSuccess = false;

            float travelTime = GetByTypeOrDefault(minigameTravelTimeByType, passengerType, 1.5f);

            // Ширина зелёной зоны зависит от текущей оценки за заказ:
            // оценка 5 -> максимальная ширина, оценка 1 -> минимальная ширина.
            float ratingT = (baseRating - 1) / 4f;  // нормализуем 1..5 -> 0..1
            float zoneWidth = Mathf.Lerp(minigameZoneWidthMin, minigameZoneWidthMax, ratingT);

            pickupMinigame.StartMinigame(travelTime, zoneWidth, baseRating, success =>
            {
                minigameSuccess = success;
                minigameDone = true;
            });

            yield return new WaitUntil(() => minigameDone);

            tipEarned = minigameSuccess;
        }

        int finalRating = baseRating;
        int finalReward = reward;

        if (tipEarned)
        {
            // +30% от стоимости заказа в виде чаевых
            finalReward += Mathf.RoundToInt(reward * tipPercentage);

            // Если поездка не была оценена на максимум — успешная мини-игра добавляет +1 балл
            if (finalRating < 5)
            {
                finalRating = Mathf.Min(5, finalRating + 1);
            }
        }

        PlayerManager.Instance?.AddRating(finalRating);
        PlayerManager.Instance?.AddBalance(finalReward);
        PlayerManager.Instance?.AddExp(1);

        // Система заданий и история заказов
        QuestManager.Instance?.ReportOrderDelivered(finalRating, finalReward);
        OrderHistoryManager.Instance?.AddEntry(finalRating, finalReward);

        UpdateRatingUI();

        string tipSuffix = tipEarned ? " (+ чаевые!)" : "";
        Debug.Log($"Пассажир доставлен! Награда: {finalReward}$. Оценка: {finalRating}/5{tipSuffix}. Получено 1 EXP");

        // Уведомляем систему реплик о доставке
        passengerReactionSystem?.OnPassengerDelivered(
            passengerType,
            passengerSprites != null && passengerType < passengerSprites.Length
                ? passengerSprites[passengerType] : null);

        // Показываем уведомление
        ShowNotification(passengerSprites[passengerType],
                       $"Поставил оценку: {finalRating}{(tipEarned ? "  +чаевые" : "")}");
    }

    /// <summary>Базовая стоимость заказа по типу пассажира (до чаевых).</summary>
    private int GetBaseReward(int passengerType)
    {
        switch (passengerType)
        {
            case 0: return 150;
            case 1: return 200;
            case 2: return 100;
            default: return 0;
        }
    }

    private float GetByTypeOrDefault(float[] array, int index, float fallback)
    {
        if (array == null || array.Length == 0) return fallback;
        if (index < 0 || index >= array.Length) return array[0];
        return array[index];
    }

    private IEnumerator SpawnCheckpointsWithDelay()
    {
        if (isSpawningCheckpoints) yield break;
        isSpawningCheckpoints = true;

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minSpawnDelay, maxSpawnDelay));
            
            var availableCheckpoints = greenCheckpoints
                .Where(c => !c.isReached && !c.gameObject.activeSelf)
                .ToList();

            if (availableCheckpoints.Count > 0)
            {
                int randomIndex = Random.Range(0, availableCheckpoints.Count);
                var checkpoint = availableCheckpoints[randomIndex];
                
                // Задаем тип пассажира для этого чекпоинта
                checkpoint.passengerType = Random.Range(0, passengerSprites.Length);
                
                checkpoint.gameObject.SetActive(true);
                
                // Используем сохраненный тип для уведомления
                ShowNotification(passengerSprites[checkpoint.passengerType], 
                               $"Поступил заказ на {checkpoint.checkpointName}");
            }
        }
    }

    private void HandleGreenCheckpoint(Checkpoint pickedUpCheckpoint)
    {
        foreach (var greenCheck in greenCheckpoints.Where(c => !c.isReached))
        {
            greenCheck.gameObject.SetActive(false);
        }

        if (purpleCheckpoints.Count > 0)
        {
            // Сначала пытаемся найти фиолетовые чекпоинты ИЗ ДРУГОГО района,
            // чтобы маршрут не получился слишком коротким.
            var candidates = purpleCheckpoints
                .Where(c => !c.isReached && c.districtIndex != pickedUpCheckpoint.districtIndex)
                .ToList();

            // Если в других районах подходящих точек назначения не нашлось
            // (например, чекпоинты ещё не успели определить свой район, или район один),
            // используем любые недостигнутые фиолетовые чекпоинты, чтобы игра не "зависла" без заказа.
            if (candidates.Count == 0)
            {
                candidates = purpleCheckpoints.Where(c => !c.isReached).ToList();
            }

            if (candidates.Count > 0)
            {
                int randomIndex = Random.Range(0, candidates.Count);
                candidates[randomIndex].gameObject.SetActive(true);
            }
        }
    }

    private void HandlePurpleCheckpoint(Checkpoint checkpoint)
    {
        // Начисление денег и записи в историю происходит в DeliverPassengerRoutine.
        // Дублирующий AddBalance здесь удалён — иначе деньги начислялись дважды.

        // Запускаем спавн новых чекпоинтов через систему с задержкой
        if (!isSpawningCheckpoints)
        {
            StartCoroutine(SpawnCheckpointsWithDelay());
        }

        foreach (var purpleCheck in purpleCheckpoints)
        {
            purpleCheck.gameObject.SetActive(false);
        }

        currentCheckpointCount += checkpoint.index;
        if (currentCheckText != null)
        {
            currentCheckText.text = currentCheckpointCount.ToString();
        }
    }

    private int CalculatePassengerRating()
    {
        int baseRating = Mathf.Clamp(Mathf.RoundToInt(currentMeterValue * 4f) + 1, 1, 5);
        
        if (currentPassengerType == 1 && currentMeterValue > 0.6f)
        {
            baseRating = Mathf.Min(5, baseRating + 1);
        }
        
        return baseRating;
    }

    private void UpdateRatingUI()
    {
        if (ratingText != null && PlayerManager.Instance != null)
        {
            float rating = PlayerManager.Instance.CurrentRating;
            ratingText.text = rating.ToString("F1");
            ratingText.fontSize = 28f;
            
            if (rating >= 4f) ratingText.color = Color.green;
            else if (rating >= 2.5f) ratingText.color = Color.yellow;
            else ratingText.color = Color.red;
        }
    }
    private void ShowNotification(Sprite avatarSprite, string message)
    {
        if (notificationPanel == null || notificationAvatar == null || notificationText == null)
        {
            Debug.LogWarning("Не все компоненты уведомления настроены!");
            return;
        }

        // Останавливаем предыдущее уведомление, если оно активно
        if (currentNotification != null)
        {
            StopCoroutine(currentNotification);
        }

        // Настраиваем уведомление
        notificationAvatar.sprite = avatarSprite;
        notificationText.text = message;
        notificationPanel.SetActive(true);

        // Запускаем скрытие уведомления через заданное время
        currentNotification = StartCoroutine(HideNotificationAfterDelay());
    }

    private IEnumerator HideNotificationAfterDelay()
    {
        yield return new WaitForSeconds(notificationDuration);

        float fadeTime = 0.5f;
        CanvasGroup group = notificationPanel.GetComponent<CanvasGroup>();
        if (group != null)
        {
            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                group.alpha = Mathf.Lerp(1f, 0f, elapsed/fadeTime);
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
        
        notificationPanel.SetActive(false);
        currentNotification = null;
    }
}