using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Slots")]
    public FunctionalWeapon[] lWeapons = new FunctionalWeapon[2];
    public FunctionalWeapon[] rWeapons = new FunctionalWeapon[2];

    public void RegisterWeapon(bool isLeft, int slotIndex, FunctionalWeapon weapon)
    {
        if (slotIndex < 0 || slotIndex >= 2) return;
        if (isLeft) lWeapons[slotIndex] = weapon;
        else rWeapons[slotIndex] = weapon;
    }

    // This is called by the Integration script
    public void FireWeapon(bool isLeft, int slotIndex, bool pressed, bool held, bool released)
    {
        FunctionalWeapon[] bank = isLeft ? lWeapons : rWeapons;
        FunctionalWeapon weapon = bank[slotIndex];

        if (weapon == null) return;

        if (pressed) weapon.OnFirePressed();
        if (held) weapon.OnFireHeld();
        if (released) weapon.OnFireReleased();
    }
    
    public void ForceRelease(bool isLeft, int slotIndex)
    {
        FunctionalWeapon[] bank = isLeft ? lWeapons : rWeapons;
        if (bank[slotIndex] != null) bank[slotIndex].OnFireReleased();
    }
}