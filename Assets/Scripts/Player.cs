using UnityEngine;
using TMPro;
using System;

public class Player : MonoBehaviour
{

    // ----singleton------
    public static Player Instance;

   public float baseMoveSpeed = 6f;
   public float moveSpeed;

   [Header("Health settings")]
   [SerializeField] TextMeshProUGUI healthText;
   float maxHealth = 100;
   float currentHealth;


   [Header("Score settings")]
   [SerializeField] TextMeshProUGUI scoreText;
   [SerializeField] TextMeshProUGUI materialsText;
   int currentScore = 0;
   private int materials = 0; //currency for the shop

   [Header("Upgrade Bonuses")]
   public int damageBonus = 0;
   public float speedBonus = 0f;


   Animator anim;
   Rigidbody2D rb;

   float moveHorizontal;
   float moveVertical;
   Vector2 movement;

   int facingDirection = 1;
   bool dead = false;

   [Header("Audio Settings")]
   [SerializeField] private AudioClip shootSound;
   [SerializeField] private AudioClip hitSound;
   private AudioSource audioSource;


   private void Awake()
    {
        // singleton setup
        if(Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
            
        
    }

    // subscribe to shop closed event for timescale management
    private void OnEnable()
    {
        GameEvents.OnShopClosed += OnShopClosed;
    }

    private void OnDisable()
    {
        GameEvents.OnShopClosed -= OnShopClosed;
    }

    private void OnShopClosed()
    {
        if (!PauseManager.Instance.IsPaused())
        {
            Time.timeScale = 1f;
        }
    }

    void Start()
    {
        // initialize variables
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        // Initialise health variables
        currentHealth = maxHealth;
        moveSpeed = baseMoveSpeed;

        // setup audio
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.enabled = true;
        audioSource.volume = 1f;
        audioSource.spatialBlend =0f;

        UpdateHealthUI();
        UpdateScoreUI();
        UpdateMaterialsUI();



    }

    void Update()
    {
        if (dead)
        {
            movement = Vector2.zero;
            anim.SetFloat("Velocity", 0);
            
            return;
        }
        moveHorizontal = Input.GetAxisRaw("Horizontal");
        moveVertical = Input.GetAxisRaw("Vertical");

        movement = new Vector2(moveHorizontal, moveVertical).normalized;

        // Enable animation switch
        anim.SetFloat("Velocity", movement.magnitude);

        if(movement.x != 0)
        {
            facingDirection = movement.x > 0 ? 1:-1;
        }
        transform.localScale = new Vector2(facingDirection, 1);
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        Enemy enemy = collision.gameObject.GetComponent<Enemy>();
        if (enemy != null)
        {
            Hit(10);
        }
    }

    public void Hit(int damage)
    {
        if (dead) return;

        anim.SetTrigger("Hit");
        currentHealth -= damage;

        healthText.text = Mathf.Clamp(currentHealth, 0, maxHealth).ToString();
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        UpdateHealthUI();

        // broadcast when player isDamaged
        GameEvents.PlayerDamaged(damage);

        // Play hit sount
        if (hitSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hitSound);
        }
        if(currentHealth <= 0)
        {
            Die();
        }
    }

    public void PlayShootSound()
    {
        if (shootSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(shootSound);
        }
    }

    void Die()
    {
        dead = true;

        currentHealth = 0;
        UpdateHealthUI();

        movement = Vector2.zero;
        rb.linearVelocity = Vector2.zero;
        anim.SetFloat("Velocity", 0);

        // Broadcast player is dead
        GameEvents.PlayerDied();

        // save Higscore on death
        UpdateScoreUI();
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SaveHighScore(currentScore);
        }

        GameManager.Instance?.GameOver();
    }
    // call game over ui

    public bool IsDead()
    {
        return dead;
    }


    void UpdateHealthUI()
    {
        if(healthText != null)
        {
            healthText.text = ": " + currentHealth.ToString();
        }
    }

    void UpdateScoreUI()
    {
        if(scoreText != null)
        {
            scoreText.text = ": " + currentScore.ToString();
        }
    }

    public void Addscore(int amount)
    {
        if(dead) return;
        currentScore += amount;
        UpdateScoreUI();
    }

    public void AddHealth(int amount)
    {
        if(dead) return;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        UpdateHealthUI();

        // Update player healed
        GameEvents.PlayerHealed(amount);
    }

    private void UpdateMaterialsUI()
    {
        if (materialsText != null)
        {
            materialsText.text = "" + materials;
        }
    }

    public int GetMaterials() => materials;
    public void AddMaterials(int amount)
    {
        if (dead) return;
        materials += amount;

        UpdateMaterialsUI();

        // Broadcast materials changed
        GameEvents.MaterialsChanged(materials);
    }

    // called by shop manager to apply an upgrade
    public void ApplyUpgrade(UpgradeSO upgrade)
    {
        switch (upgrade.stat)
        {
            case UpgradeSO.StatType.Damage:
                damageBonus += (int)upgrade.value;
                break;
            case UpgradeSO.StatType.FireRate:
                Gun gun = FindObjectOfType<Gun>();
                if (gun != null)
                {
                    gun.fireRate = Mathf.Max(gun.fireRate - upgrade.value, 0.1f);
                }
                break;
            case UpgradeSO.StatType.Speed:
                speedBonus += upgrade.value;
                moveSpeed = baseMoveSpeed + speedBonus;
                break;
            case UpgradeSO.StatType.MaxHealth:
                maxHealth += upgrade.value;
                AddHealth((int)upgrade.value);
                break;
            default:
                break;
        }
    }




}
