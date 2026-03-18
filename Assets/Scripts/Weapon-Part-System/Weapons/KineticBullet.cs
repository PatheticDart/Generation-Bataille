using UnityEngine;

public class KineticBullet : RaycastProjectile
{
    public override void HandleHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal)
    {
        Destroy(gameObject);
    }
}