using System.Collections;

using TMPro;
using UnityEngine;
using UnityEngine.UI;



public class WaveManager : MonoBehaviour

{

    [Header("UI")]

    [SerializeField] private TextMeshProUGUI timeText;

    [SerializeField] private TextMeshProUGUI waveText;

    [SerializeField] private TextMeshProUGUI enemiesLeftText; // optional

    [Header("Timer Icon")]
    [SerializeField] private Image stopwatchIcon; // swaps sprite + pulses when time is low
    [SerializeField] private Sprite stopwatchYellowSprite;
    [SerializeField] private Sprite stopwatchRedSprite;
    [SerializeField] private float lowTimeThreshold = 5f; // seconds remaining that triggers red + pulse
    [SerializeField] private float pulseSpeed = 6f; // higher = faster pulse
    [SerializeField] private float pulseScaleAmount = 0.15f; // how much bigger it gets at the peak of each pulse (0.15 = 15%)




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

    private bool stopwatchIsRed = false; // tracks current sprite so we only swap once per state change



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


    [Header("Cheat Codes (for testing - toggle off before a real build)")]
    [SerializeField] private bool enableCheatCodes = true;
    [SerializeField] private KeyCode skipWaveKey = KeyCode.F1;
    [SerializeField] private KeyCode skipToBossKey = KeyCode.F2;
    [SerializeField] private KeyCode addCoinsKey = KeyCode.F3;
    [SerializeField] private int cheatCoinsAmount = 100;
    [SerializeField] private KeyCode addScoreKey = KeyCode.F4;
    [SerializeField] private int cheatScoreAmount = 100;

    private void Update()
    {
        UpdateStopwatchPulse();

        if (!enableCheatCodes) return;

        // Time.timeScale is 0 whenever the shop or pause menu is open, so this doubles
        // as a guard against firing cheats while either of those is up.
        if (Time.timeScale == 0f) return;

        if (Input.GetKeyDown(skipWaveKey))
        {
            CheatSkipWave();
        }

        if (Input.GetKeyDown(skipToBossKey))
        {
            CheatSkipToBossWave();
        }

        if (Input.GetKeyDown(addCoinsKey))
        {
            CheatAddCoins();
        }

        if (Input.GetKeyDown(addScoreKey))
        {
            CheatAddScore();
        }
    }

    // Swaps the stopwatch icon to red and pulses it (grows/shrinks smoothly) whenever
    // the wave timer has lowTimeThreshold seconds or less remaining. Reverts to the
    // normal yellow icon at rest as soon as that's no longer true.
    private void UpdateStopwatchPulse()
    {
        if (stopwatchIcon == null) return;

        bool isLowTime = waveRunning && currentWaveTime > 0f && currentWaveTime <= lowTimeThreshold;

        if (isLowTime)
        {
            if (!stopwatchIsRed)
            {
                stopwatchIcon.sprite = stopwatchRedSprite;
                stopwatchIsRed = true;
            }

            // Oscillates smoothly between the icon's normal size and (1 + pulseScaleAmount)
            float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f; // 0..1
            float scale = 1f + pulseScaleAmount * pulse;
            stopwatchIcon.transform.localScale = new Vector3(scale, scale, 1f);
        }
        else if (stopwatchIsRed)
        {
            stopwatchIcon.sprite = stopwatchYellowSprite;
            stopwatchIcon.transform.localScale = Vector3.one;
            stopwatchIsRed = false;
        }
    }

    // Cheat: grants the player materials (the shop currency) directly.
    private void CheatAddCoins()
    {
        if (Player.Instance == null)
        {
            Debug.LogWarning("[CHEAT] Add coins: no Player.Instance found.");
            return;
        }

        Debug.Log($"[CHEAT] Adding {cheatCoinsAmount} materials");
        Player.Instance.AddMaterials(cheatCoinsAmount);
    }

    // Cheat: grants the player score directly.
    private void CheatAddScore()
    {
        if (Player.Instance == null)
        {
            Debug.LogWarning("[CHEAT] Add score: no Player.Instance found.");
            return;
        }

        Debug.Log($"[CHEAT] Adding {cheatScoreAmount} score");
        Player.Instance.Addscore(cheatScoreAmount);
    }

    // Cheat: instantly clears the current wave (no kill rewards for the skipped enemies)
    // and goes straight into the normal wave-cleared -> shop flow.
    private void CheatSkipWave()
    {
        if (!waveRunning) return;

        Debug.Log("[CHEAT] Skipping wave " + currentWave);

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.DestroyAllEnemies();
        }

        WaveComplete(true);
    }

    // Cheat: jumps straight into the next boss (or megaboss, if that's the next multiple
    // of 5) wave, bypassing the shop and any wave in between.
    private void CheatSkipToBossWave()
    {
        Debug.Log("[CHEAT] Skipping to next boss wave");

        StopAllCoroutines();
        isCountdownActive = false;

        if (EnemyManager.Instance != null)
        {
            EnemyManager.Instance.DestroyAllEnemies();
        }

        int nextBossWave = ((currentWave / 5) + 1) * 5;
        currentWave = nextBossWave - 1; // StartNewWave() increments this by 1

        waveRunning = false;

        StartNewWave();
    }



    public bool WaveRunning() => waveRunning;



    private void StartNewWave()

    {

        // Don't start if player is dead

        Player player = FindObjectOfType<Player>();

        if (player == null || player.IsDead())

        {

            Debug.Log("Player is dead -- cannot start new wave.");

            return;

        }



        StopAllCoroutines();

        isCountdownActive = false;

        timeText.color = Color.white;

        if (stopwatchIcon != null)
        {
            stopwatchIcon.sprite = stopwatchYellowSprite;
            stopwatchIcon.transform.localScale = Vector3.one;
            stopwatchIsRed = false;
        }



        currentWave++;

        waveRunning = true;

        // Player starts every wave at full health, regardless of damage taken last wave
        player.ResetHealthToFull();

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

        // Every 5th wave is a boss wave. Every 10th wave (also a multiple of 5) gets a
        // megaboss instead of the regular boss, rather than both spawning together.
        bool isMegaBossWave = currentWave % 10 == 0;
        bool isBossWave = currentWave % 5 == 0 && !isMegaBossWave;



        EnemyManager.Instance.StartWave(enemyCount, spawnInterval, isBossWave, isMegaBossWave);



        currentWaveTime = waveDuration;

        string waveLabel = ": " + currentWave;
        if (isMegaBossWave) waveLabel += " (MEGABOSS)";
        else if (isBossWave) waveLabel += " (BOSS)";
        waveText.text = waveLabel;

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