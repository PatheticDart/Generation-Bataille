using UnityEngine;
using System.Collections; 

public abstract class BaseProjectile : MonoBehaviour
{
    [Header("Universal Stats")]
    public float lifetime = 3f;
    public float damage = 50f;

    [Header("Visuals & Trails")]
    [Tooltip("Assign the mesh renderer(s) of the projectile to hide on impact.")]
    public Renderer[] visualRenderers;
    [Tooltip("Assign the Trail Renderer so it can be cleared on spawn and dictate the fade delay.")]
    public TrailRenderer trailRenderer;
    
    [Header("Impact & VFX")]
    [Tooltip("The VFX spawned when this projectile hits a surface or detonates.")]
    public PooledVFX impactEffectPrefab;

    protected BaseProjectile originalPrefabReference;
    private float currentLifeTimer;
    protected bool isReturning; 

    protected virtual void OnEnable()
    {
        currentLifeTimer = lifetime;
        isReturning = false;

        if (visualRenderers != null)
        {
            foreach (Renderer r in visualRenderers)
            {
                if (r != null) r.enabled = true;
            }
        }

        if (trailRenderer != null)
        {
            trailRenderer.Clear();
        }
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
        if (isReturning) return; 

        currentLifeTimer -= Time.deltaTime;
        if (currentLifeTimer <= 0)
        {
            InitiateReturn();
        }
    }

    public abstract void HandleHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal);

    // --- NEW: UNIVERSAL IMPACT VFX ---
    protected void SpawnImpactEffect(Vector3 position, Vector3 normal)
    {
        if (impactEffectPrefab != null && GlobalVFXPool.Instance != null)
        {
            // If we have a valid normal, rotate the VFX to face away from the surface
            Quaternion rotation = (normal != Vector3.zero) ? Quaternion.LookRotation(normal) : Quaternion.identity;
            GlobalVFXPool.Instance.Spawn(impactEffectPrefab, position, rotation);
        }
    }

    // --- UNIVERSAL POOLING LOGIC ---
    protected void InitiateReturn()
    {
        if (isReturning) return;
        isReturning = true;

        if (visualRenderers != null)
        {
            foreach (Renderer r in visualRenderers)
            {
                if (r != null) r.enabled = false;
            }
        }

        float delay = (trailRenderer != null) ? trailRenderer.time : 0f;
        
        if (delay > 0f && gameObject.activeInHierarchy)
        {
            StartCoroutine(DelayedReturn(delay));
        }
        else
        {
            ReturnToGlobalPool();
        }
    }

    private IEnumerator DelayedReturn(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToGlobalPool();
    }

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