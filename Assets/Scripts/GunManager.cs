
using UnityEngine;

using System.Collections.Generic;



public class GunManager : MonoBehaviour

{

    [Header("Gun Prefabs (max 6)")]

    [SerializeField] private Gun[] gunPrefabs;



    [Header("Positioning")]

    [SerializeField] private float radius = 0.8f;

    [SerializeField] private float startAngle = 0f;

    [SerializeField] private bool useCircle = true;



    private List<Gun> activeGuns = new List<Gun>();



    void Start()

    {

        int gunCount = Mathf.Min(gunPrefabs.Length, 6);

        if (gunPrefabs.Length > 6)

            Debug.LogWarning("More than 6 guns assigned – only first 6 used.");



        Vector2[] offsets = GetOffsets(gunCount);



        for (int i = 0; i < gunCount; i++)

        {

            Gun prefab = gunPrefabs[i];

            if (prefab == null) continue;



            Gun gun = Instantiate(prefab, transform);

            gun.SetOffset(offsets[i], transform);

            activeGuns.Add(gun);

        }

    }



    private Vector2[] GetOffsets(int count)

    {

        Vector2[] offsets = new Vector2[count];

        if (count == 0) return offsets;



        if (useCircle)

        {

            float angleStep = 360f / count;

            float currentAngle = startAngle;

            for (int i = 0; i < count; i++)

            {

                float rad = currentAngle * Mathf.Deg2Rad;

                offsets[i] = new Vector2(

                Mathf.Cos(rad) * radius,

                Mathf.Sin(rad) * radius

                );

                currentAngle += angleStep;

            }

        }

        else

        {

            float spacing = 2f * radius / (count - 1);

            float startX = -radius;

            for (int i = 0; i < count; i++)

            {

                offsets[i] = new Vector2(startX + i * spacing, 0);

            }

        }

        return offsets;

    }



    /// <summary>

    /// Adds a new gun and repositions all guns to fit the new count.

    /// </summary>

    public void AddGun(Gun gunPrefab)

    {

        if (activeGuns.Count >= 6)

        {

            Debug.Log("Max guns (6) already reached!");

            return;

        }



        // Instantiate and add the new gun

        Gun newGun = Instantiate(gunPrefab, transform);

        activeGuns.Add(newGun);



        // Get new offsets for the updated count

        int newCount = activeGuns.Count;

        Vector2[] offsets = GetOffsets(newCount);



        // Reposition ALL guns (including the new one)

        for (int i = 0; i < newCount; i++)

        {

            activeGuns[i].SetOffset(offsets[i], transform);

        }



        Debug.Log($"Gun added. Total: {newCount}/6");

    }



    /// <summary>
    /// Returns the current number of active guns.
    /// </summary>
    public int GetActiveGunCount()
    {
        return activeGuns.Count;
    }

    /// <summary>
    /// Returns the list of currently equipped guns (e.g. for building a slot-swap UI).
    /// </summary>
    public List<Gun> GetActiveGuns()
    {
        return activeGuns;
    }

    /// <summary>
    /// Replaces the gun in a specific slot with a new gun, keeping that slot's position.
    /// Used when the player is at max guns (6) and picks a slot to swap.
    /// </summary>
    public void ReplaceGun(int slotIndex, Gun gunPrefab)
    {
        if (slotIndex < 0 || slotIndex >= activeGuns.Count)
        {
            Debug.LogWarning($"ReplaceGun: invalid slot index {slotIndex}");
            return;
        }
        if (gunPrefab == null) return;

        // Reuse the same offset the replaced gun had, so positions don't shuffle
        Vector2[] offsets = GetOffsets(activeGuns.Count);

        Gun oldGun = activeGuns[slotIndex];
        if (oldGun != null)
        {
            Destroy(oldGun.gameObject);
        }

        Gun newGun = Instantiate(gunPrefab, transform);
        newGun.SetOffset(offsets[slotIndex], transform);
        activeGuns[slotIndex] = newGun;

        Debug.Log($"Gun in slot {slotIndex} replaced.");
    }
}