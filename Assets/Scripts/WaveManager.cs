using System.Collections;

using TMPro;
using UnityEngine;



public class WaveManager : MonoBehaviour

{

    [Header("UI")]

    [SerializeField] private TextMeshProUGUI timeText;

    [SerializeField] private TextMeshProUGUI waveText;

    [SerializeField] private TextMeshProUGUI enemiesLeftText; // optional




    [Header("Wave Settings")]

    [SerializeField] private int baseEnemyCount = 5;

    [SerializeField] private int extraEnemiesPerWave = 3;

    [SerializeField] private float baseSpawnInterval = 0.8f;

    [SerializeField] private float spawnIntervalDecrease = 0.05f;

    [SerializeField] private float waveDuration = 30f; // seconds

    [SerializeField] private float nextWaveDelay = 3f; // countdown before next wave



    public static WaveManager Instance;



    private bool waveRunning = false;

    private int currentWave = 0;

    private float currentWaveTime;

    private bool isCountdownActive = false; // prevent overlapping countdowns



    [Header("Shop UI")]
    [SerializeField] private ShopManager shopManager;



    private void Awake()

    {

        if (Instance == null) Instance = this;

    }



    private void Start()

    {

        StartNewWave();

    }

    // Subscribe to wave cleared event
    private void OnEnable()
    {
        GameEvents.OnWaveCleared += HandleWaveCleared;
    }

    // Unsubscribe to wave cleared event if wave managed is not enabled (avoid memory leaks)
    private void OnDisable()
    {
        GameEvents.OnWaveCleared -= HandleWaveCleared;
    }


    // for testing

    // private void Update()

    // {

    // if (Input.GetKeyDown(KeyCode.Space))

    // StartNewWave();

    // }



    public bool WaveRunning() => waveRunning;



    private void StartNewWave()

    {

        // Don't start if player is dead

        Player player = FindObjectOfType<Player>();

        if (player == null || player.IsDead())

        {

            Debug.Log("Player is dead – cannot start new wave.");

            return;

        }



        StopAllCoroutines();

        isCountdownActive = false;

        timeText.color = Color.white;



        currentWave++;

        waveRunning = true;

        // Save Highscore on wave start
        GameManager gameManager = FindObjectOfType<GameManager>();
        if (gameManager != null)
        {
            gameManager.SaveLastWave(currentWave);
        }
        else
        {
            Debug.LogError("GameManager not Found");
        }



        // Difficulty scaling

        int enemyCount = baseEnemyCount + (currentWave - 1) * extraEnemiesPerWave;

        float spawnInterval = Mathf.Max(

        baseSpawnInterval - (currentWave - 1) * spawnIntervalDecrease,

        0.15f

        );



        EnemyManager.Instance.StartWave(enemyCount, spawnInterval);



        currentWaveTime = waveDuration;

        waveText.text = ": " + currentWave;

        StartCoroutine(WaveTimer());

    }



    private IEnumerator WaveTimer()

    {

        while (waveRunning)

        {

            yield return new WaitForSeconds(1f);

            currentWaveTime--;

            timeText.text = Mathf.CeilToInt(currentWaveTime).ToString();



            if (enemiesLeftText != null)

                enemiesLeftText.text = ": " + EnemyManager.Instance.GetAliveCount();



            if (currentWaveTime <= 0)

            {

                WaveComplete(false); // timeout

            }

        }

    }



    /// <summary>

    /// Called when wave ends – either all enemies killed (success) or timer runs out (fail).

    /// </summary>

    public void WaveComplete(bool success)

    {

        if (!waveRunning) return;



        StopAllCoroutines();

        waveRunning = false;



        if (success)
        {
            StartCoroutine(ShowWaveClearedText());
        }

        else

        {

            // Time ran out – destroy remaining enemies and show fail

            EnemyManager.Instance.DestroyAllEnemies();

            timeText.text = "✘";

            timeText.color = Color.red;

            // Start next wave after a short delay (no countdown)


            // Open shop 
            if (shopManager != null)
            {
                shopManager.OpenShop(OnShopClosed);
            }
            Invoke(nameof(StartNewWave), 2f);


        }

    }

    public int GetCurrentWave() => currentWave;

    private void OnShopClosed()
    {
        if (isCountdownActive)
        {
            StopCoroutine(NextWaveCountdown());
            isCountdownActive = false;
        }
        StartCoroutine(NextWaveCountdown());
    }



    /// <summary>

    /// Shows a 3‑2‑1 countdown before starting the next wave.

    /// </summary>

    private IEnumerator NextWaveCountdown()

    {

        isCountdownActive = true;



        // Display countdown from nextWaveDelay down to 1

        float countdown = nextWaveDelay;

        while (countdown > 0)

        {

            timeText.text = Mathf.CeilToInt(countdown).ToString();

            timeText.color = Color.yellow;

            yield return new WaitForSeconds(1f);

            countdown--;

        }



        timeText.text = "GO!";

        timeText.color = Color.green;

        yield return new WaitForSeconds(0.5f);



        isCountdownActive = false;

        StartNewWave();

    }


    private IEnumerator ShowWaveClearedText()
    {
        if (enemiesLeftText != null)
            enemiesLeftText.text = "";

        timeText.text = $"Wave {currentWave} Cleared!";
        timeText.color = Color.green;

        yield return new WaitForSeconds(1.5f); // how long the message stays up

        if (shopManager != null)
        {
            shopManager.OpenShop(OnShopClosed);
        }
        else
        {
            StartCoroutine(NextWaveCountdown());
        }
    }

    public void HandleWaveCleared()
    {
        WaveComplete(true);
    }

}
