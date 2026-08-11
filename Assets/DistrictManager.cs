using UnityEngine;
using System.Linq;

/// <summary>
/// Централизованно собирает все объекты CityDistrict на сцене
/// и позволяет определить, в каком районе находится точка/чекпоинт.
/// 
/// Достаточно один раз разместить этот скрипт на любом объекте сцены (например, на том же
/// объекте, где висит CheckpointCounter) — districts заполнятся автоматически в Awake,
/// либо можно перетащить районы в инспекторе вручную.
/// </summary>
public class DistrictManager : MonoBehaviour
{
    public static DistrictManager Instance { get; private set; }

    [Tooltip("Если оставить пустым, список заполнится автоматически всеми CityDistrict на сцене.")]
    public CityDistrict[] districts;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (districts == null || districts.Length == 0)
        {
            districts = FindObjectsOfType<CityDistrict>();
        }
    }

    /// <summary>
    /// Возвращает индекс района, в котором находится точка, либо -1, если точка
    /// не попала ни в один из заданных районов.
    /// </summary>
    public int GetDistrictIndex(Vector3 worldPosition)
    {
        for (int i = 0; i < districts.Length; i++)
        {
            if (districts[i] != null && districts[i].Contains(worldPosition))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Удобный метод для случая, когда чекпоинт привязан к конкретному CityDistrict
    /// по ссылке, а не вычисляется по координатам.
    /// </summary>
    public int GetDistrictIndex(CityDistrict district)
    {
        if (district == null) return -1;
        return System.Array.IndexOf(districts, district);
    }
}
