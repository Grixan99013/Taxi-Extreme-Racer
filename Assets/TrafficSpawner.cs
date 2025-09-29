using UnityEngine;
using System.Collections.Generic;

public class TrafficSpawner : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] private GameObject[] carPrefabs; 
    [SerializeField] private Transform[] spawnPoints; 
    [SerializeField] private Transform[] waypointRoutes;

    [Header("Настройки спавна")]
    [SerializeField] private float spawnInterval = 3f;
    [SerializeField] private int maxCarsOnScene = 20;
    [SerializeField] private float minCarSpeed = 5f;
    [SerializeField] private float maxCarSpeed = 12f;
    [SerializeField] private float crashDestroyDelay = 15f; // Добавляем параметр в спавнер

    private List<GameObject> activeCars = new List<GameObject>();
    private float spawnTimer = 0f;

    void Update()
    {
        if (activeCars.Count < maxCarsOnScene && spawnTimer >= spawnInterval)
        {
            SpawnCar();
            spawnTimer = 0f;
        }
        spawnTimer += Time.deltaTime;
    }

    void SpawnCar()
    {
        if (carPrefabs.Length == 0 || spawnPoints.Length == 0 || waypointRoutes.Length == 0)
        {
            Debug.LogWarning("Не хватает префабов, точек спавна или маршрутов!");
            return;
        }

        GameObject randomCar = carPrefabs[Random.Range(0, carPrefabs.Length)];
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Transform routeParent = waypointRoutes[Random.Range(0, waypointRoutes.Length)];

        GameObject newCar = Instantiate(randomCar, spawnPoint.position, spawnPoint.rotation);
        activeCars.Add(newCar);

        TrafficCarWaypoints carController = newCar.AddComponent<TrafficCarWaypoints>();

        carController.OnReachEnd += () => RemoveCar(newCar);

        carController.maxSpeed = Random.Range(minCarSpeed, maxCarSpeed);
        carController.minSpeed = carController.maxSpeed * 0.7f;
        carController.destroyDelay = crashDestroyDelay;
        
        // Наполняем список waypoints
        carController.waypoints = new List<Transform>();
        foreach (Transform child in routeParent)
        {
            carController.waypoints.Add(child);
        }

        carController.OnReachEnd += () => RemoveCar(newCar);
        carController.OnCrash += () => RemoveCar(newCar);
    }

    void RemoveCar(GameObject car)
    {
        if (car != null && activeCars.Contains(car))
        {
            var carController = car.GetComponent<TrafficCarWaypoints>();
            if (carController != null && !carController.IsCrashed) // Используем публичное свойство
            {
                activeCars.Remove(car);
                Destroy(car);
            }
        }
    }

    void OnDisable()
    {
        foreach (var car in activeCars)
        {
            if (car != null) Destroy(car);
        }
        activeCars.Clear();
    }
}