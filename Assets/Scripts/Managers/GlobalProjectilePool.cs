using System.Collections.Generic;
using UnityEngine;

public class GlobalProjectilePool : MonoBehaviour
{
    // The Singleton instance
    public static GlobalProjectilePool Instance;

    // A dictionary where the Key is the Prefab's ID, and the Value is a Queue of inactive bullets
    private Dictionary<int, Queue<BaseProjectile>> _poolDictionary = new Dictionary<int, Queue<BaseProjectile>>();

    private void Awake()
    {
        // Standard Singleton setup
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public BaseProjectile GetProjectile(BaseProjectile prefab, Vector3 position, Quaternion rotation)
    {
        int prefabId = prefab.gameObject.GetInstanceID();

        // If this is the first time anyone has asked for this specific bullet type, create a new queue for it
        if (!_poolDictionary.ContainsKey(prefabId))
        {
            _poolDictionary.Add(prefabId, new Queue<BaseProjectile>());
        }

        BaseProjectile spawnedBullet = null;

        // Try to grab an inactive bullet from the queue
        if (_poolDictionary[prefabId].Count > 0)
        {
            spawnedBullet = _poolDictionary[prefabId].Dequeue();
        }
        else
        {
            // The queue is empty (we fired too fast!), so we must instantiate a new one
            spawnedBullet = Instantiate(prefab);
        }

        // Set its position, wake it up, and return it to the weapon
        spawnedBullet.transform.position = position;
        spawnedBullet.transform.rotation = rotation;
        spawnedBullet.gameObject.SetActive(true);

        return spawnedBullet;
    }

    // Bullets will call this on themselves when they hit a wall or expire
    public void ReturnToPool(BaseProjectile activeBullet, BaseProjectile originalPrefab)
    {
        activeBullet.gameObject.SetActive(false);
        int prefabId = originalPrefab.gameObject.GetInstanceID();
        
        // Put it back in the correct queue
        if (_poolDictionary.ContainsKey(prefabId))
        {
            _poolDictionary[prefabId].Enqueue(activeBullet);
        }
    }

    // Add this to GlobalProjectilePool.cs
    public void PreWarm(BaseProjectile prefab, int amount)
    {
        int prefabId = prefab.gameObject.GetInstanceID();

        // Create the queue if it doesn't exist yet
        if (!_poolDictionary.ContainsKey(prefabId))
        {
            _poolDictionary.Add(prefabId, new Queue<BaseProjectile>());
        }

        // Spawn the requested amount and hide them
        for (int i = 0; i < amount; i++)
        {
            BaseProjectile newBullet = Instantiate(prefab);
            newBullet.gameObject.SetActive(false);
            
            // Put it in the queue for later
            _poolDictionary[prefabId].Enqueue(newBullet);
        }
    }
}