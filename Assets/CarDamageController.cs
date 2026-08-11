using UnityEngine;
using UnityEngine.VFX;

public class CarDamageController : MonoBehaviour
{
    [Header("Car Parts")]
    public GameObject frontIntact;
    public GameObject frontDamaged;
    public GameObject rearIntact;
    public GameObject rearDamaged;

    [Header("VFX Settings")]
    public VisualEffect smokeVFX;
    public float smokeSpawnRate = 10f;

    [Header("Health Settings")]
    public int maxHP = 100;
    public int minDamage = 5;
    public int maxDamage = 15;
    public int smokeThreshold = 50;
    public int repairCostPerHP = 10;
    public int criticalHP = 10;

    // Событие для оповещения о столкновении
    public event System.Action OnCarCollision;

    [SerializeField] private int currentHP;
    [SerializeField] private bool isSmokeActive = false;
    [SerializeField] private bool isInRepairZone = false;
    [SerializeField] private bool isEngineLocked = false;
    
    // Публичное свойство для доступа к статусу двигателя
    public bool IsEngineLocked => isEngineLocked;
    
    // Событие для оповещения о блокировке/разблокировке двигателя
    public event System.Action<bool> OnEngineLockStatusChanged;

    [SerializeField] private Сontroler carController;
    [SerializeField] private float originalMotorForce;

    private void Start()
    {
        carController = GetComponent<Сontroler>();
        if (carController != null)
        {
            originalMotorForce = carController._motorForce;
        }
        
        currentHP = maxHP;
        InitializeCarParts();
    }

    private void Update()
    {
        CheckEngineStatus();
    }

    private void CheckEngineStatus()
    {
        if (currentHP <= criticalHP && !isEngineLocked)
        {
            LockEngine();
        }
        else if (currentHP > criticalHP && isEngineLocked)
        {
            UnlockEngine();
        }
    }

    private void LockEngine()
    {
        bool wasLocked = isEngineLocked;
        isEngineLocked = true;
        if (carController != null)
        {
            carController.UpdateTorqueCurve(0f);
        }
        Debug.Log("Двигатель заблокирован! Автомобиль не может двигаться.");
        
        // Уведомляем о изменении статуса
        if (!wasLocked)
        {
            OnEngineLockStatusChanged?.Invoke(true);
        }
    }

    private void UnlockEngine()
    {
        bool wasLocked = isEngineLocked;
        isEngineLocked = false;
        if (carController != null)
        {
            carController.UpdateTorqueCurve(originalMotorForce);
        }
        Debug.Log("Двигатель разблокирован! Мощность восстановлена.");
        
        // Уведомляем о изменении статуса
        if (wasLocked)
        {
            OnEngineLockStatusChanged?.Invoke(false);
        }
    }

    private void InitializeCarParts()
    {
        frontIntact.SetActive(true);
        frontDamaged.SetActive(false);
        rearIntact.SetActive(true);
        rearDamaged.SetActive(false);

        if (smokeVFX != null)
        {
            smokeVFX.Stop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("RepairZone"))
        {
            isInRepairZone = true;
            TryRepairCar();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("RepairZone"))
        {
            TryRepairCar();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("RepairZone"))
        {
            isInRepairZone = false;
        }
    }

    private void TryRepairCar()
    {
        if (currentHP >= maxHP) return;

        int repairAmount = Mathf.Min(10, maxHP - currentHP);
        int repairCost = repairAmount * repairCostPerHP;

        if (PlayerManager.Instance.Balance >= repairCost)
        {
            PlayerManager.Instance.AddBalance(-repairCost);
            RepairCar(repairAmount);
            Debug.Log($"Починка: +{repairAmount} HP | Стоимость: {repairCost} | Баланс: {PlayerManager.Instance.Balance}");
        }
        else
        {
            Debug.Log("Недостаточно средств для ремонта!");
        }
    }

    private void RepairCar(int amount)
    {
        int oldHP = currentHP;
        currentHP = Mathf.Min(currentHP + amount, maxHP);
        
        // Применяем эффекты ремонта на основе нового уровня HP
        ApplyRepairEffects(oldHP, currentHP);
    }
    
    // Приватный метод для применения всех эффектов ремонта
    private void ApplyRepairEffects(int oldHP, int newHP)
    {
        // Убираем дым, если HP выше порога
        if (newHP >= smokeThreshold && isSmokeActive)
        {
            if (smokeVFX != null)
            {
                smokeVFX.Stop();
                isSmokeActive = false;
            }
        }
        
        // Восстанавливаем мощность двигателя, если HP выше порога дыма
        if (newHP >= smokeThreshold && carController != null)
        {
            // Если двигатель заблокирован и теперь HP выше критического, разблокируем
            if (isEngineLocked && newHP > criticalHP)
            {
                // Разблокировка произойдет автоматически в CheckEngineStatus в следующем кадре
                // Но можем вызвать напрямую для немедленного эффекта
                UnlockEngine();
            }
            else if (!isEngineLocked)
            {
                // Восстанавливаем полную мощность, если двигатель не заблокирован
                carController.UpdateTorqueCurve(originalMotorForce);
            }
        }
        
        // Восстанавливаем части автомобиля при полном ремонте
        if (newHP == maxHP)
        {
            frontIntact.SetActive(true);
            frontDamaged.SetActive(false);
            rearIntact.SetActive(true);
            rearDamaged.SetActive(false);
        }
    }
    
    // Публичный метод для полного ремонта автомобиля
    public void FullRepair()
    {
        int oldHP = currentHP;
        currentHP = maxHP;
        
        // Применяем все эффекты полного ремонта
        ApplyRepairEffects(oldHP, currentHP);
        
        // Разблокируем двигатель, если он был заблокирован (на случай, если CheckEngineStatus еще не сработал)
        if (isEngineLocked)
        {
            UnlockEngine();
        }
        
        Debug.Log("Автомобиль полностью отремонтирован!");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.relativeVelocity.magnitude > 2f)
        {
            ApplyDamage(collision);
        }
    }

    private void ApplyDamage(Collision collision)
    {
        int damage = Random.Range(minDamage, maxDamage + 1);
        currentHP -= damage;
        currentHP = Mathf.Clamp(currentHP, 0, maxHP);

        ContactPoint contact = collision.contacts[0];
        if (IsFrontCollision(contact.point))
        {
            frontIntact.SetActive(false);
            frontDamaged.SetActive(true);
        }
        else if (IsRearCollision(contact.point))
        {
            rearIntact.SetActive(false);
            rearDamaged.SetActive(true);
        }

        if (currentHP < smokeThreshold && !isSmokeActive && smokeVFX != null)
        {
            smokeVFX.SetFloat("SpawnRate", smokeSpawnRate);
            smokeVFX.Play();
            isSmokeActive = true;
            
            if (carController != null)
            {
                carController.UpdateTorqueCurve(originalMotorForce / 2f);
                Debug.Log("Двигатель поврежден! Мощность снижена вдвое");
            }
        }

        OnCarCollision?.Invoke();
    }

    private bool IsFrontCollision(Vector3 collisionPoint)
    {
        return collisionPoint.z > transform.position.z;
    }

    private bool IsRearCollision(Vector3 collisionPoint)
    {
        return collisionPoint.z < transform.position.z;
    }

    public float GetDamagePercentage()
    {
        return 1f - (float)currentHP / maxHP;
    }
    
    // Публичный метод для расчета стоимости полного ремонта
    public int GetFullRepairCost()
    {
        int damageAmount = maxHP - currentHP;   // весь урон, не ограничиваем 10
        return damageAmount * repairCostPerHP;
    }
    
    // Публичное свойство для доступа к текущему HP
    public int CurrentHP => currentHP;
}