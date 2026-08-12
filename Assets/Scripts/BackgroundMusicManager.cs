using UnityEngine;
using UnityEngine.SceneManagement;

public class BackgroundMusicManager : MonoBehaviour
{
   public static BackgroundMusicManager Instance;

   private AudioSource audioSource;

   [Header("Music Clips")]
   [SerializeField] private AudioClip mainMenuMusic;
   [SerializeField] private AudioClip gameplayMusic;
   [SerializeField] private AudioClip gameOverMusic;

   [SerializeField] private float volume = 0.4f;

   [Header("Scene Names (must match Build Settings exactly)")]
   [SerializeField] private string mainMenuSceneName = "MainMenu";
   [SerializeField] private string gameSceneName = "GameScene";



   void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.volume = volume;
        audioSource.playOnAwake = false;
        audioSource.loop = true;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        GameEvents.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        GameEvents.OnGameOver -= HandleGameOver;
    }

    private void Start()
    {
        // play the correct track for whichever scene this object was created in
        HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
    }

    // Fires every time a scene finishes loading - including MainMenu -> GameScene,
    // and GameScene -> GameScene on restart (since RestartGame reloads the scene).
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == mainMenuSceneName)
        {
            PlayClip(mainMenuMusic);
        }
        else if (scene.name == gameSceneName)
        {
            PlayClip(gameplayMusic);
        }
    }

    // Game over happens inside GameScene (it's a UI panel, not a scene change),
    // so it needs its own event rather than relying on a scene load.
    private void HandleGameOver()
    {
        PlayClip(gameOverMusic);
    }

    private void PlayClip(AudioClip clip)
    {
        if (clip == null || audioSource == null) return;
        if (audioSource.clip == clip && audioSource.isPlaying) return; // don't restart a track that's already playing

        audioSource.clip = clip;
        audioSource.Play();
    }

}
