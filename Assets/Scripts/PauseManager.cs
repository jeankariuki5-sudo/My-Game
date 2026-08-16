using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    [Header("Resume UI")]
    [SerializeField] private GameObject pauseMenuUI;
    public MonoBehaviour playerScript;
    // This game has no mouse-based aiming, so nothing ever needs the cursor locked/hidden -
    // resuming should just restore the same visible/unlocked state the game starts in.
    // Left as a toggle in case that ever changes, but defaults to off so out-of-the-box
    // behavior is consistent instead of only hiding the cursor after the first resume.
    [SerializeField] private bool lockCursorOnResume = false;
    private bool isPaused = false;

    [Header("Quit to Main Menu")]
    // A serialized field instead of a method parameter - Unity's OnClick() Inspector
    // wiring doesn't respect C# default parameter values, so a button wired to a
    // string-argument method with the field left blank silently loads scene "" and
    // does nothing. This can't fall into that trap.
    [SerializeField] private string mainMenuSceneName = "MainMenu";

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

        // Only actually unfreeze time if nothing else needs it frozen - the shop being
        // open, or the game being over. Without this, resuming from pause while the shop
        // is still open (or after death) would silently restart gameplay in the background
        // even though the shop/game-over panel is still on screen.
        bool shopStillOpen = ShopManager.Instance != null && ShopManager.Instance.IsShopOpen();
        bool gameIsOver = GameManager.Instance != null && !GameManager.Instance.isGameRunning();
        if (!shopStillOpen && !gameIsOver)
        {
            Time.timeScale = 1f;
        }

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

        // Only lock/hide the cursor if we're actually returning to gameplay - not if the
        // shop or a game-over panel is still up and needs the cursor visible for clicking.
        if (lockCursorOnResume && !shopStillOpen && !gameIsOver)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void QuitMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
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