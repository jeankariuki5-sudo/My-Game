using TMPro;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI healthText;

    Animator anim;
    Rigidbody2D rb;

    [SerializeField] float moveSpeed = 6f;
    int maxHealth = 100;
    int currentHealth;

    bool isDead = false;
    
    float moveHorizontal, moveVertical;
    Vector2 movement;

    int facingDirection;  //-1 left, 1 right

    private void Start()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();

        currentHealth = maxHealth;
        healthText.text = "Health: " + maxHealth.ToString();
    }

    private void Update()
    {
        // Testing
        if (Input.GetKeyDown(KeyCode.Space))
            Hit(10);
            

        if (isDead)
        {
            movement = Vector2.zero;
            anim.SetFloat("Velocity", 0);
            return;
        }

        moveHorizontal = Input.GetAxisRaw("Horizontal");
        moveVertical = Input.GetAxisRaw("Vertical");

        movement = new Vector2(moveHorizontal, moveVertical).normalized;

        anim.SetFloat("Velocity", movement.magnitude);

        if (movement.x != 0)
            facingDirection = movement.x > 0 ? 1 : -1;

        transform.localScale = new Vector2(facingDirection, 1);
    }

    private void FixedUpdate()
    {
      rb.linearVelocity =   movement * moveSpeed;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Collide with an enemy
    }

    void Hit(int damage)
    {
        anim.SetTrigger("Hit");
        currentHealth -= damage;
        healthText.text = "Haelth: " +  Mathf.Clamp(currentHealth, 0, maxHealth).ToString();

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        isDead = true;
        // Call gameover
    }

}
