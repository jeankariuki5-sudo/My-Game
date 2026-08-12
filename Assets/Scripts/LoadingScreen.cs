using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadingScreen : MonoBehaviour
{
    [Tooltip("The scene to load once the logo has been shown (e.g. MainMenu)")]
    [SerializeField] private string sceneToLoad = "MainMenu";

    [Tooltip("How long to display the logo before moving on, in seconds")]
    [SerializeField] private float displayDuration = 2f;

    private void Start()
    {
        StartCoroutine(ShowThenLoad());
    }

    private IEnumerator ShowThenLoad()
    {
        yield return new WaitForSeconds(displayDuration);
        SceneManager.LoadScene(sceneToLoad);
    }
}
