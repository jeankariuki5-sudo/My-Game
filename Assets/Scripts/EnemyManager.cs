using TMPro;
using UnityEngine;



public class EnemyManager : MonoBehaviour

{

    [SerializeField] private GameObject enemyPrefab;

    [Header("Charger Variants (fast, tanky, etc.)")]
    [SerializeField] private GameObject[] chargerPrefabs;

    [Header("Boss Waves")]
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private GameObject megaBossPrefab;

    [SerializeField] private float baseSpawnInterval = 0.8f;
    [SerializeField] private TextMeshProUGUI enemiesKilledText;



    private Transform enemiesParent;



    private int enemiesToSpawn = 0;

    private int enemiesSpawnedSoFar = 0;

    private int enemiesAlive = 0;

    private float currentSpawnInterval;

    private float spawnTimer = 0f;

    private bool isSpawning = false;

    private int enemiesKiled = 0;

    private int enemiesThisWaveTotal = 0; // peak enemy count for the wave, used as the progress bar's max

    public static EnemyManager Instance;



    private void Awake()

    {

        if (Instance == null) Instance = this;

    }

    // subscribenon enemy dead event
    private void OnEnable()
    {
        GameEvents.OnEnemyDied += HandleEnemyDied;
    }

    // Unsubscribe
    private void OnDisable()
    {
        GameEvents.OnEnemyDied -= HandleEnemyDied;
    }



    private void Start()

    {

        // --- FIX: ensure the "Enemies" GameObject exists ---

        GameObject parentGO = GameObject.Find("Enemies");

        if (parentGO == null)

        {

            parentGO = new GameObject("Enemies");

        }

        enemiesParent = parentGO.transform;

    }



    private void Update()

    {

        if (!isSpawning || enemiesSpawnedSoFar >= enemiesToSpawn)

            return;



        spawnTimer -= Time.deltaTime;

        if (spawnTimer <= 0f)

        {

            SpawnEnemy();

            spawnTimer = currentSpawnInterval;

        }

    }

    // Event handler enemy dead
    private void HandleEnemyDied(Transform enemyTransform)
    {
        if (enemiesAlive  <= 0)
        {
            return;
        }
        enemiesAlive --;
        OnEnemyKill();
        if (enemiesSpawnedSoFar >= enemiesToSpawn && enemiesAlive == 0)
        {
            isSpawning = false;
            // broadcast wave cleared event
            GameEvents.WaveCleared();
        }
    }




    public void StartWave(int totalEnemies, float spawnInterval)

    {

        StartWave(totalEnemies, spawnInterval, false, false);

    }

    public void StartWave(int totalEnemies, float spawnInterval, bool spawnBoss, bool spawnMegaBoss)

    {

        enemiesToSpawn = totalEnemies;

        enemiesSpawnedSoFar = 0;

        enemiesAlive = 0;

        enemiesKiled = 0;

        enemiesThisWaveTotal = totalEnemies + ((spawnBoss || spawnMegaBoss) ? 1 : 0);

        currentSpawnInterval = spawnInterval;

        spawnTimer = 0f;

        isSpawning = true;

        if (spawnMegaBoss && megaBossPrefab != null)
        {
            SpawnBoss(megaBossPrefab);
        }
        else if (spawnBoss && bossPrefab != null)
        {
            SpawnBoss(bossPrefab);
        }

    }

    // Spawns a boss/megaboss immediately when the wave starts, separate from the regular
    // timed spawn loop. Counted in enemiesAlive (so the wave won't clear until it dies too)
    // but not in enemiesToSpawn/enemiesSpawnedSoFar, which track the regular enemy batch.
    private void SpawnBoss(GameObject prefab)

    {

        if (ObjectPooler.Instance == null)
        {
            Debug.LogWarning("EnemyManager.SpawnBoss: no ObjectPooler in the scene.");
            return;
        }

        Vector2 spawnPos = RandomPosition();

        GameObject b = ObjectPooler.Instance.GetObject(prefab, spawnPos, Quaternion.identity);

        if (b == null)
        {
            Debug.LogWarning($"EnemyManager.SpawnBoss: pooler returned null for prefab '{prefab.name}'.");
            return;
        }

        b.transform.SetParent(enemiesParent);

        enemiesAlive++;

    }



    private void SpawnEnemy()

    {

        if (enemiesSpawnedSoFar >= enemiesToSpawn) return;

        if (ObjectPooler.Instance == null)
        {
            Debug.LogWarning("EnemyManager.SpawnEnemy: no ObjectPooler in the scene.");
            return;
        }



        float roll = Random.Range(0f, 100f);

        GameObject enemyType;

        if (roll < 90f || chargerPrefabs == null || chargerPrefabs.Length == 0)
        {
            enemyType = enemyPrefab;
        }
        else
        {
            enemyType = chargerPrefabs[Random.Range(0, chargerPrefabs.Length)];
        }



        Vector2 spawnPos = RandomPosition();

        GameObject e = ObjectPooler.Instance.GetObject(enemyType, spawnPos, Quaternion.identity);

        if (e == null)
        {
            Debug.LogWarning($"EnemyManager.SpawnEnemy: pooler returned null for prefab '{(enemyType != null ? enemyType.name : "null")}'.");
            return;
        }

        e.transform.SetParent(enemiesParent);


        enemiesSpawnedSoFar++;

        enemiesAlive++;

    }



    private void OnEnemyKill()

    {
        enemiesKiled++;
        enemiesKilledText.text = ":" + enemiesKiled;

    }



    private Vector2 RandomPosition()

    {

        return new Vector2(Random.Range(-16f, 16f), Random.Range(-8f, 8f));

    }



    public int GetAliveCount()

    {

        return enemiesAlive;

    }

    // Peak enemy count for the current wave (regular enemies + boss, if any).
    // Used as the denominator for the enemies-left progress bar.
    public int GetTotalCountThisWave()
    {
        return enemiesThisWaveTotal;
    }



    public void DestroyAllEnemies()

    {

        // --- FIX: check if enemiesParent is null ---

        if (enemiesParent == null) return;


        Transform[] children = new Transform[enemiesParent.childCount];
        for(int i = 0; i < enemiesParent.childCount; i++)
        {
            children[i] = enemiesParent.GetChild(i);
        }



        foreach (Transform e in children)

        {

            Enemy enemy = e.GetComponent<Enemy>();

            if (enemy != null)
                enemy.ReturnToPoolImmediately();

        }

        enemiesAlive = 0;

        isSpawning = false;

    }

}