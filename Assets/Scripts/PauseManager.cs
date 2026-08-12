using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Resume UI")]
    [SerializeField] private GameObject pauseMenuUI;
    public MonoBehaviour playerScript;
    private bool lockCursorOnResume = true;
    private bool isPaused = false;

    public static PauseManager Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public void Update()
    {
        // Don't let Escape toggle pause once the game is over - a second press would
        // call ResumeGame() and set Time.timeScale back to 1, un-freezing everything
        // GameManager just froze behind the Game Over screen.
        if (GameManager.Instance != null && !GameManager.Instance.isGameRunning()) return;

       if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePaused();
        }
    }

    public void TogglePaused()
    {
         if (isPaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        if(pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);

        }

        if (playerScript != null)
        {
            playerScript.enabled = false;
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

    }

    public void ResumeGame()
    {
       isPaused = false;
       Time.timeScale = 1f;

        // Hide the pause menu
        if(pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }

        // re enable the player script
        if(playerScript != null)
        {
            playerScript.enabled = true;
        }

        if (lockCursorOnResume)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void QuitMainMenu(string sceneName = "MainMenu")
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif

    }

    public bool IsPaused() => isPaused;


}