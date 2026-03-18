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

    [Header("Testing (No FCS Yet)")]
    [Tooltip("Change this to test how the array handles partial lock-ons.")]
    public int simulatedLocks = 4;

    private Rifle _missileStats; 
    private float _nextFireTime = 0f;
    private float _nextStaggerTime = 0f;
    
    // Burst Tracking
    private bool _isFiringBurst = false;
    private int _currentBarrelIndex = 0;
    private int _missilesToFireThisBurst = 0;
    private int _missilesFiredSoFar = 0;

    public override void InitializeWeapon(Part data)
    {
        base.InitializeWeapon(data); 
        _missileStats = data as Rifle;

        if (_missileStats != null && _missileStats.bulletPrefab != null && GlobalProjectilePool.Instance != null)
        {
            GlobalProjectilePool.Instance.PreWarm(_missileStats.bulletPrefab, muzzlePoints.Count * 3);
        }
    }

    // Moved to OnFirePressed so it only takes a single click!
    public override void OnFirePressed()
    {
        if (_missileStats == null || muzzlePoints.Count == 0) return;

        // Only start a new salvo if we aren't currently firing one, the weapon cooled down, and we have ammo
        if (!_isFiringBurst && Time.time >= _nextFireTime && currentResource > 0)
        {
            // --- FCS LOCK SIMULATION ---
            // TODO: Later, we will replace 'simulatedLocks' with: fcsLockBox.GetCurrentLockCount()
            int currentLocks = simulatedLocks; 
            
            // Calculate the exact size of the salvo. 
            // It takes the SMALLEST number out of: your locks, your tubes, or your remaining ammo.
            _missilesToFireThisBurst = Mathf.Min(currentLocks, muzzlePoints.Count, Mathf.FloorToInt(currentResource));
            
            if (_missilesToFireThisBurst > 0)
            {
                _isFiringBurst = true;
                _missilesFiredSoFar = 0;
                _currentBarrelIndex = 0;
                _nextStaggerTime = Time.time; // Force the first missile to fire immediately
            }
        }
    }

    // We use Unity's Update loop to handle the stagger so you don't have to hold the button
    private void Update()
    {
        if (_isFiringBurst && Time.time >= _nextStaggerTime)
        {
            FireMissileFromCurrentTube();

            _missilesFiredSoFar++;
            _currentBarrelIndex++; // Move to the next tube
            _nextStaggerTime = Time.time + staggerTime;

            // If we fired the amount we locked onto, OR we ran out of ammo mid-burst, stop.
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

        // Temporarily passing null since the FCS isn't built yet
        if (proj is HomingMissile homingMissile)
        {
            homingMissile.SetHomingData(null);
        }

        currentResource--;
        NotifyResourceChange();
    }

    // We leave these empty now!
    public override void OnFireHeld() { }
    public override void OnFireReleased() { } 
}