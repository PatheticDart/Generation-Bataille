using UnityEngine;

public class ExplosiveBullet : RaycastProjectile
{
    [Header("Explosion Stats")]
    public float explosionRadius = 5f;
    public float explosionForce = 500f;
    
    [Tooltip("Optional: Drop a particle effect prefab here to spawn on impact.")]
    public GameObject explosionEffectPrefab; 

    public override void HandleHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
    {
        Detonate(hitPoint);
        ReturnToGlobalPool();
    }

    private void Detonate(Vector3 point)
    {
        // 1. Play visual effect
        if (explosionEffectPrefab != null)
        {
            // Note: For a fully optimized game, you'd want to Pool your explosions too!
            Instantiate(explosionEffectPrefab, point, Quaternion.identity);
        }

        // 2. Find everything in the blast radius
        Collider[] caughtInBlast = Physics.OverlapSphere(point, explosionRadius, hitMask);

        foreach (Collider col in caughtInBlast)
        {
            // Apply physical force if the object has a Rigidbody
            if (col.TryGetComponent<Rigidbody>(out Rigidbody rb))
            {
                rb.AddExplosionForce(explosionForce, point, explosionRadius);
            }

            // TODO: Pass the `damage` variable to the object's health script here!
            // Example: if (col.TryGetComponent<MechHealth>(out MechHealth hp)) hp.TakeDamage(damage);
        }
    }
}