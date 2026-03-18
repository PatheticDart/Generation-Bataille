using UnityEngine;

public class BasicProjectileWeapon : FunctionalWeapon
{
    [Header("Weapon Setup")]
    [Tooltip("Where the projectile will spawn. Create an empty GameObject child for this.")]
    public Transform muzzlePoint;
    public GameObject projectilePrefab;

    [Header("Testing Stats")]
    public float fireRate = 0.2f; // Time in seconds between shots
    public float projectileSpeed = 100f;

    private float _nextFireTime = 0f;

    // Triggered by your WeaponManager when the input is held
    public override void OnFireHeld()
    {
        if (Time.time >= _nextFireTime)
        {
            Fire();
            _nextFireTime = Time.time + fireRate;
        }
    }

    // You can also use this for semi-automatic firing
    public override void OnFirePressed()
    {
        // Optional: Add logic here if you want it to fire instantly on click 
        // regardless of fireRate, or keep it strictly tied to OnFireHeld.
    }

    private void Fire()
    {
        if (projectilePrefab == null || muzzlePoint == null)
        {
            Debug.LogWarning($"{gameObject.name}: Missing Muzzle Point or Projectile Prefab!");
            return;
        }

        // Spawn the projectile at the muzzle
        GameObject proj = Instantiate(projectilePrefab, muzzlePoint.position, muzzlePoint.rotation);

        // Try to apply physics if the projectile has a Rigidbody
        if (proj.TryGetComponent<Rigidbody>(out Rigidbody rb))
        {
            rb.linearVelocity = muzzlePoint.forward * projectileSpeed;
        }
        else
        {
            // Fallback for simple transform-based movement
            SimpleProjectile basicMovement = proj.AddComponent<SimpleProjectile>();
            basicMovement.speed = projectileSpeed;
        }

        // Optional: Play sound or muzzle flash particle effect here
    }
}