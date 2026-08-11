using UnityEngine;

public class GameSceneCarSpawner : MonoBehaviour
{
    void Start()
    {
        // Получаем индекс из PlayerPrefs
        int selectedIndex = PlayerPrefs.GetInt("SelectedCarIndex", 0);
        
        // Или получаем префаб из CarDataHolder
        GameObject carPrefab = CarDataHolder.Instance.selectedCarPrefab;
        
        if (carPrefab != null)
        {
            Instantiate(carPrefab, transform.position, transform.rotation);
        }
    }
}