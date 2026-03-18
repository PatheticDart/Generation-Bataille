using UnityEngine;

public class BasicProjectileWeapon : FunctionalWeapon
{
    [Header("Weapon Setup")]
    [Tooltip("Where the projectile will spawn. Create an empty GameObject child for this.")]
    public Transform muzzlePoint;

    // The data container passed in from the PartSystem
    private Rifle _rifleStats; 
    
    private float _nextFireTime = 0f;

    // 1. Receive and store the stats when the mech is initialized
    public override void InitializeWeapon(Part data)
    {
        base.InitializeWeapon(data);
        
        // Cast the generic Part data to our specific Rifle data
        _rifleStats = data as Rifle;

        if (_rifleStats == null)
        {
            Debug.LogError($"[{gameObject.name}] Initialization failed: Provided data is not a Rifle Part!");
        }
    }

    // 2. Handle the firing logic using the ScriptableObject stats
    public override void OnFireHeld()
    {
        if (_rifleStats == null) return;

        if (Time.time >= _nextFireTime)
        {
            Fire();
            
            // Assuming firingInterval is in milliseconds (e.g., 100 for 10 shots a second)
            _nextFireTime = Time.time + (_rifleStats.firingInterval / 1000f);
        }
    }

    public override void OnFirePressed()
    {
        // Optional: If you want pulling the trigger to immediately fire 
        // regardless of the automatic fire rate, you can put a check here.
    }

    private void Fire()
    {
        if (muzzlePoint == null)
        {
            Debug.LogWarning($"{gameObject.name}: Missing Muzzle Point!");
            return;
        }

        if (_rifleStats.bulletPrefab == null)
        {
            Debug.LogWarning($"{gameObject.name}: Missing Bullet Prefab in the Rifle ScriptableObject!");
            return;
        }

        // Spawn the exact projectile defined in your Garage/ScriptableObject
        BaseProjectile proj = Instantiate(_rifleStats.bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);

        // Pass the stats from the gun into the newly spawned bullet
        proj.SetupStats(_rifleStats.attackPower, _rifleStats.bulletSpeed);

        // TODO: Ammo depletion logic goes here

        // Optional: Play sound or muzzle flash particle effect here
    }
}