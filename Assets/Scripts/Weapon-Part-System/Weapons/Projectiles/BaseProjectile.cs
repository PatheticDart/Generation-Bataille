using UnityEngine;
using System.Collections;

public abstract class BaseProjectile : MonoBehaviour
{
    [Header("Universal Stats")]
    public float lifetime = 3f;
    public float damage = 50f;

    [Header("Visuals & Trails")]
    public Renderer[] visualRenderers;
    public TrailRenderer trailRenderer;

    [Header("Impact & VFX")]
    public PooledVFX impactEffectPrefab;

    protected BaseProjectile originalPrefabReference;
    private float currentLifeTimer;
    protected bool isReturning;

    // --- NEW: Remember who fired this! ---
    protected int shooterLayer;

    protected virtual void OnEnable()
    {
        currentLifeTimer = lifetime;
        isReturning = false;

        if (visualRenderers != null) foreach (Renderer r in visualRenderers) if (r != null) r.enabled = true;
        if (trailRenderer != null) trailRenderer.Clear();
    }

    public void SetPrefabReference(BaseProjectile prefabRef)
    {
        originalPrefabReference = prefabRef;
    }

    // --- NEW: Added shooterLayer to the setup ---
    public virtual void SetupStats(float newDamage, float newSpeed, int sourceLayer)
    {
        damage = newDamage;
        shooterLayer = sourceLayer;
    }

    protected virtual void Update()
    {
        if (isReturning) return;

        currentLifeTimer -= Time.deltaTime;
        if (currentLifeTimer <= 0) InitiateReturn();
    }

    public abstract void HandleHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal);

    protected void SpawnImpactEffect(Vector3 position, Vector3 normal)
    {
        if (impactEffectPrefab != null && GlobalVFXPool.Instance != null)
        {
            Quaternion rotation = (normal != Vector3.zero) ? Quaternion.LookRotation(normal) : Quaternion.identity;
            GlobalVFXPool.Instance.Spawn(impactEffectPrefab, position, rotation);
        }
    }

    protected void InitiateReturn()
    {
        if (isReturning) return;
        isReturning = true;

        if (visualRenderers != null) foreach (Renderer r in visualRenderers) if (r != null) r.enabled = false;

        float delay = (trailRenderer != null) ? trailRenderer.time : 0f;

        if (delay > 0f && gameObject.activeInHierarchy) StartCoroutine(DelayedReturn(delay));
        else ReturnToGlobalPool();
    }

    private IEnumerator DelayedReturn(float delay)
    {
        yield return new WaitForSeconds(delay);
        ReturnToGlobalPool();
    }

    protected void ReturnToGlobalPool()
    {
        if (GlobalProjectilePool.Instance != null && originalPrefabReference != null)
            GlobalProjectilePool.Instance.ReturnToPool(this, originalPrefabReference);
        else
            gameObject.SetActive(false);
    }
}