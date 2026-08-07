using System.Collections;
using UnityEngine;

public class Projactile : MonoBehaviour
{
   [SerializeField] float speed = 12f;

   private Coroutine autoReturnCoroutine;


    // OnEnable runs every time this pooled object is reactivated (Start only runs once, ever)
    private void OnEnable()
    {
        autoReturnCoroutine = StartCoroutine(ReturnToPoolAfterDelay(2f));
    }

    private void OnDisable()
    {
        // stop the timer if this object was already returned/deactivated some other way
        if (autoReturnCoroutine != null)
        {
            StopCoroutine(autoReturnCoroutine);
            autoReturnCoroutine = null;
        }
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
            // fallback if no pooler exists in the scene, so this doesn't silently do nothing
            Destroy(gameObject);
        }
    }

    private void FixedUpdate()
    {
        transform.Translate(Vector2.right * speed * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log("Bullet hit: " + collision.gameObject.name);
        if(collision.CompareTag("Player")) return;

        Enemy enemy = collision.GetComponent<Enemy>();

        if (enemy != null)
        {
            ReturnToPool();
            enemy.Hit(20);
        }
    }
}
