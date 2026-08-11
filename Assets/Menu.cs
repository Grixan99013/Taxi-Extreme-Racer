using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class Menu : MonoBehaviour
{
    public Button exitButton;
    public Button startButton;

    void Start()
    {
        startButton.onClick.AddListener(OpenGame);
        exitButton.onClick.AddListener(QuitGame);
    }

    void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
    void OpenGame()
    {
        SceneManager.LoadScene("CarMenu");
        Time.timeScale = 1f;
    }
}
