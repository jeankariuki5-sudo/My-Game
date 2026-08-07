using UnityEngine;

public class HealthPickup : MonoBehaviour
{
    [Header("Pickup Values")]
    [SerializeField] int healAmount = 20;


    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        if (player != null)
        {
            player.AddHealth(healAmount);

            if (ObjectPooler.Instance != null)
            {
                ObjectPooler.Instance.ReturnObject(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
    

}
