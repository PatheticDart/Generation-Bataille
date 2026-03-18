using UnityEngine;

public class BasicProjectileWeapon : FunctionalWeapon
{
    [Header("Weapon Setup")]
    public Transform muzzlePoint;

    private Rifle _rifleStats; 
    private float _nextFireTime = 0f;

    public override void InitializeWeapon(Part data)
    {
        base.InitializeWeapon(data); // Sets maxResource and currentResource
        _rifleStats = data as Rifle;

        if (_rifleStats == null) return;

        // Pool warming logic remains the same
        if (_rifleStats.bulletPrefab != null && GlobalProjectilePool.Instance != null)
        {
            float shotsPerSecond = 1000f / _rifleStats.firingInterval;
            float maxAliveBullets = shotsPerSecond * _rifleStats.bulletPrefab.lifetime;
            int optimalPoolSize = Mathf.CeilToInt(maxAliveBullets) + 5;

            GlobalProjectilePool.Instance.PreWarm(_rifleStats.bulletPrefab, optimalPoolSize);
        }
    }

    public override void OnFireHeld()
    {
        if (_rifleStats == null) return;

        // Use currentResource (Ammo) from base class
        if (Time.time >= _nextFireTime && currentResource > 0)
        {
            Fire();
            _nextFireTime = Time.time + (_rifleStats.firingInterval / 1000f);
        }
    }

    private void Fire()
    {
        if (muzzlePoint == null || _rifleStats.bulletPrefab == null) return;

        // 1. Subtract resource and notify UI
        currentResource--;
        NotifyResourceChange();

        // 2. Spawn bullet
        BaseProjectile proj = GlobalProjectilePool.Instance.GetProjectile(
            _rifleStats.bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);

        proj.SetupStats(_rifleStats.attackPower, _rifleStats.bulletSpeed);
        proj.SetPrefabReference(_rifleStats.bulletPrefab);
    }

    public override void OnFirePressed() { }
    public override void OnFireReleased() { }
}