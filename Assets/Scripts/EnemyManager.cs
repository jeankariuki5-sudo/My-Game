using TMPro;
using UnityEngine;



public class EnemyManager : MonoBehaviour

{

    [SerializeField] private GameObject enemyPrefab;

    [Header("Charger Variants (fast, tanky, etc.)")]
    [SerializeField] private GameObject[] chargerPrefabs;

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

        enemiesToSpawn = totalEnemies;

        enemiesSpawnedSoFar = 0;

        enemiesAlive = 0;

        enemiesKiled = 0;

        currentSpawnInterval = spawnInterval;

        spawnTimer = 0f;

        isSpawning = true;

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