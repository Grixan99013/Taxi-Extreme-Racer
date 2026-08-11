using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CarSelectionManager : MonoBehaviour
{
    [System.Serializable]
    public class CarInfo
    {
        public GameObject carPrefab;
        public string carName;
        public int carPrice;
        public Sprite carImage;
    }

    [Header("Car Settings")]
    public CarInfo[] availableCars;
    public Transform carDisplayPosition;
    
    [Header("UI Elements (TextMeshPro)")]
    public TMP_Text carNameText;
    public TMP_Text carPriceText;
    public TMP_Text selectedText;
    public Image carImageDisplay;
    
    [Header("Buttons")]
    public Button selectButton;
    public Button startButton;
    public Button nextButton;
    public Button prevButton;

    private GameObject currentCarInstance;
    private int selectedCarIndex = -1;

    void Start()
    {
        selectedText.gameObject.SetActive(false);
        startButton.interactable = false;
        
        selectButton.onClick.AddListener(SelectCurrentCar);
        startButton.onClick.AddListener(StartGame);
        nextButton.onClick.AddListener(NextCar);
        prevButton.onClick.AddListener(PreviousCar);

        if (availableCars.Length > 0)
        {
            ShowCar(0);
        }
    }

    public void ShowCar(int index)
    {
        if (currentCarInstance != null)
        {
            Destroy(currentCarInstance);
        }

        selectedCarIndex = index;
        currentCarInstance = Instantiate(availableCars[index].carPrefab, carDisplayPosition);
        currentCarInstance.transform.localPosition = Vector3.zero;
        currentCarInstance.transform.localRotation = Quaternion.identity;
        
        // Вызываем метод конфигурации для меню
        ConfigureCarForMenu(currentCarInstance);

        carNameText.text = availableCars[index].carName;
        carPriceText.text = $"Цена: {availableCars[index].carPrice}$";
        carImageDisplay.sprite = availableCars[index].carImage;

        selectedText.gameObject.SetActive(false);
        selectButton.interactable = true;
        
        UpdateNavigationButtons();
    }

    void UpdateNavigationButtons()
    {
        prevButton.interactable = selectedCarIndex > 0;
        nextButton.interactable = selectedCarIndex < availableCars.Length - 1;
    }

    public void SelectCurrentCar()
    {
        if (selectedCarIndex == -1) return;

        PlayerPrefs.SetInt("SelectedCarIndex", selectedCarIndex);
        CarDataHolder.Instance.SetSelectedCar(availableCars[selectedCarIndex].carPrefab);
        
        selectedText.gameObject.SetActive(true);
        selectButton.interactable = false;
        startButton.interactable = true;

        carNameText.fontStyle = FontStyles.Bold;
        carPriceText.color = Color.green;
    }

    public void StartGame()
    {
        if (selectedCarIndex == -1) return;
        SceneManager.LoadScene("GameScene");
    }

    public void NextCar()
    {
        if (availableCars.Length == 0) return;
        int newIndex = Mathf.Min(selectedCarIndex + 1, availableCars.Length - 1);
        ShowCar(newIndex);
    }

    public void PreviousCar()
    {
        if (availableCars.Length == 0) return;
        int newIndex = Mathf.Max(selectedCarIndex - 1, 0);
        ShowCar(newIndex);
    }

    void ConfigureCarForMenu(GameObject carInstance)
    {
        // Отключаем физику
        Rigidbody rb = carInstance.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.detectCollisions = false;
        }

        

        // Отключаем UI элементы машины
        Canvas[] canvases = carInstance.GetComponentsInChildren<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            canvas.enabled = false;
        }
    }
}