using UnityEngine;

public class BasicProjectileWeapon : FunctionalWeapon
{
    [Header("Weapon Setup")]
    [Tooltip("Where the projectile will spawn. Create an empty GameObject child for this.")]
    public Transform muzzlePoint;

    private Rifle _rifleStats; 
    
    #region State
        private int _currentAmmo;
        private float _nextFireTime = 0f;
    #endregion

    // 1. Receive and store the stats when the mech is initialized
    public override void InitializeWeapon(Part data)
    {
        base.InitializeWeapon(data); 
        _rifleStats = data as Rifle;

        if (_rifleStats == null) return;

        if (_rifleStats.bulletPrefab != null && GlobalProjectilePool.Instance != null)
        {
            // 1. Calculate shots per second (1000ms / firing interval)
            float shotsPerSecond = 1000f / _rifleStats.firingInterval;
            
            // 2. Multiply by the bullet's maximum lifetime
            float maxAliveBullets = shotsPerSecond * _rifleStats.bulletPrefab.lifetime;
            
            // 3. Round up to the nearest whole number, and add a safety buffer of 5
            int optimalPoolSize = Mathf.CeilToInt(maxAliveBullets) + 5;

            // 4. Tell the global pool to warm up the exact optimal amount!
            GlobalProjectilePool.Instance.PreWarm(_rifleStats.bulletPrefab, optimalPoolSize);
            
            Debug.Log($"[{gameObject.name}] Auto-calculated optimal pool size: {optimalPoolSize}");
        }
    }

    public override void OnFireHeld()
    {
        if (_rifleStats == null) return;

        // Check if the fire rate timer has passed AND if we actually have bullets left
        if (Time.time >= _nextFireTime && _currentAmmo > 0)
        {
            Fire();
            _nextFireTime = Time.time + (_rifleStats.firingInterval / 1000f);
        }
        else if (_currentAmmo <= 0 && Time.time >= _nextFireTime)
        {
            // TODO: Play an "empty magazine click" sound effect here!
        }
    }

    private void Fire()
    {
        if (muzzlePoint == null || _rifleStats.bulletPrefab == null) return;

        if (GlobalProjectilePool.Instance == null)
        {
            Debug.LogError("GlobalProjectilePool is missing from the scene!");
            return;
        }

        _currentAmmo--;

        BaseProjectile proj = GlobalProjectilePool.Instance.GetProjectile(_rifleStats.bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);

        // Pass the stats from the gun into the newly spawned bullet
        proj.SetupStats(_rifleStats.attackPower, _rifleStats.bulletSpeed);
        proj.SetPrefabReference(_rifleStats.bulletPrefab);

        // TODO: Play sound or muzzle flash particle effect here
    }
}