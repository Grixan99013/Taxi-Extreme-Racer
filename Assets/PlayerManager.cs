using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    
    [Header("UI References — назначить в инспекторе")]
    [Tooltip("TMP текст баланса на HUD. Назначить вручную — не ищется по имени объекта.")]
    [SerializeField] public TextMeshProUGUI BalanceText;
    [Tooltip("Слайдер опыта на HUD. Назначить вручную.")]
    [SerializeField] public Slider ExpSlider;
    
    [SerializeField] private float currentRating;
    [SerializeField] private int currentExp;
    [SerializeField] private int maxExp = 1000;
    private List<int> ratingHistory = new List<int>();
    public int Balance { get; private set; }

    public float CurrentRating { get { return currentRating; } }
    public int CurrentExp { get { return currentExp; } }


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAllData();
            // Подписываемся на событие загрузки сцены, чтобы переподключать UI.
            // PlayerManager живёт вечно, но BalanceText/ExpSlider принадлежат
            // конкретной сцене и уничтожаются при переходе.
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// Вызывается автоматически при каждой загрузке сцены.
    /// Ищет UI-элементы текущей сцены по тегу и обновляет ссылки.
    /// На объектах BalanceText и ExpSlider в каждой сцене выставь теги:
    ///   BalanceText  -> тег "BalanceText"
    ///   ExpSlider    -> тег "ExpSlider"
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ReconnectUI();
    }

    /// <summary>
    /// Переподключает ссылки на UI текущей сцены.
    /// Вызывается автоматически при смене сцены, но можно вызвать вручную
    /// из любого скрипта сцены через PlayerManager.Instance.ReconnectUI().
    /// </summary>
    public void ReconnectUI()
    {
        // Ищем по тегу — быстро и не зависит от имени объекта
        GameObject balanceObj = GameObject.FindWithTag("BalanceText");
        if (balanceObj != null)
            BalanceText = balanceObj.GetComponent<TextMeshProUGUI>();
        else
            BalanceText = null;

        GameObject sliderObj = GameObject.FindWithTag("ExpSlider");
        if (sliderObj != null)
            ExpSlider = sliderObj.GetComponent<Slider>();
        else
            ExpSlider = null;

        // Сразу обновляем UI с актуальными данными
        UpdateBalanceText();
        UpdateExpUI();

        Debug.Log($"[PlayerManager] ReconnectUI: BalanceText={(BalanceText != null ? "OK" : "null")}, ExpSlider={(ExpSlider != null ? "OK" : "null")}");
    }

    private void Start()
    {
        // Start вызывается только один раз (при первой сцене).
        // При последующих сценах UI переподключается через OnSceneLoaded.
        ReconnectUI();
    }

    private void LoadAllData()
    {
        LoadBalance();
        LoadRating();
        LoadExpData();
    }

    public void AddBalance(int amount)
    {
        Balance += amount;
        UpdateBalanceText();
        Debug.Log($"Balance increased by {amount}. New balance: {Balance}");
    }

    public void SaveBalance()
    {
        PlayerPrefs.SetInt("PlayerBalance", Balance);
        Debug.Log("Balance saved.");
    }

    public void LoadBalance()
    {
        if (PlayerPrefs.HasKey("PlayerBalance"))
        {
            Balance = PlayerPrefs.GetInt("PlayerBalance");
            UpdateBalanceText();
            Debug.Log("Balance loaded.");
        }
        else
        {
            Debug.Log("No saved balance found. Using default balance.");
        }
    }

    private void OnApplicationQuit()
    {
        SaveBalance();
        SaveAllData();
    }

    public void ResetBalance()
    {
        Balance = 0;
        UpdateBalanceText();
        Debug.Log("Balance reset.");
    }


    private void UpdateBalanceText()
    {
        if (BalanceText != null)
            BalanceText.text = Balance.ToString();
    }

    public void AddRating(int rating)
    {
        int clampedRating = Mathf.Clamp(rating, 1, 5);
        ratingHistory.Add(clampedRating);
        
        Debug.Log($"Добавлена оценка: {clampedRating}");
        CalculateRating();
        SaveRating();
    }

    private void CalculateRating()
    {
        if (ratingHistory.Count == 0)
        {
            currentRating = 5f;
            return;
        }
        
        float sum = 0f;
        foreach (int rating in ratingHistory)
        {
            sum += rating;
        }
        currentRating = sum / ratingHistory.Count;
    }

    public void SaveRating()
    {
        PlayerPrefs.SetString("RatingHistory", string.Join(",", ratingHistory));
        PlayerPrefs.Save();
    }

    public void LoadRating()
    {
        if (PlayerPrefs.HasKey("RatingHistory"))
        {
            string[] ratings = PlayerPrefs.GetString("RatingHistory").Split(',');
            foreach (string rating in ratings)
            {
                if (int.TryParse(rating, out int r))
                {
                    ratingHistory.Add(r);
                }
            }
            CalculateRating();
        }
    }

    public void SaveExpData()
    {
        PlayerPrefs.SetInt("PlayerExp", currentExp);
        PlayerPrefs.SetInt("MaxExp", maxExp);
    }

    public void LoadExpData()
    {
        if (PlayerPrefs.HasKey("PlayerExp"))
        {
            currentExp = PlayerPrefs.GetInt("PlayerExp");
            maxExp = PlayerPrefs.GetInt("MaxExp", 1000);
            UpdateExpUI();
        }
    }

     private void UpdateExpUI()
    {
        if (ExpSlider != null)
        {
            ExpSlider.maxValue = maxExp;
            ExpSlider.value = currentExp;
        }
    }

    public void AddExp(int amount)
    {
        currentExp = Mathf.Clamp(currentExp + amount, 0, maxExp);
        UpdateExpUI();
        SaveExpData();
        Debug.Log($"Added {amount} EXP. Total: {currentExp}/{maxExp}");
    }

    public void ResetExp()
    {
        currentExp = 0;
        UpdateExpUI();
        SaveExpData();
    }
    public void SaveAllData()
    {
        SaveBalance();
        SaveRating();
        SaveExpData();
    }
}