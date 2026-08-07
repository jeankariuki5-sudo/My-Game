using UnityEngine;

using System;
using System.Collections;



public class Enemy : MonoBehaviour

{

    [Header("Stats")]

    [SerializeField] private Animator anim;

    private Transform target;



    [Header("Charger enemy")]

    [SerializeField] private float distanceToCharge = 15f;

    [SerializeField] private bool isCharger;

    [SerializeField] private float chargeSpeed = 12f;

    [SerializeField] private float prepareTime = 2f;

    private bool isCharging = false;

    private bool isPreparingCharge = false;



    [SerializeField] private float speed = 2f;
    private float baseSpeed; // caches the original serialized speed, since `speed` is temporarily overwritten during a charge

    [SerializeField] private float stoppingDistance = 1.5f;



    private int maxHealth = 100;

    private int currentHealth;



    [Header("Health drop")]

    [SerializeField] private GameObject healthPickupPrefab;
    [SerializeField] private int materialValue = 5;

    [Header("Audio Sethhings")]
    [SerializeField] private AudioClip enemyHit;
    private AudioSource audioSource;

    private bool isDead = false;

    private Coroutine returnToPoolCoroutine;



    // --- New death event ---

    public event Action OnDeath;



    // Awake runs exactly once per object, right after instantiation — safe place for one-time setup
    private void Awake()
    {
        anim = GetComponent<Animator>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.enabled = true;
        audioSource.volume = 0.5f;
        audioSource.spatialBlend = 0f;

        baseSpeed = speed;
    }

    // OnEnable runs every time this object is reactivated (including from the pool), unlike Start/Awake.
    // All per-life state that needs resetting between pooled reuses lives here.
    private void OnEnable()
    {
        GameObject playerGO = GameObject.Find("Player");
        target = playerGO != null ? playerGO.transform : null;

        currentHealth = maxHealth;
        isDead = false;
        isCharging = false;
        isPreparingCharge = false;
        speed = baseSpeed;

        CancelInvoke();
    }

    private void OnDisable()
    {
        CancelInvoke();
        if (returnToPoolCoroutine != null)
        {
            StopCoroutine(returnToPoolCoroutine);
            returnToPoolCoroutine = null;
        }
    }



    private void Update()

    {

        if (!WaveManager.Instance.WaveRunning())

        {

            return;

        }

        if (target == null) return;



        if (isPreparingCharge)

        {

            return;

        }



        Vector3 direction = (target.position - transform.position).normalized;

        transform.position += direction * speed * Time.deltaTime;



        bool playerToRight = target.position.x > transform.position.x;

        transform.localScale = new Vector2(playerToRight ? -1 : 1, 1);



        if (isCharger && !isCharging && Vector2.Distance(transform.position, target.position) < distanceToCharge)

        {

            isPreparingCharge = true;

            Invoke(nameof(StartCharging), prepareTime);

        }

    }



    private void StartCharging()

    {

        Debug.Log("Charge started!");

        isPreparingCharge = false;

        isCharging = true;

        speed = chargeSpeed;

        Invoke(nameof(StopCharging), 2f);

    }



    private void StopCharging()

    {

        isCharging = false;

        speed = baseSpeed;

    }



    public void Hit(int damage)

    {

        anim.SetTrigger("Hit");

        Debug.Log("Hit enemy");

        currentHealth -= damage;

        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (enemyHit != null && audioSource != null)
        {
            audioSource.PlayOneShot(enemyHit);
        }

        if (currentHealth <= 0) Die();

    }



    private void Die()

    {
        if (isDead)
        {
            return;
        }
        isDead = true;


        GameEvents.EnemyDied(transform);


        // --- Notify the manager BEFORE destroying ---

        OnDeath?.Invoke();



        Player player = FindObjectOfType<Player>();

        if (player != null)
        {
            player.Addscore(10);
            player.AddMaterials(materialValue);
        } 
            



        // 1 in 100 chance to drop a health pickup
        if (ObjectPooler.Instance != null)

        {

            int dropRoll = UnityEngine.Random.Range(0, 100); // 0-99 inclusive
            if (dropRoll == 0)
            {
                ObjectPooler.Instance.GetObject(healthPickupPrefab, transform.position, Quaternion.identity);
            }

        }



        returnToPoolCoroutine = StartCoroutine(ReturnToPoolAfterDelay(0.5f));

    }

    private IEnumerator ReturnToPoolAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.ReturnObject(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Called by EnemyManager when a wave times out and remaining enemies need to be cleared
    // immediately, bypassing the normal death sequence (no score/materials/pickup).
    public void ReturnToPoolImmediately()
    {
        if (isDead) return; // already mid-death-sequence, let that finish on its own
        isDead = true;
        ReturnToPool();
    }

}
