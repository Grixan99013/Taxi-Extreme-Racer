using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public int index;
    public string checkpointName;
    public int passengerType = -1; 
    public bool isReached;

    [Header("Район (для системы маршрутов)")]
    [Tooltip("Только для отображения в инспекторе. Реальное значение всегда считается через свойство District ниже.")]
    [SerializeField] private int debugDistrictIndex = -1;

    /// <summary>
    /// Индекс района, в котором сейчас находится чекпоинт.
    /// Считается по факту обращения, поэтому не зависит от порядка выполнения Start()
    /// между разными объектами сцены.
    /// </summary>
    public int districtIndex
    {
        get
        {
            int result = -1;
            if (DistrictManager.Instance != null)
            {
                result = DistrictManager.Instance.GetDistrictIndex(transform.position);
            }
            debugDistrictIndex = result;
            return result;
        }
    }
}
