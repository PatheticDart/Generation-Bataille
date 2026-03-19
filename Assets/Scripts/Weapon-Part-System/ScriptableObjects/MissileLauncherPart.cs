using UnityEngine;

[CreateAssetMenu(fileName = "MissileLauncher", menuName = "Parts/MissileLauncher")]
public class MissileLauncherPart : ProjectileWeaponPart
{
    [Header("Missile Specifics")]
    public float staggerTime = 0.1f;
    public int maxLocks = 4;
    public LaunchTrajectory launchTrajectory = LaunchTrajectory.Direct;
}