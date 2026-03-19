using UnityEngine;

[CreateAssetMenu(fileName = "New Shotgun Part", menuName = "Mech/Weapons/Shotgun Part")]
public class ShotgunPart : ProjectileWeaponPart
{
    [Header("Shotgun Specific Stats")]
    [Tooltip("How many individual pellets are spawned per shot?")]
    public int pelletCount = 8;
    
    [Tooltip("The maximum angle of deviation (in degrees) from the center crosshair.")]
    public float spreadAngle = 7.5f;
}