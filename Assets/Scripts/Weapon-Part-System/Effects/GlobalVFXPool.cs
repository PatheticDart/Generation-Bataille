using System.Collections.Generic;
using UnityEngine;

public class GlobalVFXPool : MonoBehaviour
{
    public static GlobalVFXPool Instance;

    private Dictionary<int, Queue<PooledVFX>> _poolDictionary = new Dictionary<int, Queue<PooledVFX>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public PooledVFX Spawn(PooledVFX prefab, Vector3 position, Quaternion rotation)
    {
        int prefabId = prefab.gameObject.GetInstanceID();

        if (!_poolDictionary.ContainsKey(prefabId))
        {
            _poolDictionary.Add(prefabId, new Queue<PooledVFX>());
        }

        PooledVFX spawnedVFX = null;

        if (_poolDictionary[prefabId].Count > 0)
        {
            spawnedVFX = _poolDictionary[prefabId].Dequeue();
        }
        else
        {
            spawnedVFX = Instantiate(prefab);
        }

        spawnedVFX.transform.position = position;
        spawnedVFX.transform.rotation = rotation;
        spawnedVFX.gameObject.SetActive(true);

        // Tell the effect who its parent prefab is so it knows where to return!
        spawnedVFX.SetPrefabReference(prefab);

        return spawnedVFX;
    }

    public void ReturnToPool(PooledVFX activeVFX, PooledVFX originalPrefab)
    {
        activeVFX.gameObject.SetActive(false);
        int prefabId = originalPrefab.gameObject.GetInstanceID();
        
        if (_poolDictionary.ContainsKey(prefabId))
        {
            _poolDictionary[prefabId].Enqueue(activeVFX);
        }
    }
}