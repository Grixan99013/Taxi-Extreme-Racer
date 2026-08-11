using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class EngineBreakdownUI : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject breakdownPanel;
    public TextMeshProUGUI messageText;
    public Button teleportToServiceButton;
    public Button goToMenuButton;
    public TextMeshProUGUI serviceCostText;

    [Header("Settings")]
    public int serviceTeleportCost = 500;
    public string menuSceneName = "Menu";
    [Tooltip("Точка телепорта в сервис (пустой GameObject)")]
    public Transform serviceTeleportPoint;

    private CarDamageController carDamageController;
    private bool panelWasShown = false;

    private void Start()
    {
        // Находим CarDamageController на игроке
        carDamageController = FindObjectOfType<CarDamageController>();
        
        if (carDamageController != null)
        {
            // Подписываемся на событие изменения статуса двигателя
            carDamageController.OnEngineLockStatusChanged += OnEngineLockStatusChanged;
        }
        
        // Проверяем наличие точки телепорта
        if (serviceTeleportPoint == null)
        {
            Debug.LogWarning("Service Teleport Point не назначен! Назначьте пустой GameObject в Inspector.");
        }
        
        // Настраиваем кнопки
        if (teleportToServiceButton != null)
        {
            teleportToServiceButton.onClick.AddListener(TeleportToService);
        }
        
        if (goToMenuButton != null)
        {
            goToMenuButton.onClick.AddListener(GoToMenu);
        }
        
        // Скрываем панель при старте
        if (breakdownPanel != null)
        {
            breakdownPanel.SetActive(false);
        }
        
        // Обновляем текст стоимости
        UpdateServiceCostText();
        
        // Проверяем начальный статус
        CheckEngineStatus();
    }

    private void OnDestroy()
    {
        // Отписываемся от события при уничтожении
        if (carDamageController != null)
        {
            carDamageController.OnEngineLockStatusChanged -= OnEngineLockStatusChanged;
        }
    }

    private void Update()
    {
        if (carDamageController == null)
        {
            carDamageController = FindObjectOfType<CarDamageController>();
            if (carDamageController != null)
            {
                carDamageController.OnEngineLockStatusChanged += OnEngineLockStatusChanged;
                CheckEngineStatus();
            }
            return;
        }
        
        // Обновляем доступность кнопки телепорта и текст стоимости
        UpdateTeleportButton();
        UpdateServiceCostText();
    }

    private void OnEngineLockStatusChanged(bool isLocked)
    {
        if (isLocked)
        {
            ShowBreakdownPanel();
            panelWasShown = true;
        }
        else
        {
            HideBreakdownPanel();
            panelWasShown = false;
        }
    }

    private void CheckEngineStatus()
    {
        if (carDamageController != null && carDamageController.IsEngineLocked && !panelWasShown)
        {
            ShowBreakdownPanel();
            panelWasShown = true;
        }
    }

    private void ShowBreakdownPanel()
    {
        if (breakdownPanel != null)
        {
            breakdownPanel.SetActive(true);
            Time.timeScale = 0f; // Останавливаем время
        }
    }

    private void HideBreakdownPanel()
    {
        if (breakdownPanel != null)
        {
            breakdownPanel.SetActive(false);
            Time.timeScale = 1f; // Возобновляем время
        }
    }

    private void UpdateServiceCostText()
    {
        if (serviceCostText != null)
        {
            int repairCost = GetRepairCost();
            int totalCost = serviceTeleportCost + repairCost;
            serviceCostText.text = $"Телепорт в сервис: {totalCost}$";
        }
    }

    private int GetRepairCost()
    {
        if (carDamageController != null)
        {
            return carDamageController.GetFullRepairCost();
        }
        return 0;
    }

    private int GetTotalCost()
    {
        return serviceTeleportCost + GetRepairCost();
    }

    private void UpdateTeleportButton()
    {
        if (teleportToServiceButton != null)
        {
            int totalCost = GetTotalCost();
            bool canAfford = PlayerManager.Instance != null && 
                           PlayerManager.Instance.Balance >= totalCost;
            teleportToServiceButton.interactable = canAfford;
        }
    }

    public void TeleportToService()
    {
        if (PlayerManager.Instance == null)
        {
            Debug.LogError("PlayerManager.Instance не найден!");
            return;
        }

        if (serviceTeleportPoint == null)
        {
            Debug.LogError("Service Teleport Point не назначен! Назначьте пустой GameObject в Inspector.");
            return;
        }

        if (carDamageController == null)
        {
            Debug.LogError("CarDamageController не найден!");
            return;
        }

        // Рассчитываем стоимость ремонта
        int repairCost = GetRepairCost();
        int totalCost = serviceTeleportCost + repairCost;

        // Проверяем достаточность средств
        if (PlayerManager.Instance.Balance < totalCost)
        {
            Debug.Log($"Недостаточно средств! Нужно: {totalCost}$ (Эвакуация: {serviceTeleportCost}$ + Ремонт: {repairCost}$), доступно: {PlayerManager.Instance.Balance}$");
            return;
        }

        // Списываем деньги (эвакуация + ремонт)
        PlayerManager.Instance.AddBalance(-totalCost);
        
        // Телепортируем машину
        Transform carTransform = carDamageController.transform;
        
        // Получаем Rigidbody для сброса скорости
        Rigidbody carRigidbody = carTransform.GetComponent<Rigidbody>();
        if (carRigidbody != null)
        {
            carRigidbody.velocity = Vector3.zero;
            carRigidbody.angularVelocity = Vector3.zero;
        }
        
        // Телепортируем на позицию точки телепорта
        carTransform.position = serviceTeleportPoint.position;
        carTransform.rotation = serviceTeleportPoint.rotation;
        
        // Полностью ремонтируем автомобиль
        carDamageController.FullRepair();
        
        Debug.Log($"Автомобиль телепортирован в сервис и полностью отремонтирован. Потрачено: {totalCost}$ (Эвакуация: {serviceTeleportCost}$ + Ремонт: {repairCost}$)");
        
        // Скрываем панель
        HideBreakdownPanel();
        panelWasShown = false;
        
        // Возобновляем время
        Time.timeScale = 1f;
    }

    public void GoToMenu()
    {
        Time.timeScale = 1f; // Возобновляем время перед сменой сцены
        SceneManager.LoadScene(menuSceneName);
    }
}

