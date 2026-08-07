using UnityEngine;

public class BackgroundMusicManager : MonoBehaviour
{
   public static BackgroundMusicManager Instance;

   private AudioSource audioSource;

   [SerializeField] private AudioClip bgMusic;
   [SerializeField] private float volume = 0.4f;



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
        audioSource.clip = bgMusic;
        audioSource.volume = volume;
        audioSource.playOnAwake = true;
        audioSource.loop = true;
        audioSource.Play();
    }

}
