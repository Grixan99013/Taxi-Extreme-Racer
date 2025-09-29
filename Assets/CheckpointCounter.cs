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
    
    private float currentMeterValue = 1f;
    private int currentPassengerType = -1;
    private bool hasPassenger = false;
    private CarDamageController carDamageController;

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
            
            HandleGreenCheckpoint();
        }
        else if (checkpoint.CompareTag("PurpleCheckpoint"))
        {
            if (hasPassenger)
            {
                DeliverPassenger();
            }
            
            HandlePurpleCheckpoint(checkpoint);
        }
    }

    private void DeliverPassenger()
    {
        hasPassenger = false;
        HidePassengerUI();
        
        int reward = 0;
        int rating = CalculatePassengerRating();
        
        switch (currentPassengerType)
        {
            case 0: reward = 150; break;
            case 1: reward = 200; break;
            case 2: reward = 100; break;
        }
        
        PlayerManager.Instance?.AddRating(rating);
        PlayerManager.Instance?.AddBalance(reward);
        PlayerManager.Instance?.AddExp(1);
        
        UpdateRatingUI();
        Debug.Log($"Пассажир доставлен! Награда: {reward}$. Оценка: {rating}/5. Получено 1 EXP");
        
        // Показываем уведомление
        ShowNotification(passengerSprites[currentPassengerType], 
                       $"Поставил оценку: {rating} ");
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

    private void HandleGreenCheckpoint()
    {
        foreach (var greenCheck in greenCheckpoints.Where(c => !c.isReached))
        {
            greenCheck.gameObject.SetActive(false);
        }

        if (purpleCheckpoints.Count > 0)
        {
            var unreachedPurple = purpleCheckpoints.Where(c => !c.isReached).ToList();
            if (unreachedPurple.Count > 0)
            {
                int randomIndex = Random.Range(0, unreachedPurple.Count);
                unreachedPurple[randomIndex].gameObject.SetActive(true);
            }
        }
    }

    private void HandlePurpleCheckpoint(Checkpoint checkpoint)
    {
        int randomAmount = Random.Range(10, 101);
        PlayerManager.Instance?.AddBalance(randomAmount);

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