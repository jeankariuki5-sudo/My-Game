using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Tooltip("Name of the scene to load when starting the game")]
    public string gameSceneName = "GameScene";


    // Load the game scene
    public void StartGame()
    {
        // Check if the scene name is set to avoid errors
        if (!string.IsNullOrEmpty(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
        else
        {
            Debug.LogError("Game Scene name is not set in the main menu");
        }
    }

    // This quits the application and it stops the playing mode
    public void QuitGame()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

}