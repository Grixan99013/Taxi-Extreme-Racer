using UnityEngine;

public class CarDataHolder : MonoBehaviour
{
    public static CarDataHolder Instance;
    public GameObject selectedCarPrefab;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetSelectedCar(GameObject prefab)
    {
        selectedCarPrefab = prefab;
    }
}