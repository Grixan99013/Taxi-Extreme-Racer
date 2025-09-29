using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance { get; private set; }
    
    [Header("UI References")]
    public TextMeshProUGUI BalanceText;
    public Slider ExpSlider;
    
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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (BalanceText == null || ExpSlider == null)
        {
            FindUIElements();
        }
        UpdateBalanceText();
        UpdateExpUI();
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

    private void FindUIElements()
    {
        var texts = FindObjectsOfType<TextMeshProUGUI>();
        foreach (var text in texts)
        {
            if (text.name == "Balance")
            {
                BalanceText = text;
                BalanceText.fontSize = 28f;
                BalanceText.color = Color.black;
            }
        }

        var sliders = FindObjectsOfType<Slider>();
        foreach (var slider in sliders)
        {
            if (slider.name == "ExpSlider")
            {
                ExpSlider = slider;
            }
        }
    }

    private void UpdateBalanceText()
    {
        if (BalanceText != null)
        {
            BalanceText.text = Balance.ToString();
        }
        else
        {
            FindUIElements();
            if (BalanceText != null)
            {
                BalanceText.text = Balance.ToString();
            }
        }
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