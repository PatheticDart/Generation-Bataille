using UnityEngine;

public class KineticBullet : RaycastProjectile
{
    public override void HandleHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
    {
        // 1. Ignore Friendly Fire
        if (hitObject.layer == shooterLayer) return;

        // 2. Try to find MechStats and deal damage
        MechStats enemyStats = hitObject.GetComponentInParent<MechStats>();
        if (enemyStats != null)
        {
            enemyStats.currentArmorPoints -= (int)damage;

            // Optional: Clamp to 0
            if (enemyStats.currentArmorPoints < 0) enemyStats.currentArmorPoints = 0;
        }

        SpawnImpactEffect(hitPoint, hitNormal);
        InitiateReturn();
    }
}