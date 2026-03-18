using UnityEngine;

public abstract class BaseProjectile : MonoBehaviour
{
    [Header("Universal Stats")]
    public float lifetime = 3f;
    public float damage = 50f;

    public virtual void InitializeBullet()
    {
        Destroy(gameObject, lifetime);
    }

    public virtual void SetupStats(float newDamage, float newSpeed)
    {
        damage = newDamage;
        // We leave speed for the child classes to handle, since physics and hitscan handle speed differently
    }

    protected virtual void Update() {}
    protected virtual void FixedUpdate() {}

    public abstract void HandleHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal);
}