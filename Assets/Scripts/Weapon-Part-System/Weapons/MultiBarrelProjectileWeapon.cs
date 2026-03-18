using UnityEngine;
using System.Collections.Generic;

public class MultiBarrelProjectileWeapon : FunctionalWeapon
{
    public enum FireMode { Sequential, Simultaneous }

    [Header("Multi-Barrel Setup")]
    [Tooltip("Add all muzzle points in the order they should fire.")]
    public List<Transform> muzzlePoints = new List<Transform>();
    
    public FireMode fireMode = FireMode.Sequential;

    private Rifle _rifleStats; 
    private float _nextFireTime = 0f;
    private int _currentBarrelIndex = 0;

    public override void InitializeWeapon(Part data)
    {
        base.InitializeWeapon(data); 
        _rifleStats = data as Rifle;

        if (_rifleStats == null) return;

        // Auto-calculate pool size
        if (_rifleStats.bulletPrefab != null && GlobalProjectilePool.Instance != null)
        {
            float shotsPerSecond = 1000f / _rifleStats.firingInterval;
            float maxAliveBullets = shotsPerSecond * _rifleStats.bulletPrefab.lifetime;
            
            // If simultaneous, we need a larger pool buffer
            int multiplier = (fireMode == FireMode.Simultaneous) ? muzzlePoints.Count : 1;
            int optimalPoolSize = (Mathf.CeilToInt(maxAliveBullets) * multiplier) + 5;

            GlobalProjectilePool.Instance.PreWarm(_rifleStats.bulletPrefab, optimalPoolSize);
        }
    }

    public override void OnFireHeld()
    {
        if (_rifleStats == null || muzzlePoints.Count == 0) return;

        if (Time.time >= _nextFireTime && currentResource > 0)
        {
            if (fireMode == FireMode.Sequential)
            {
                FireSequential();
            }
            else
            {
                FireSimultaneous();
            }

            _nextFireTime = Time.time + (_rifleStats.firingInterval / 1000f);
        }
    }

    private void FireSequential()
    {
        // 1. Get current barrel
        Transform currentMuzzle = muzzlePoints[_currentBarrelIndex];
        
        // 2. Spawn and setup bullet
        SpawnBullet(currentMuzzle);

        // 3. Subtract 1 ammo and notify
        currentResource--;
        NotifyResourceChange();

        // 4. Increment index for next shot
        _currentBarrelIndex = (_currentBarrelIndex + 1) % muzzlePoints.Count;
    }

    private void FireSimultaneous()
    {
        // 1. Fire from every barrel at once
        foreach (Transform muzzle in muzzlePoints)
        {
            if (currentResource <= 0) break;

            SpawnBullet(muzzle);
            
            // 2. Subtract 1 ammo per bullet spawned
            currentResource--;
        }

        NotifyResourceChange();
    }

    private void SpawnBullet(Transform muzzle)
    {
        if (_rifleStats.bulletPrefab == null || GlobalProjectilePool.Instance == null) return;

        BaseProjectile proj = GlobalProjectilePool.Instance.GetProjectile(
            _rifleStats.bulletPrefab, muzzle.position, muzzle.rotation);

        proj.SetupStats(_rifleStats.attackPower, _rifleStats.bulletSpeed);
        proj.SetPrefabReference(_rifleStats.bulletPrefab);
    }

    public override void OnFirePressed() { }
    public override void OnFireReleased() { }
}