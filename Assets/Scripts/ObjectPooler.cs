using UnityEngine;
using System;
using System.Collections.Generic;

public class ObjectPooler : MonoBehaviour
{
    [Serializable]
    public class Pool{
        public string tag;
        public GameObject prefab;
        public int size;
    }

    public List<Pool> pools;
    private Dictionary<string, Queue<GameObject>> poolDictionary;

    // Maps a prefab asset reference back to its tag, so callers can just drag a prefab
    // instead of typing a tag string by hand.
    private Dictionary<GameObject, string> prefabToTag;

    // Remembers which tag each live instance came from, so ReturnObject doesn't need
    // to be told the tag either - just hand back the GameObject.
    private Dictionary<int, string> instanceTagLookup;

    public static ObjectPooler Instance;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        poolDictionary = new Dictionary<string, Queue<GameObject>>();
        prefabToTag = new Dictionary<GameObject, string>();
        instanceTagLookup = new Dictionary<int, string>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();

            if (pool.prefab != null)
            {
                prefabToTag[pool.prefab] = pool.tag;
            }

            for (int i = 0; i < pool.size; i++)
            {
               GameObject obj = Instantiate(pool.prefab);
               obj.SetActive(false);
               instanceTagLookup[obj.GetInstanceID()] = pool.tag;
                objectPool.Enqueue(obj);
            }
            poolDictionary.Add(pool.tag, objectPool);
        }
    }

    // Core tag-based lookup. Still available directly if you ever need it.
    public GameObject GetObject(string tag)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"ObjectPooler: no pool with tag '{tag}'.");
            return null;
        }

        Queue<GameObject> pool = poolDictionary[tag];
        if (pool.Count == 0)
        {
            // Expand pool if empty
            Pool poolConfig = pools.Find(p => p.tag == tag);
            if (poolConfig != null)
            {
                GameObject obj = Instantiate(poolConfig.prefab);
                obj.SetActive(false);
                instanceTagLookup[obj.GetInstanceID()] = tag;
                pool.Enqueue(obj);
            }
        }

        GameObject objectToSpawn = pool.Dequeue();
        objectToSpawn.SetActive(true);
        return objectToSpawn;
    }

    // Convenience overload: gets an object and positions it in one call
    public GameObject GetObject(string tag, Vector3 position, Quaternion rotation)
    {
        GameObject obj = GetObject(tag);
        if (obj == null) return null;

        obj.transform.SetParent(null); // clear any leftover parent from its previous use
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        return obj;
    }

    // Prefab-based lookup: drag a prefab reference in the Inspector instead of typing a tag.
    // The prefab must be the SAME asset reference that's assigned in this ObjectPooler's
    // Pools list (drag the same prefab from the Project window in both places).
    public GameObject GetObject(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null) return null;

        if (!prefabToTag.TryGetValue(prefab, out string tag))
        {
            Debug.LogWarning($"ObjectPooler: prefab '{prefab.name}' isn't registered in the Pools list on ObjectPooler. Add it there first.");
            return null;
        }

        return GetObject(tag, position, rotation);
    }

    public void ReturnObject(string tag, GameObject obj)
    {
        if (!poolDictionary.ContainsKey(tag))
        {
            Debug.LogWarning($"ReturnObject: no pool exists for tag '{tag}'. Destroying object instead.");
            Destroy(obj);
            return;
        }

        obj.SetActive(false);
        poolDictionary[tag].Enqueue(obj);
    }

    // Simplified return: hand back a GameObject that was dispensed by this pooler and it
    // figures out which pool it belongs to on its own.
    public void ReturnObject(GameObject obj)
    {
        if (obj == null) return;

        if (instanceTagLookup.TryGetValue(obj.GetInstanceID(), out string tag))
        {
            ReturnObject(tag, obj);
        }
        else
        {
            Debug.LogWarning($"ReturnObject: '{obj.name}' wasn't dispensed by this pooler. Destroying instead.");
            Destroy(obj);
        }
    }

}
