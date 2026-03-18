using UnityEngine;

public abstract class BaseProjectile : MonoBehaviour
{
    [Header("Universal Stats")]
    public float lifetime = 3f;
    public float damage = 50f;

    protected BaseProjectile originalPrefabReference;
    private float currentLifeTimer;

    // Added 'virtual' so RaycastProjectile can tap into this
    protected virtual void OnEnable()
    {
        currentLifeTimer = lifetime;
    }

    public void SetPrefabReference(BaseProjectile prefabRef)
    {
        originalPrefabReference = prefabRef;
    }

    public virtual void SetupStats(float newDamage, float newSpeed)
    {
        damage = newDamage;
    }

    protected virtual void Update()
    {
        currentLifeTimer -= Time.deltaTime;
        if (currentLifeTimer <= 0)
        {
            ReturnToGlobalPool();
        }
    }

    public abstract void HandleHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal);

    protected void ReturnToGlobalPool()
    {
        if (GlobalProjectilePool.Instance != null && originalPrefabReference != null)
        {
            GlobalProjectilePool.Instance.ReturnToPool(this, originalPrefabReference);
        }
        else
        {
            gameObject.SetActive(false); 
        }
    }
}