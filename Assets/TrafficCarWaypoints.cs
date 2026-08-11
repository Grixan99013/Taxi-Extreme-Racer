using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Движение трафикового автомобиля по вейпоинтам + система "поезда":
/// машина может следовать за лидером (другим TrafficCarWaypoints) вместо вейпоинтов.
///
/// СИСТЕМА ПОЕЗДА:
///   Назначьте trainLeader — машина будет держать дистанцию позади лидера.
///   TrafficSpawner вызывает SetupTrain() при спавне колонны.
///
/// ИСПРАВЛЕНИЯ КОЛЁС:
///   - Скорость передаётся как линейная (м/с), WheelController сам переводит в °/с через радиус.
///   - Угол руления вычисляется правильно: SignedAngle между forward и направлением на цель.
///   - Все вызовы UpdateWheels идут из FixedUpdate, внутри WheelController используется fixedDeltaTime.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(WheelController))]
public class TrafficCarWaypoints : MonoBehaviour
{
    // -----------------------------------------------------------------------
    // Inspector
    // -----------------------------------------------------------------------

    [Header("Маршрут")]
    public List<Transform> waypoints;
    [Range(1f, 10f)]  public float reachDistance   = 3f;
    [Range(5f, 30f)]  public float maxSpeed         = 12f;
    [Range(1f, 10f)]  public float minSpeed         = 3f;
    [Range(0.1f, 5f)] public float acceleration     = 2f;
    [Range(1f, 10f)]  public float rotationSpeed    = 5f;

    [Header("Столкновения и уничтожение")]
    public float    destroyDelay    = 15f;
    public LayerMask collisionLayers;

    [Header("Обнаружение препятствий")]
    [Range(5f, 30f)]  public float detectionDistance = 15f;
    [Range(1f, 10f)]  public float stopDistance      = 5f;
    [Range(0.5f, 3f)] public float detectionWidth    = 2f;
    public LayerMask obstacleLayers = ~0;
    [Range(0.1f, 2f)] public float brakeForce        = 1f;

    // -----------------------------------------------------------------------
    // Система поезда
    // -----------------------------------------------------------------------

    [Header("Поезд (колонна)")]
    [Tooltip("Лидер колонны. Если null — машина едет по вейпоинтам самостоятельно.")]
    public TrafficCarWaypoints trainLeader;

    [Tooltip("Желаемая дистанция до лидера в метрах.")]
    [Range(3f, 20f)] public float trainFollowDistance = 8f;

    [Tooltip("Максимальное рассогласование (буфер) дистанции, в котором машина не ускоряется и не тормозит.")]
    [Range(0.5f, 5f)] public float trainDistanceBuffer = 2f;

    // -----------------------------------------------------------------------
    // Публичные свойства
    // -----------------------------------------------------------------------

    public bool IsCrashed { get; private set; }

    /// <summary>Текущая линейная скорость (м/с). Доступна ведомым машинам для синхронизации.</summary>
    public float CurrentSpeed => currentSpeed;

    // -----------------------------------------------------------------------
    // События
    // -----------------------------------------------------------------------

    public event Action OnReachEnd;
    public event Action OnCrash;

    // -----------------------------------------------------------------------
    // Приватные поля
    // -----------------------------------------------------------------------

    private WheelController wheelController;
    private Rigidbody rb;

    private int   currentWaypointIndex = 0;
    private float currentSpeed         = 0f;
    private float targetSpeed          = 0f;
    private bool  isObstacleDetected   = false;

    // Anti-stuck
    private Vector3 lastPosition;
    private float   stuckTimer = 0f;
    private const float STUCK_THRESHOLD = 0.1f;
    private const float STUCK_TIME      = 5f;

    // -----------------------------------------------------------------------
    // Unity lifecycle
    // -----------------------------------------------------------------------

    void Awake()
    {
        rb              = GetComponent<Rigidbody>();
        wheelController = GetComponent<WheelController>();
        lastPosition    = transform.position;
    }

    void FixedUpdate()
    {
        if (IsCrashed) return;

        CheckIfStuck();

        if (trainLeader != null && !trainLeader.IsCrashed)
        {
            // Режим поезда: следуем за лидером
            FollowLeader();
        }
        else
        {
            // Обычный режим: едем по вейпоинтам
            if (waypoints == null || waypoints.Count == 0) return;
            CheckForObstacles();
            MoveToWaypoint();
            CheckWaypointProximity();
        }

        UpdateWheels();
    }

    // -----------------------------------------------------------------------
    // Публичный API для TrafficSpawner
    // -----------------------------------------------------------------------

    /// <summary>
    /// Настраивает машину как ведомую в поезде.
    /// Вейпоинты при этом копируются с лидера, чтобы машина могла продолжить маршрут,
    /// если лидер уничтожается.
    /// </summary>
    public void SetupAsFollower(TrafficCarWaypoints leader, float followDistance)
    {
        trainLeader         = leader;
        trainFollowDistance = followDistance;

        // Копируем маршрут лидера как запасной
        if (leader.waypoints != null)
            waypoints = new List<Transform>(leader.waypoints);

        // Подписываемся: если лидер разбился — переходим на вейпоинтный маршрут
        leader.OnCrash  += OnLeaderLost;
        leader.OnReachEnd += OnLeaderLost;
    }

    // -----------------------------------------------------------------------
    // Режим поезда
    // -----------------------------------------------------------------------

    void FollowLeader()
    {
        Vector3 toLeader    = trainLeader.transform.position - transform.position;
        float   distToLeader = toLeader.magnitude;

        // Направляемся к лидеру с небольшим смещением назад (держим хвост)
        Vector3 targetPos = trainLeader.transform.position
                            - trainLeader.transform.forward * trainFollowDistance;
        Vector3 direction = (targetPos - transform.position).normalized;

        // Поворот корпуса
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation  = Quaternion.Slerp(
                transform.rotation, targetRot, rotationSpeed * Time.fixedDeltaTime);
        }

        // Скорость: синхронизируемся с лидером, корректируем дистанцию
        float gap = distToLeader - trainFollowDistance;

        if (gap > trainDistanceBuffer)
        {
            // Слишком далеко — ускоряемся, можно чуть быстрее лидера
            targetSpeed = Mathf.Min(trainLeader.CurrentSpeed * 1.2f, maxSpeed);
        }
        else if (gap < -trainDistanceBuffer)
        {
            // Слишком близко — тормозим
            targetSpeed = Mathf.Max(trainLeader.CurrentSpeed * 0.6f, 0f);
        }
        else
        {
            // В зоне комфорта — едем с той же скоростью
            targetSpeed = trainLeader.CurrentSpeed;
        }

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        currentSpeed = Mathf.Max(0f, currentSpeed);
        rb.velocity  = transform.forward * currentSpeed;
    }

    void OnLeaderLost()
    {
        // Лидер исчез — отключаемся и едем сами по скопированному маршруту
        if (trainLeader != null)
        {
            trainLeader.OnCrash   -= OnLeaderLost;
            trainLeader.OnReachEnd -= OnLeaderLost;
            trainLeader = null;
        }
        // currentWaypointIndex уже задан при SetupAsFollower (0), продолжим с начала
    }

    // -----------------------------------------------------------------------
    // Обычный маршрутный режим
    // -----------------------------------------------------------------------

    void CheckForObstacles()
    {
        isObstacleDetected = false;
        Vector3 rayOrigin  = transform.position + Vector3.up * 0.5f;
        Vector3 fwd        = transform.forward;

        Vector3[] offsets =
        {
            Vector3.zero,
            transform.right  *  detectionWidth * 0.5f,
            transform.right  * -detectionWidth * 0.5f,
        };

        float minDist = float.MaxValue;

        foreach (var offset in offsets)
        {
            if (Physics.Raycast(rayOrigin + offset, fwd, out RaycastHit hit,
                                detectionDistance, obstacleLayers))
            {
                if (IsRelevantObstacle(hit.collider.gameObject, out _))
                {
                    isObstacleDetected = true;
                    minDist = Mathf.Min(minDist, hit.distance);
                }
            }
        }

        // OverlapBox для надёжности
        Vector3    boxCenter = transform.position + fwd * (detectionDistance * 0.5f);
        Collider[] cols      = Physics.OverlapBox(
            boxCenter,
            new Vector3(detectionWidth, 1f, detectionDistance) * 0.5f,
            transform.rotation,
            obstacleLayers);

        foreach (var col in cols)
        {
            if (col.transform.IsChildOf(transform) || col.gameObject == gameObject) continue;
            if (IsRelevantObstacle(col.gameObject, out Transform root))
            {
                float d = Vector3.Distance(transform.position, root.position);
                minDist = Mathf.Min(minDist, d);
                isObstacleDetected = true;
            }
        }

        // Вычисляем целевую скорость с учётом препятствия
        float normalSpeed = NormalSpeedToWaypoint();

        if (isObstacleDetected)
        {
            if (minDist < stopDistance)
                targetSpeed = 0f;
            else if (minDist < detectionDistance)
            {
                float t = Mathf.Clamp01((minDist - stopDistance) / (detectionDistance - stopDistance));
                targetSpeed = normalSpeed * t * 0.3f;
            }
            else
                targetSpeed = normalSpeed * 0.5f;
        }
        else
        {
            targetSpeed = normalSpeed;
        }
    }

    float NormalSpeedToWaypoint()
    {
        if (waypoints == null || waypoints.Count == 0) return minSpeed;
        float d = Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position);
        return d > reachDistance * 2f ? maxSpeed : minSpeed;
    }

    bool IsRelevantObstacle(GameObject go, out Transform root)
    {
        root = go.transform.root;
        Сontroler          player  = go.GetComponentInParent<Сontroler>();
        TrafficCarWaypoints traffic = go.GetComponentInParent<TrafficCarWaypoints>();
        if (traffic == this) { root = null; return false; }
        return player != null || (traffic != null && !traffic.IsCrashed);
    }

    void MoveToWaypoint()
    {
        Vector3   direction     = (waypoints[currentWaypointIndex].position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

        float accelRate = isObstacleDetected ? brakeForce : acceleration;
        currentSpeed    = Mathf.Lerp(currentSpeed, targetSpeed, accelRate * Time.fixedDeltaTime);
        currentSpeed    = Mathf.Max(0f, currentSpeed);
        rb.velocity     = transform.forward * currentSpeed;
    }

    void CheckWaypointProximity()
    {
        if (waypoints == null || waypoints.Count == 0) return;
        float d = Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position);
        if (d < reachDistance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            if (currentWaypointIndex == 0) OnReachEnd?.Invoke();
        }
    }

    // -----------------------------------------------------------------------
    // Колёса
    // -----------------------------------------------------------------------

    void UpdateWheels()
    {
        if (wheelController == null) return;

        // Определяем направление к цели
        Vector3 targetDir;
        if (trainLeader != null && !trainLeader.IsCrashed)
        {
            Vector3 targetPos = trainLeader.transform.position
                                - trainLeader.transform.forward * trainFollowDistance;
            targetDir = (targetPos - transform.position).normalized;
        }
        else if (waypoints != null && waypoints.Count > 0)
        {
            targetDir = (waypoints[currentWaypointIndex].position - transform.position).normalized;
        }
        else
        {
            targetDir = transform.forward;
        }

        // Угол между текущим направлением вперёд и направлением к цели по горизонтали
        // SignedAngle даёт корректный знак (+/- = право/лево)
        float steeringAngle = Vector3.SignedAngle(transform.forward, targetDir, Vector3.up);

        // Передаём линейную скорость — WheelController сам переводит в угловую через радиус
        wheelController.UpdateWheels(currentSpeed, steeringAngle);
    }

    // -----------------------------------------------------------------------
    // Anti-stuck
    // -----------------------------------------------------------------------

    void CheckIfStuck()
    {
        if (Vector3.Distance(transform.position, lastPosition) < STUCK_THRESHOLD)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= STUCK_TIME) HandleCrash();
        }
        else
        {
            stuckTimer = 0f;
        }
        lastPosition = transform.position;
    }

    // -----------------------------------------------------------------------
    // Столкновение
    // -----------------------------------------------------------------------

    void OnCollisionEnter(Collision collision)
    {
        if (IsCrashed) return;
        if (((1 << collision.gameObject.layer) & collisionLayers) != 0)
            HandleCrash();
    }

    void HandleCrash()
    {
        if (IsCrashed) return;
        IsCrashed    = true;
        currentSpeed = 0f;

        rb.velocity        = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic     = true;

        foreach (var col in GetComponentsInChildren<Collider>())
            col.enabled = false;

        OnCrash?.Invoke();
        Destroy(gameObject, destroyDelay);
    }
}
