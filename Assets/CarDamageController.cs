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

    // Событие для оповещения о столкновении
    public event System.Action OnCarCollision;

    private int currentHP;
    private bool isSmokeActive = false;
    private bool isInRepairZone = false;

    private Сontroler carController;
    private float originalMotorForce;

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
        currentHP = Mathf.Min(currentHP + amount, maxHP);

        if (currentHP >= smokeThreshold && isSmokeActive)
        {
            if (smokeVFX != null)
            {
                smokeVFX.Stop();
                isSmokeActive = false;
            }
            
            // Восстанавливаем оригинальную мощность двигателя
            if (carController != null)
            {
                carController._motorForce = originalMotorForce;
                Debug.Log("Двигатель отремонтирован! Мощность восстановлена");
            }
        }

        if (currentHP == maxHP)
        {
            frontIntact.SetActive(true);
            frontDamaged.SetActive(false);
            rearIntact.SetActive(true);
            rearDamaged.SetActive(false);
        }
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
                carController._motorForce = originalMotorForce / 2f;
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
}