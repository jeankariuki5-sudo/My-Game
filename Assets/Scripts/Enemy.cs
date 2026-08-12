using UnityEngine;

using System;
using System.Collections;



public class Enemy : MonoBehaviour

{

    [Header("Stats")]

    [SerializeField] private Animator anim;

    private Transform target;



    [Header("Charger / Boss Dash")]

    [SerializeField] private float distanceToCharge = 15f;

    [SerializeField] private bool isCharger;
    [SerializeField] private bool isBoss;       // NEW: Checkbox for your Boss prefab
    [SerializeField] private bool isMegaboss;   // NEW: Checkbox for your Megaboss prefab

    [SerializeField] private float chargeSpeed = 12f;

    [SerializeField] private float prepareTime = 2f;

    [Tooltip("Max seconds a charge can last if it never reaches its target (safety fallback)")]
    [SerializeField] private float maxChargeDuration = 2f;

    private bool isCharging = false;

    private bool isPreparingCharge = false;

    // The point being charged toward. Captured once when the charge begins, then never
    // updated again for the duration of the charge - the enemy commits to this point even
    // if the player moves away, rather than continuing to home in on their live position.
    private Vector3 chargeTargetPosition;



    [SerializeField] private float speed = 2f;
    private float baseSpeed; // caches the original serialized speed, since `speed` is temporarily overwritten during a charge

    [SerializeField] private float stoppingDistance = 1.5f;

    [Header("Size (for boss/megaboss variants)")]
    [Tooltip("1 = normal size, 2 = double size (boss), 5 = 5x size (megaboss), etc.")]
    [SerializeField] private float sizeMultiplier = 1f;



    [SerializeField] private int maxHealth = 100;

    private int currentHealth;



    [Header("Rewards")]

    [SerializeField] private int scoreValue = 10;
    [SerializeField] private int materialValue = 5;

    [Header("Health drop")]

    [SerializeField] private GameObject healthPickupPrefab;

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

        anim.SetBool("IsCharger", isCharger);
        anim.SetBool("IsBoss", isBoss);
        anim.SetBool("IsMegaboss", isMegaboss);
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
        if (!WaveManager.Instance.WaveRunning()) { return; }
        if (target == null) return;

        // 1. IF PREPARING CHARGE: The enemy stops completely to wind up.
        if (isPreparingCharge)
        {
            anim.SetBool("IsMoving", false); // Force them into Idle while winding up!
            return;
        }

        // 2. IF CHARGING: The enemy is sprinting toward the target point
        if (isCharging)
        {
            Vector3 chargeDirection = (chargeTargetPosition - transform.position).normalized;
            transform.position += chargeDirection * speed * Time.deltaTime;
            bool facingRightDuringCharge = chargeTargetPosition.x > transform.position.x;
            transform.localScale = new Vector2(facingRightDuringCharge ? -sizeMultiplier : sizeMultiplier, sizeMultiplier);

            if (Vector2.Distance(transform.position, chargeTargetPosition) < 0.2f)
            {
                CancelInvoke(nameof(StopCharging));
                StopCharging();
            }

            anim.SetBool("IsMoving", true); // FORCE animation to play while charging!
            return;
        }

        // 3. STANDARD MOVEMENT: Regular tracking toward the player
        Vector3 direction = (target.position - transform.position).normalized;
        transform.position += direction * speed * Time.deltaTime;
        bool playerToRight = target.position.x > transform.position.x;
        transform.localScale = new Vector2(playerToRight ? -sizeMultiplier : sizeMultiplier, sizeMultiplier);

        bool canDash = isCharger || isBoss || isMegaboss;

        if (canDash && !isCharging && Vector2.Distance(transform.position, target.position) < distanceToCharge)
        {
            isPreparingCharge = true;
            Invoke(nameof(StartCharging), prepareTime);
        }

        // FORCE animation to play during normal walk state
        anim.SetBool("IsMoving", true);
    }




    private void StartCharging()

    {

        Debug.Log("Charge started!");

        isPreparingCharge = false;

        isCharging = true;

        speed = chargeSpeed;

        // Lock in the target position now - this is the point being charged toward for the
        // whole dash, regardless of where the player moves afterward.
        chargeTargetPosition = target != null ? target.position : transform.position;

        Invoke(nameof(StopCharging), maxChargeDuration);

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
            player.Addscore(scoreValue);
            player.AddMaterials(materialValue);
        }




        // 1 in 10 chance to drop a health pickup
        if (ObjectPooler.Instance != null)

        {

            int dropRoll = UnityEngine.Random.Range(0, 10); // 0-9 inclusive
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
