using UnityEngine;
using System.Collections.Generic;
using System;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(WheelController))]
public class TrafficCarWaypoints : MonoBehaviour
{
    [Header("Movement Settings")]
    public List<Transform> waypoints;
    [Range(1f, 10f)] public float reachDistance = 3f;
    [Range(5f, 30f)] public float maxSpeed = 12f;
    [Range(1f, 10f)] public float minSpeed = 3f;
    [Range(0.1f, 5f)] public float acceleration = 2f;
    [Range(1f, 10f)] public float rotationSpeed = 5f;

    [Header("Collision Settings")]
    public float destroyDelay = 15f;
    public LayerMask collisionLayers;

    [Header("Wheel Settings")]
    [Range(0.1f, 1f)] public float speedToWheelRotation = 0.3f;

    // Events
    public event Action OnReachEnd;
    public event Action OnCrash;

    // Components
    private WheelController wheelController;
    private Rigidbody rb;
    
    // Runtime variables
    private int currentWaypointIndex = 0;
    private float currentSpeed = 0f;
    public bool IsCrashed { get; private set; }
    private Vector3 lastPosition;
    private float stuckTimer = 0f;
    private const float STUCK_THRESHOLD = 0.1f;
    private const float STUCK_TIME = 5f;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        wheelController = GetComponent<WheelController>();
        lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        if (IsCrashed || waypoints == null || waypoints.Count == 0) return;

        CheckIfStuck();
        MoveToWaypoint();
        UpdateWheels();
        CheckWaypointProximity();
    }

    void MoveToWaypoint()
    {
        Vector3 direction = (waypoints[currentWaypointIndex].position - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

        float distance = Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position);
        float targetSpeed = distance > reachDistance * 2f ? maxSpeed : minSpeed;
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);
        
        rb.velocity = transform.forward * currentSpeed;
    }

    void UpdateWheels()
    {
        if (wheelController == null) return;

        Vector3 direction = (waypoints[currentWaypointIndex].position - transform.position).normalized;
        float steeringAngle = Vector3.SignedAngle(transform.forward, direction, Vector3.up);
        wheelController.UpdateWheels(currentSpeed * speedToWheelRotation, steeringAngle);
    }

    void CheckWaypointProximity()
    {
        float distance = Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position);
        if (distance < reachDistance)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            if (currentWaypointIndex == 0) OnReachEnd?.Invoke();
        }
    }

    void CheckIfStuck()
    {
        if (Vector3.Distance(transform.position, lastPosition) < STUCK_THRESHOLD)
        {
            stuckTimer += Time.fixedDeltaTime;
            if (stuckTimer >= STUCK_TIME)
            {
                HandleCrash();
            }
        }
        else
        {
            stuckTimer = 0f;
        }
        lastPosition = transform.position;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (IsCrashed) return;
        
        if (((1 << collision.gameObject.layer) & collisionLayers) != 0)
        {
            HandleCrash();
        }
    }

    void HandleCrash()
    {
        if (IsCrashed) return;
        
        IsCrashed = true;
        currentSpeed = 0f;
        acceleration = 0f;
        
        // Полная остановка физики
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        
        // Отключаем все коллайдеры
        foreach(var collider in GetComponentsInChildren<Collider>())
        {
            collider.enabled = false;
        }
        
        Destroy(gameObject, destroyDelay);
    }

}