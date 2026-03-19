using UnityEngine;

public class BasicProjectileWeapon : FunctionalWeapon
{
    [Header("Weapon Setup")]
    public Transform muzzlePoint;
    public PooledVFX muzzleFlash; // NEW: Assign your flash prefab here!

    private Rifle _rifleStats; 
    private float _nextFireTime = 0f;

    public override void InitializeWeapon(Part data)
    {
        base.InitializeWeapon(data); 
        _rifleStats = data as Rifle;

        if (_rifleStats == null) return;

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

        if (Time.time >= _nextFireTime && currentResource > 0)
        {
            Fire();
            _nextFireTime = Time.time + (_rifleStats.firingInterval / 1000f);
        }
    }

    private void Fire()
    {
        if (muzzlePoint == null || _rifleStats.bulletPrefab == null) return;

        currentResource--;
        NotifyResourceChange();

        // NEW: Play the flash!
        PlayMuzzleFlash(muzzleFlash, muzzlePoint);

        BaseProjectile proj = GlobalProjectilePool.Instance.GetProjectile(
            _rifleStats.bulletPrefab, muzzlePoint.position, muzzlePoint.rotation);

        proj.SetupStats(_rifleStats.attackPower, _rifleStats.bulletSpeed);
        proj.SetPrefabReference(_rifleStats.bulletPrefab);
    }

    public override void OnFirePressed() { }
    public override void OnFireReleased() { }
}