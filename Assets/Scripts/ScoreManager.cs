using UnityEngine;
using TMPro;

public class ScoreManager : MonoBehaviour
{
    [Header("Pickup Values")]
    [SerializeField] int scoreAmount = 10;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Player player = collision.GetComponent<Player>();
        if (player != null)
        {
            player.Addscore(scoreAmount);
            Destroy(gameObject);
        }
    }

}
