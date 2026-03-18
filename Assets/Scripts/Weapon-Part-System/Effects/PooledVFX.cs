using UnityEngine;

public class PooledVFX : MonoBehaviour
{
    [Tooltip("How long before this object returns to the pool.")]
    public float lifetime = 2f;

    private PooledVFX _originalPrefabReference;
    private float _currentTimer;

    // Called by the GlobalVFXPool when it spawns
    public void SetPrefabReference(PooledVFX prefabRef)
    {
        _originalPrefabReference = prefabRef;
    }

    private void OnEnable()
    {
        // Reset the timer every time the pool wakes this object up
        _currentTimer = lifetime;

        // If you have a ParticleSystem on this object, tell it to play!
        ParticleSystem ps = GetComponent<ParticleSystem>();
        if (ps != null) ps.Play();
    }

    private void Update()
    {
        _currentTimer -= Time.deltaTime;
        
        if (_currentTimer <= 0)
        {
            if (GlobalVFXPool.Instance != null && _originalPrefabReference != null)
            {
                GlobalVFXPool.Instance.ReturnToPool(this, _originalPrefabReference);
            }
            else
            {
                // Fallback just in case the manager is missing
                gameObject.SetActive(false); 
            }
        }
    }
}