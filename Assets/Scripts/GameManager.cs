using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] Button restartButton;

    private bool gameRunning;

    [Header("Score Settings")]
    [SerializeField] TextMeshProUGUI lastWaveText;
    [SerializeField] TextMeshProUGUI highScoreText;

    [Header("New High Score Flow")]
    [Tooltip("Shown instead of the normal restart flow when the player beats their high score")]
    [SerializeField] private GameObject newHighScorePanel;
    [SerializeField] private Button playAgainButton;
    [Tooltip("The high score number, shown on the New High Score panel itself")]
    [SerializeField] private TextMeshProUGUI highScoreValue;
    [Tooltip("How long the Game Over panel stays up before switching to the New High Score panel")]
    [SerializeField] private float newHighScoreDisplayDelay = 5f;

    private bool achievedNewHighScoreThisRun = false;

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
    }

    // Unsubscribe to avoid memeory leaks
    private void OnDisable()
    {
        GameEvents.OnPlayerDied -= HandlePlayerDied;
    }


    void Start()
    {
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
    
        }

        if (playAgainButton != null)
        {
            playAgainButton.onClick.AddListener(RestartGame);
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

        if (newHighScorePanel != null)
        {
            newHighScorePanel.SetActive(false);
        }

        // Show Higscore on ui immediately
        if (highScoreText != null)
        {
            highScoreText.text = ": " + highScore;
        }

        gameRunning = true;

    }

    void Update()
    {
        LoadHighScore();
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
            achievedNewHighScoreThisRun = true;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
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

        // Freeze gameplay - without this, enemies keep spawning/moving and the wave
        // timer keeps ticking behind the Game Over panel. RestartGame() sets this back
        // to 1 when the player restarts.
        Time.timeScale = 0f;

        // Hide the normal restart button when a new high score was achieved - that flow
        // continues into the dedicated New High Score panel instead, after a short delay.
        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(!achievedNewHighScoreThisRun);
        }

        // Get last seave from wavemanager
        int finalWave = WaveManager.Instance != null ? WaveManager.Instance.GetCurrentWave() : 0;
        SaveLastWave(finalWave);

        if (achievedNewHighScoreThisRun)
        {
            StartCoroutine(ShowNewHighScorePanelAfterDelay());
        }

        GameEvents.GameOver();
    }

    private IEnumerator ShowNewHighScorePanelAfterDelay()
    {
        // Realtime rather than scaled time, in case Time.timeScale is ever 0 at this point
        yield return new WaitForSecondsRealtime(newHighScoreDisplayDelay);

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (newHighScorePanel != null)
        {
            newHighScorePanel.SetActive(true);
        }

        // highScoreValue lives under the New High Score panel, so this is the first
        // moment it becomes visible - update it here rather than every frame during play
        LoadHighScoreValue();

        // Cue the high-score jingle exactly when the panel appears, not when the score
        // was first recorded - those two moments are newHighScoreDisplayDelay apart
        GameEvents.HighScoreUpdated(highScore);
    }

    private void HandlePlayerDied()
    {
        GameOver();
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