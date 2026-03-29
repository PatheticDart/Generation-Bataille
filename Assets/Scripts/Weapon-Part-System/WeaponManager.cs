using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Slots")]
    public FunctionalWeapon[] lWeapons = new FunctionalWeapon[2];
    public FunctionalWeapon[] rWeapons = new FunctionalWeapon[2];

    // --- NEW: AUTO-RELOAD MONITOR ---
    private void Update()
    {
        CheckAndAutoReload(lWeapons);
        CheckAndAutoReload(rWeapons);
    }

    private void CheckAndAutoReload(FunctionalWeapon[] weapons)
    {
        for (int i = 0; i < weapons.Length; i++)
        {
            FunctionalWeapon weapon = weapons[i];

            // If the weapon exists and its current magazine is empty, trigger the reload
            // (Assumes your FunctionalWeapon.Reload() method already has an internal 
            // check to ignore the command if it is already in the middle of reloading).
            if (weapon != null && weapon.currentResource <= 0)
            {
                weapon.Reload();
            }
        }
    }

    public void RegisterWeapon(bool isLeft, int slotIndex, FunctionalWeapon weapon)
    {
        if (slotIndex < 0 || slotIndex >= 2) return;
        if (isLeft) lWeapons[slotIndex] = weapon;
        else rWeapons[slotIndex] = weapon;
    }

    // --- UI EXPOSED METHODS FOR AMMO ---

    // Safely returns the specific weapon so the UI can subscribe to its events
    public FunctionalWeapon GetWeapon(bool isLeft, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 2) return null;
        return isLeft ? lWeapons[slotIndex] : rWeapons[slotIndex];
    }

    public float GetCurrentResource(bool isLeft, int slotIndex)
    {
        FunctionalWeapon weapon = GetWeapon(isLeft, slotIndex);
        return weapon != null ? weapon.currentResource : 0f;
    }

    public float GetMaxResource(bool isLeft, int slotIndex)
    {
        FunctionalWeapon weapon = GetWeapon(isLeft, slotIndex);
        return weapon != null ? weapon.maxResource : 0f;
    }

    // --- INPUT ROUTING ---
    public void FireWeapon(bool isLeft, int slotIndex, bool pressed, bool held, bool released)
    {
        FunctionalWeapon weapon = GetWeapon(isLeft, slotIndex);
        if (weapon == null) return;

        if (pressed) weapon.OnFirePressed();
        if (held) weapon.OnFireHeld();
        if (released) weapon.OnFireReleased();
    }

    public void ForceRelease(bool isLeft, int slotIndex)
    {
        FunctionalWeapon weapon = GetWeapon(isLeft, slotIndex);
        if (weapon != null) weapon.OnFireReleased();
    }

    public void ReloadWeapon(bool isLeft, int slotIndex)
    {
        FunctionalWeapon weapon = GetWeapon(isLeft, slotIndex);
        if (weapon != null) weapon.Reload();
    }
}