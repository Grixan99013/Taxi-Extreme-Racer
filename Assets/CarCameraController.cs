using UnityEngine;
using UnityEngine.SceneManagement;

public class CarCameraController : MonoBehaviour
{
    public Camera mainCamera;        // Основная камера (обычный вид)
    public Camera frontViewCamera;   // Камера вида спереди
    public Camera menuCamera;        // Камера для меню
    public Transform carTarget;      // Цель (машина), вокруг которой вращаемся
    
    [Header("Menu Camera Orbit Settings")]
    public float rotationSpeed = 5f; // Скорость вращения
    public float distance = 5f;      // Дистанция от камеры до машины
    public float height = 1.5f;      // Высота камеры относительно машины
    public float minVerticalAngle = -20f; // Минимальный угол наклона
    public float maxVerticalAngle = 45f;  // Максимальный угол наклона
    
    private float currentX = 0f;
    private float currentY = 0f;
    private Vector3 initialOffset;

    private void Start()
    {
        // Инициализация углов вращения
        if (menuCamera != null && carTarget != null)
        {
            initialOffset = menuCamera.transform.position - carTarget.position;
            distance = initialOffset.magnitude;
            height = initialOffset.y;
            
            // Вычисляем начальные углы
            Vector3 flatOffset = new Vector3(initialOffset.x, 0, initialOffset.z);
            currentX = Vector3.SignedAngle(Vector3.forward, flatOffset, Vector3.up);
            currentY = Vector3.Angle(flatOffset, initialOffset);
        }

        // Автоматически определяем режим при старте
        bool isMenuScene = SceneManager.GetActiveScene().name == "CarMenu";
        SetMenuMode(isMenuScene);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool isMenuScene = scene.name == "CarMenu";
        SetMenuMode(isMenuScene);
    }

    public void SetMenuMode(bool menuActive)
    {
        if (menuActive)
        {
            if (menuCamera != null) 
            {
                menuCamera.enabled = true;
                UpdateMenuCameraPosition();
            }
            if (mainCamera != null) mainCamera.enabled = false;
            if (frontViewCamera != null) frontViewCamera.enabled = false;
        }
        else
        {
            if (menuCamera != null) menuCamera.enabled = false;
            if (mainCamera != null) mainCamera.enabled = true;
            if (frontViewCamera != null) frontViewCamera.enabled = false;
        }
    }

    private void Update()
    {
        if (menuCamera != null && menuCamera.enabled)
        {
            HandleMenuCameraRotation();
            return;
        }

        if (Input.GetKey(KeyCode.B))
        {
            if (mainCamera != null) mainCamera.enabled = false;
            if (frontViewCamera != null) frontViewCamera.enabled = true;
        }
        else
        {
            if (mainCamera != null) mainCamera.enabled = true;
            if (frontViewCamera != null) frontViewCamera.enabled = false;
        }
    }

    private void HandleMenuCameraRotation()
    {
        if (carTarget == null) return;

        if (Input.GetMouseButton(0))
        {
            currentX += Input.GetAxis("Mouse X") * rotationSpeed;
            currentY -= Input.GetAxis("Mouse Y") * rotationSpeed;
            currentY = Mathf.Clamp(currentY, minVerticalAngle, maxVerticalAngle);
        }

        UpdateMenuCameraPosition();
    }

    private void UpdateMenuCameraPosition()
    {
        if (carTarget == null) return;

        // Вычисляем новую позицию камеры
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 offset = new Vector3(0, height, -distance);
        Vector3 position = carTarget.position + rotation * offset;

        // Обновляем позицию и поворот камеры
        menuCamera.transform.position = position;
        menuCamera.transform.LookAt(carTarget.position + Vector3.up * height * 0.5f);
    }
}