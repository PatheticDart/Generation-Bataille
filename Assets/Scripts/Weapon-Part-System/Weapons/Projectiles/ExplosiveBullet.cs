using UnityEngine;

public class ExplosiveBullet : RaycastProjectile
{
    [Header("Explosion Stats")]
    public float explosionRadius = 5f;
    public float explosionForce = 500f;

    public override void HandleHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
    {
        SpawnImpactEffect(hitPoint, hitNormal); // Play the visual explosion
        Detonate(hitPoint);                     // Do the physical damage
        InitiateReturn();                       // Start the trail fade
    }

    private void Detonate(Vector3 point)
    {
        Collider[] caughtInBlast = Physics.OverlapSphere(point, explosionRadius, hitMask);

        foreach (Collider col in caughtInBlast)
        {
            if (col.TryGetComponent(out Rigidbody rb))
            {
                rb.AddExplosionForce(explosionForce, point, explosionRadius);
            }
            // TODO: Pass the `damage` variable to the object's health script here
        }
    }
}