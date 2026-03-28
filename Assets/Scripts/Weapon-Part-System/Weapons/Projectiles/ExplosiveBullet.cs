using UnityEngine;
using System.Collections.Generic;

public class ExplosiveBullet : RaycastProjectile
{
    [Header("Explosion Stats")]
    public float explosionRadius = 5f;
    public float explosionForce = 500f;

    public override void HandleHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
    {
        SpawnImpactEffect(hitPoint, hitNormal);
        Detonate(hitPoint);
        InitiateReturn();
    }

    private void Detonate(Vector3 point)
    {
        Collider[] caughtInBlast = Physics.OverlapSphere(point, explosionRadius, hitMask);

        // --- NEW: Track damaged mechs so an explosion hitting an arm and a leg doesn't double-damage! ---
        HashSet<MechStats> alreadyDamagedMechs = new HashSet<MechStats>();

        foreach (Collider col in caughtInBlast)
        {
            // Ignore Friendly Fire
            if (col.gameObject.layer == shooterLayer) continue;

            // Apply Damage (Only once per mech!)
            MechStats enemyStats = col.GetComponentInParent<MechStats>();
            if (enemyStats != null && !alreadyDamagedMechs.Contains(enemyStats))
            {
                enemyStats.currentArmorPoints -= (int)damage;
                if (enemyStats.currentArmorPoints < 0) enemyStats.currentArmorPoints = 0;

                alreadyDamagedMechs.Add(enemyStats);
            }

            if (col.TryGetComponent(out Rigidbody rb))
            {
                rb.AddExplosionForce(explosionForce, point, explosionRadius);
            }
        }
    }
}