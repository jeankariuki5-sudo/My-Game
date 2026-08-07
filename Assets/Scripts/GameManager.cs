using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] Button restartButton;

    private bool gameRunning;

    [Header("Score Settings")]
    [SerializeField] TextMeshProUGUI lastWaveText;
    [SerializeField] TextMeshProUGUI highScoreText;
    [SerializeField] GameObject newHighScoreMessage;
    [SerializeField] TextMeshProUGUI highScoreValue;

    private int highScore = 0;

    public static GameManager Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    // subscribe to player died event
    private void OnEnable()
    {
        GameEvents.OnPlayerDied += HandlePlayerDied;
        GameEvents.OnHighScoreUpdated += HandleHighScoreUpdated;
    }

    // Unsubscribe to avoid memeory leaks
    private void OnDisable()
    {
        GameEvents.OnPlayerDied -= HandlePlayerDied;
        GameEvents.OnHighScoreUpdated -= HandleHighScoreUpdated;
    }


    void Start()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
    
        }
        // Function to allow loading previous highscore
        // PlayerPrefs.DeleteKey("HighScore");
        // PlayerPrefs.Save();

        LoadHighScore();
        LoadLastWave();


        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        // Check if highscore message is available
        if (newHighScoreMessage != null)
        {
            newHighScoreMessage.SetActive(false);
        }

        // Show Higscore on ui immediately
        if (highScoreText != null)
        {
            highScoreText.text = ": " + highScore;
        }

        if (highScoreValue != null)
        {
            highScoreValue.text = "" + highScore;
        }

    }

    void Update()
    {
        LoadHighScore();
        LoadHighScoreValue();
    }

    void RestartGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void LoadHighScore()
    {
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (highScoreText != null)
            highScoreText.text = "" + highScore;
        Debug.Log("Highscore Loaded");
    }

    private void LoadHighScoreValue()
    {
        if (highScoreValue != null)
        {
            highScoreValue.text = "" + highScore;
        }
    }

    // Method to save Highscore in playerf prefs
    public void SaveHighScore(int currentScore)
    {
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
            if (newHighScoreMessage != null)
            {
                newHighScoreMessage.SetActive(true);
            }
        }
    }

    public bool isGameRunning()
    {
        return gameRunning;
    }

    public void GameOver()
    {
        gameRunning = false;
        gameOverPanel.SetActive(true);

        // Get last seave from wavemanager
        int finalWave = WaveManager.Instance != null ? WaveManager.Instance.GetCurrentWave() : 0;
        SaveLastWave(finalWave);

        GameEvents.GameOver();
    }

    private void HandlePlayerDied()
    {
        GameOver();
    }

    private void HandleHighScoreUpdated(int score)
    {
        if (newHighScoreMessage != null)
            newHighScoreMessage.SetActive(true);
    }

    public void SaveLastWave(int finalWave)
    {
        PlayerPrefs.SetInt("LastWave", finalWave);
        PlayerPrefs.Save();
        lastWaveText.text = ": " + finalWave;
    }

    void LoadLastWave()
    {
        int lastWave = PlayerPrefs.GetInt("LastWave", 0);
        lastWaveText.text = "" + lastWave;
    }

}
