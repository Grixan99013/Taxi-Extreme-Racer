using UnityEngine;
using UnityEngine.SceneManagement;

public class CarCameraController : MonoBehaviour
{
    public Camera mainCamera;        
    public Camera frontViewCamera;   
    public Camera menuCamera;        
    public Transform carTarget;      
    
    [Header("Menu Camera Orbit Settings")]
    public float rotationSpeed = 5f; 
    public float distance = 5f;      
    public float height = 1.5f;      
    public float minVerticalAngle = -20f; 
    public float maxVerticalAngle = 45f;  
    
    private float currentX = 0f;
    private float currentY = 0f;
    private Vector3 initialOffset;

    // Счётчик, а не bool: если когда-нибудь откроется сразу несколько панелей поверх друг друга
    // (например, задания + подтверждение покупки), камера останется заблокированной,
    // пока не закроется последняя из них.
    private int blockingPanelsOpen = 0;

    /// <summary>
    /// Вызывается UI-панелями (например QuestListUI, OrderHistoryUI) при открытии/закрытии,
    /// чтобы вращение камеры мышью не работало "сквозь" открытую панель.
    /// </summary>
    public void SetCameraRotationBlocked(bool blocked)
    {
        blockingPanelsOpen = Mathf.Max(0, blockingPanelsOpen + (blocked ? 1 : -1));
    }

    private void Start()
    {
 
        if (menuCamera != null && carTarget != null)
        {
            initialOffset = menuCamera.transform.position - carTarget.position;
            distance = initialOffset.magnitude;
            height = initialOffset.y;
            
            Vector3 flatOffset = new Vector3(initialOffset.x, 0, initialOffset.z);
            currentX = Vector3.SignedAngle(Vector3.forward, flatOffset, Vector3.up);
            currentY = Vector3.Angle(flatOffset, initialOffset);
        }


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
        if (blockingPanelsOpen > 0) return;

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


        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);
        Vector3 offset = new Vector3(0, height, -distance);
        Vector3 position = carTarget.position + rotation * offset;


        menuCamera.transform.position = position;
        menuCamera.transform.LookAt(carTarget.position + Vector3.up * height * 0.5f);
    }
}