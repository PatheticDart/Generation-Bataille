using UnityEngine;

public class ExplosiveBullet : RaycastProjectile
{
    [Header("Explosion Stats")]
    public float explosionRadius = 5f;
    public float explosionForce = 500f;
    
    [Tooltip("Optional: Drop a particle effect prefab here to spawn on impact.")]
    public PooledVFX explosionEffectPrefab;

    public override void HandleHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
    {
        Detonate(hitPoint);
        ReturnToGlobalPool();
    }

    private void Detonate(Vector3 point)
    {
        // 1. Play visual effect
        // CHANGE THIS from Instantiate to GlobalVFXPool.Spawn:
        if (explosionEffectPrefab != null && GlobalVFXPool.Instance != null)
        {
            GlobalVFXPool.Instance.Spawn(explosionEffectPrefab, point, Quaternion.identity);
        }

        // 2. Find everything in the blast radius
        Collider[] caughtInBlast = Physics.OverlapSphere(point, explosionRadius, hitMask);

        foreach (Collider col in caughtInBlast)
        {
            // Apply physical force if the object has a Rigidbody
            if (col.TryGetComponent(out Rigidbody rb))
            {
                rb.AddExplosionForce(explosionForce, point, explosionRadius);
            }

            // TODO: Pass the `damage` variable to the object's health script here!
        }
    }
}