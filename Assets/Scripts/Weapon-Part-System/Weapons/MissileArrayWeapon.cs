using UnityEngine;
using System.Collections.Generic;

public class MissileArrayWeapon : FunctionalWeapon
{
    public enum LaunchTrajectory { Direct, Vertical }

    [Header("Missile Array Settings")]
    public List<Transform> muzzlePoints = new List<Transform>();
    public LaunchTrajectory trajectory = LaunchTrajectory.Direct;
    
    [Tooltip("Time between each missile leaving the tube in a single burst.")]
    public float staggerTime = 0.1f;

    private FCSLockBox _fcs;
    private Rifle _missileStats; 
    private float _nextFireTime = 0f;
    private float _nextStaggerTime = 0f;
    
    // Burst Tracking
    private bool _isFiringBurst = false;
    private int _currentBarrelIndex = 0;
    private int _missilesToFireThisBurst = 0;
    private int _missilesFiredSoFar = 0;
    
    // NEW: The weapon must remember who we were looking at when the trigger was pulled!
    private Transform _burstTarget;

    public override void InitializeWeapon(Part data)
    {
        base.InitializeWeapon(data); 
        _missileStats = data as Rifle;
        
        _fcs = transform.root.GetComponentInChildren<FCSLockBox>();

        if (_missileStats != null && _missileStats.bulletPrefab != null && GlobalProjectilePool.Instance != null)
        {
            GlobalProjectilePool.Instance.PreWarm(_missileStats.bulletPrefab, muzzlePoints.Count * 3);
        }
    }

    public override void OnFirePressed()
    {
        if (_missileStats == null || muzzlePoints.Count == 0) return;

        if (!_isFiringBurst && Time.time >= _nextFireTime && currentResource > 0)
        {
            int currentLocks = 1; 
            _burstTarget = null; // Default to no target (dumb-fire)

            if (_fcs != null && _fcs.isHardLocked)
            {
                currentLocks = _fcs.currentLockCount;
                
                // 1. SAVE THE TARGET!
                _burstTarget = _fcs.currentTarget; 
                
                // 2. Now it is safe to reset the FCS
                _fcs.ConsumeLocks(); 
            }

            _missilesToFireThisBurst = Mathf.Min(currentLocks, muzzlePoints.Count, Mathf.FloorToInt(currentResource));
            
            if (_missilesToFireThisBurst > 0)
            {
                _isFiringBurst = true;
                _missilesFiredSoFar = 0;
                _currentBarrelIndex = 0;
                _nextStaggerTime = Time.time; 
            }
        }
    }

    private void Update()
    {
        if (_isFiringBurst && Time.time >= _nextStaggerTime)
        {
            FireMissileFromCurrentTube();

            _missilesFiredSoFar++;
            _currentBarrelIndex++; 
            _nextStaggerTime = Time.time + staggerTime;

            if (_missilesFiredSoFar >= _missilesToFireThisBurst || currentResource <= 0)
            {
                _isFiringBurst = false;
                _nextFireTime = Time.time + (_missileStats.firingInterval / 1000f);
            }
        }
    }

    private void FireMissileFromCurrentTube()
    {
        Transform muzzle = muzzlePoints[_currentBarrelIndex];
        
        Quaternion spawnRotation = muzzle.rotation;
        if (trajectory == LaunchTrajectory.Vertical)
        {
            spawnRotation = Quaternion.LookRotation(muzzle.up, -muzzle.forward);
        }

        BaseProjectile proj = GlobalProjectilePool.Instance.GetProjectile(
            _missileStats.bulletPrefab, muzzle.position, spawnRotation);

        proj.SetupStats(_missileStats.attackPower, _missileStats.bulletSpeed);
        proj.SetPrefabReference(_missileStats.bulletPrefab);

        // Pass the saved target instead of asking the FCS!
        if (proj is HomingMissile homingMissile)
        {
            homingMissile.SetHomingData(_burstTarget);
        }

        currentResource--;
        NotifyResourceChange();
    }

    public override void OnFireHeld() { }
    public override void OnFireReleased() { } 
}