using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Slots")]
    // Index 0 is Arm, Index 1 is Back/Shoulder
    public FunctionalWeapon[] lWeapons = new FunctionalWeapon[2];
    public FunctionalWeapon[] rWeapons = new FunctionalWeapon[2];

    #region State
    private int _activeL = 0;
    private int _activeR = 0;

    private bool lPressed, lHeld, lReleased;
    private bool rPressed, rHeld, rReleased;
    #endregion

    public void RegisterWeapon(bool isLeft, int slotIndex, FunctionalWeapon weapon)
    {
        if (isLeft)
        {
            lWeapons[slotIndex] = weapon;
        }
        else
        {
            rWeapons[slotIndex] = weapon;
        }
    }

    void Update()
    {
        GetInputs();
        ProcessFiring();
    }

    void GetInputs()
    {
        lPressed = Input.GetKeyDown(KeyCode.Mouse0);
        lHeld = Input.GetKey(KeyCode.Mouse0);
        lReleased = Input.GetKeyUp(KeyCode.Mouse0);

        rPressed = Input.GetKeyDown(KeyCode.Mouse1);
        rHeld = Input.GetKey(KeyCode.Mouse1);
        rReleased = Input.GetKeyUp(KeyCode.Mouse1);
    }

    void ProcessFiring()
    {
        // Handle Left Active Weapon
        FunctionalWeapon activeLeftWep = lWeapons[_activeL];
        if (activeLeftWep != null)
        {
            if (lPressed) activeLeftWep.OnFirePressed();
            if (lHeld) activeLeftWep.OnFireHeld();
            if (lReleased) activeLeftWep.OnFireReleased();
        }

        // Handle Right Active Weapon
        FunctionalWeapon activeRightWep = rWeapons[_activeR];
        if (activeRightWep != null)
        {
            if (rPressed) activeRightWep.OnFirePressed();
            if (rHeld) activeRightWep.OnFireHeld();
            if (rReleased) activeRightWep.OnFireReleased();
        }
    }

    // --- WEAPON TOGGLING ---
    public void ToggleLeftWeapon()
    {
        _activeL = (_activeL + 1) % lWeapons.Length;
    }

    public void ToggleRightWeapon()
    {
        _activeR = (_activeR + 1) % rWeapons.Length;
    }
}