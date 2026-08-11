using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Спавнер трафика. Логика обычных одиночных машин и логика поездов (колонн)
/// полностью разделены и используют разные наборы префабов/точек спавна:
///
///   ОБЫЧНЫЕ МАШИНЫ  — carPrefabs   + spawnPoints      → всегда спавнятся поодиночке.
///   ПОЕЗДА (колонны) — trainPrefabs + trainSpawnPoints → спавнятся только колонной.
///
/// Обычные машины никогда не образуют колонну — это отдельный, независимый цикл спавна.
///
/// НАСТРОЙКА ВАГОНОВ ПОЕЗДА (trainPrefabs):
///   Каждый элемент массива — это TrainCarPrefab с галочкой isLeader.
///   Вагон с isLeader = true всегда спавнится первым и едет во главе колонны,
///   независимо от того, на какой позиции в массиве он находится.
///   Остальные (isLeader = false) — обычные вагоны, выбираются случайно для оставшихся мест.
///   Если в массиве несколько вагонов помечены как лидер — используется случайный из них.
///   Если ни один не помечен — лидером станет случайный вагон из массива (с предупреждением в консоли).
///
/// ВАЖНО: все машины в проекте должны иметь WheelController в префабе,
///   TrafficCarWaypoints добавляется динамически здесь.
/// </summary>
public class TrafficSpawner : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Вспомогательный тип для вагонов поезда
    // -----------------------------------------------------------------------

    [System.Serializable]
    public class TrainCarPrefab
    {
        public GameObject prefab;

        [Tooltip("Если включено — этот префаб всегда спавнится первым и едёт во главе колонны.")]
        public bool isLeader;
    }

    // -----------------------------------------------------------------------
    // Inspector — обычные машины
    // -----------------------------------------------------------------------

    [Header("Обычные машины")]
    [SerializeField] private GameObject[] carPrefabs;
    [SerializeField] private Transform[]  spawnPoints;
    [SerializeField] private Transform[]  waypointRoutes;

    [Header("Настройки спавна обычных машин")]
    [SerializeField] private float spawnInterval     = 3f;
    [SerializeField] private int   maxCarsOnScene    = 20;
    [SerializeField] private float minCarSpeed       = 5f;
    [SerializeField] private float maxCarSpeed       = 12f;
    [SerializeField] private float crashDestroyDelay = 15f;

    // -----------------------------------------------------------------------
    // Inspector — поезда (колонны)
    // -----------------------------------------------------------------------

    [Header("Поезда (колонны)")]
    [Tooltip("Вагоны поезда. Минимум один должен быть помечен isLeader = true.")]
    [SerializeField] private TrainCarPrefab[] trainPrefabs;

    [Tooltip("Точки спавна, используемые только для поездов.")]
    [SerializeField] private Transform[] trainSpawnPoints;

    [Tooltip("Маршруты, по которым едут поезда. Если пусто — используются waypointRoutes обычных машин.")]
    [SerializeField] private Transform[] trainWaypointRoutes;

    [Header("Настройки спавна поездов")]
    [SerializeField] private float trainSpawnInterval = 10f;
    [SerializeField] private int   maxTrainsOnScene    = 3;

    [Tooltip("Количество вагонов в колонне (включая лидера).")]
    [Range(2, 6)]
    [SerializeField] private int trainLength = 3;

    [Tooltip("Расстояние между вагонами в колонне (м).")]
    [Range(4f, 20f)]
    [SerializeField] private float trainCarSpacing = 8f;

    [SerializeField] private float trainMinSpeed       = 5f;
    [SerializeField] private float trainMaxSpeed       = 10f;
    [SerializeField] private float trainCrashDestroyDelay = 15f;

    // -----------------------------------------------------------------------
    // Runtime
    // -----------------------------------------------------------------------

    private List<GameObject> activeCars   = new List<GameObject>();
    private List<GameObject> activeTrains = new List<GameObject>(); // хранит только лидеров колонн

    private float carSpawnTimer   = 0f;
    private float trainSpawnTimer = 0f;

    // -----------------------------------------------------------------------
    // Unity lifecycle
    // -----------------------------------------------------------------------

    void Update()
    {
        UpdateCarSpawning();
        UpdateTrainSpawning();
    }

    void OnDisable()
    {
        foreach (var car in activeCars)
            if (car != null) Destroy(car);
        activeCars.Clear();

        foreach (var leader in activeTrains)
            if (leader != null) Destroy(leader);
        activeTrains.Clear();
    }

    // =========================================================================
    // ОБЫЧНЫЕ МАШИНЫ — независимый цикл, никогда не образуют колонну
    // =========================================================================

    void UpdateCarSpawning()
    {
        activeCars.RemoveAll(c => c == null);

        if (activeCars.Count < maxCarsOnScene && carSpawnTimer >= spawnInterval)
        {
            SpawnSingleCar();
            carSpawnTimer = 0f;
        }

        carSpawnTimer += Time.deltaTime;
    }

    void SpawnSingleCar()
    {
        if (!ValidateCarAssets()) return;

        GameObject prefab     = carPrefabs[Random.Range(0, carPrefabs.Length)];
        Transform  spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Transform  route      = waypointRoutes[Random.Range(0, waypointRoutes.Length)];

        SpawnCar(prefab, spawnPoint.position, spawnPoint.rotation, route,
                 minCarSpeed, maxCarSpeed, crashDestroyDelay, activeCars);
    }

    bool ValidateCarAssets()
    {
        if (carPrefabs == null     || carPrefabs.Length     == 0 ||
            spawnPoints == null    || spawnPoints.Length    == 0 ||
            waypointRoutes == null || waypointRoutes.Length == 0)
        {
            Debug.LogWarning("[TrafficSpawner] Не настроены carPrefabs, spawnPoints или waypointRoutes!", this);
            return false;
        }
        return true;
    }

    // =========================================================================
    // ПОЕЗДА (КОЛОННЫ) — полностью отдельный цикл, своя точка спавна и префабы
    // =========================================================================

    void UpdateTrainSpawning()
    {
        activeTrains.RemoveAll(leader => leader == null);

        if (activeTrains.Count < maxTrainsOnScene && trainSpawnTimer >= trainSpawnInterval)
        {
            SpawnTrain();
            trainSpawnTimer = 0f;
        }

        trainSpawnTimer += Time.deltaTime;
    }

    void SpawnTrain()
    {
        if (!ValidateTrainAssets()) return;

        Transform[] routesToUse = (trainWaypointRoutes != null && trainWaypointRoutes.Length > 0)
            ? trainWaypointRoutes
            : waypointRoutes;

        if (routesToUse == null || routesToUse.Length == 0)
        {
            Debug.LogWarning("[TrafficSpawner] Нет маршрутов для поезда (ни trainWaypointRoutes, ни waypointRoutes)!", this);
            return;
        }

        Transform spawnPoint = trainSpawnPoints[Random.Range(0, trainSpawnPoints.Length)];
        Transform route      = routesToUse[Random.Range(0, routesToUse.Length)];

        GameObject leaderPrefab = PickLeaderPrefab();
        List<GameObject> regularPrefabs = trainPrefabs
            .Where(t => t.prefab != null && !t.isLeader)
            .Select(t => t.prefab)
            .ToList();

        // Если кроме лидера больше нет обычных вагонов — колонна будет состоять только из лидеров той же модели
        if (regularPrefabs.Count == 0)
            regularPrefabs.Add(leaderPrefab);

        // Спавним лидера — всегда первым, всегда во главе колонны
        TrafficCarWaypoints leader = SpawnCar(
            leaderPrefab, spawnPoint.position, spawnPoint.rotation, route,
            trainMinSpeed, trainMaxSpeed, trainCrashDestroyDelay, null);

        if (leader == null) return;

        activeTrains.Add(leader.gameObject);

        // Спавним ведомых позади лидера
        for (int i = 1; i < trainLength; i++)
        {
            Vector3 followerPos = spawnPoint.position - spawnPoint.forward * (trainCarSpacing * i);
            GameObject followerPrefab = regularPrefabs[Random.Range(0, regularPrefabs.Count)];

            TrafficCarWaypoints follower = SpawnCar(
                followerPrefab, followerPos, spawnPoint.rotation, route,
                trainMinSpeed, trainMaxSpeed, trainCrashDestroyDelay, null);

            if (follower != null)
                follower.SetupAsFollower(leader, trainCarSpacing);
        }
    }

    /// <summary>
    /// Выбирает префаб лидера колонны. Если помечено несколько — случайный из них.
    /// Если ни один не помечен — берётся случайный вагон из всего массива (с предупреждением).
    /// </summary>
    GameObject PickLeaderPrefab()
    {
        var leaders = trainPrefabs.Where(t => t.prefab != null && t.isLeader).Select(t => t.prefab).ToList();

        if (leaders.Count > 0)
            return leaders[Random.Range(0, leaders.Count)];

        Debug.LogWarning("[TrafficSpawner] В trainPrefabs не помечен ни один вагон как isLeader! " +
                          "Используется случайный вагон в качестве лидера.", this);

        var anyPrefabs = trainPrefabs.Where(t => t.prefab != null).Select(t => t.prefab).ToList();
        return anyPrefabs[Random.Range(0, anyPrefabs.Count)];
    }

    bool ValidateTrainAssets()
    {
        if (trainPrefabs == null || trainPrefabs.Length == 0 ||
            trainPrefabs.All(t => t.prefab == null))
        {
            Debug.LogWarning("[TrafficSpawner] Не настроен trainPrefabs!", this);
            return false;
        }

        if (trainSpawnPoints == null || trainSpawnPoints.Length == 0)
        {
            Debug.LogWarning("[TrafficSpawner] Не настроен trainSpawnPoints!", this);
            return false;
        }

        return true;
    }

    // =========================================================================
    // Общий низкоуровневый спавн одной машины
    // =========================================================================

    /// <summary>
    /// Создаёт машину, добавляет TrafficCarWaypoints, настраивает параметры.
    /// trackingList — список, в который добавится созданный объект (для авто-уборки), может быть null.
    /// </summary>
    TrafficCarWaypoints SpawnCar(
        GameObject prefab,
        Vector3    position,
        Quaternion rotation,
        Transform  route,
        float      minSpeed,
        float      maxSpeed,
        float      destroyDelay,
        List<GameObject> trackingList)
    {
        GameObject newCar = Instantiate(prefab, position, rotation);
        trackingList?.Add(newCar);

        TrafficCarWaypoints ctrl = newCar.AddComponent<TrafficCarWaypoints>();

        ctrl.maxSpeed     = Random.Range(minSpeed, maxSpeed);
        ctrl.minSpeed     = ctrl.maxSpeed * 0.7f;
        ctrl.destroyDelay = destroyDelay;

        ctrl.waypoints = new List<Transform>();
        foreach (Transform child in route)
            ctrl.waypoints.Add(child);

        ctrl.OnReachEnd += () => RemoveTrackedCar(newCar, trackingList);
        ctrl.OnCrash    += () => RemoveTrackedCar(newCar, trackingList);

        return ctrl;
    }

    void RemoveTrackedCar(GameObject car, List<GameObject> trackingList)
    {
        if (car == null) return;
        trackingList?.Remove(car);

        var ctrl = car.GetComponent<TrafficCarWaypoints>();
        if (ctrl == null || !ctrl.IsCrashed)
            Destroy(car);
        // Если разбита — уничтожится сама через destroyDelay
    }
}
