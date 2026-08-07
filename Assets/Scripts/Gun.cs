using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    [Header("Bullet Settings")]
    [SerializeField] GameObject muzzle;
    [SerializeField] Transform muzzlePosition;
    [SerializeField] GameObject projectile;

    [Header("Bullet configs")]
    [SerializeField] float fireDistance = 10f;
    [SerializeField] public float fireRate = 0.5f;
    private float timeSinceLastShot = 0f;


    [Header("Position relative to player")]
    [SerializeField] private Vector2 baseOffset;


    Transform player;
    Vector2 offset;
    Transform closestEnemy;



    private void Start()
    {
        player = GameObject.Find("Player").transform;
        timeSinceLastShot = fireRate;
        // setOffset(new Vector2(1, 0.5f));
        baseOffset = offset;
    }

    private void Update()
    {
        transform.position = (Vector2)player.position + offset;
        // if (player == null) return;

        // // Mirror position when player flips
        float facing = Mathf.Sign(player.localScale.x);
        // Vector2 mirroredOffset = new Vector2(baseOffset.x * facing, baseOffset.y);
        // transform.localPosition = mirroredOffset;


        FindClosestEnemy();

        // If there is an enemy, aim at it; otherwise point forward
        if (closestEnemy != null)
        {
            AimAtEnemy();
        }
        else
        {
            // Default rotation: point in the player's facing direction
            // facing = 1 (right) -> angle 0; facing = -1(left) -> angle 180
            float defaultAngle = (facing > 0) ? 0f : 180f;
            transform.rotation = Quaternion.Euler(0, 0, defaultAngle);
        }
        
        Shooting();
    }

    void FindClosestEnemy()
    {
        closestEnemy = null;
        float closestDistance = Mathf.Infinity;
        Enemy[] enemies = FindObjectsOfType<Enemy>();

        foreach (Enemy enemy in enemies)
        {
            float distance = Vector2.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance && distance <= fireDistance)
            {
                closestDistance = distance;
                closestEnemy = enemy.transform;
            }
        }
    }

    void AimAtEnemy()
    {
        if(closestEnemy != null)
        {
            Vector3 direction = closestEnemy.position - transform.position;
            direction.Normalize();
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle);
            transform.position = (Vector2)player.position + offset;
        }
    }


    void Shooting()
    {
        if(closestEnemy == null)
        {
            return;
        }
        timeSinceLastShot += Time.deltaTime;
        if (timeSinceLastShot >= fireRate)
        {
            Shoot();
            timeSinceLastShot = 0;

        }
    }


    void Shoot()
    {
        if (ObjectPooler.Instance == null)
        {
            Debug.LogWarning("Gun.Shoot: no ObjectPooler in the scene.");
            return;
        }

        var muzzleGo = ObjectPooler.Instance.GetObject(muzzle, muzzlePosition.position, transform.rotation);
        if (muzzleGo != null)
        {
            muzzleGo.transform.SetParent(transform);
            StartCoroutine(ReturnMuzzleFlashAfterDelay(muzzleGo, 0.05f));
        }

        var projectileGo = ObjectPooler.Instance.GetObject(projectile, muzzlePosition.position, transform.rotation);
        // Note: the projectile returns itself to the pool on a timer (see Projactile.cs) or on hit,
        // so no destroy/return call is needed here.

        Player.Instance?.PlayShootSound();
    }

    // MuzzleFlash has no script of its own, so Gun is responsible for returning it to the pool
    private IEnumerator ReturnMuzzleFlashAfterDelay(GameObject muzzleGo, float delay)
    {
        yield return new WaitForSeconds(delay);
        muzzleGo.transform.SetParent(null);
        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.ReturnObject(muzzleGo);
        }
    }

    // Called by gun manager to set the offset and parent
    // didtance of gun to player
    public void SetOffset(Vector2 off, Transform parent)
    {
        baseOffset = off;
        offset = off;
        transform.SetParent(parent);
        float facing = Mathf.Sign(parent.localScale.x);
        transform.localPosition = new Vector2(baseOffset.x * facing, baseOffset.y);
        transform.localRotation = Quaternion.identity;
        player = parent;
    }

}
