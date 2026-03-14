using UnityEngine;

public class MechWeaponManager : MonoBehaviour
{
    [Header("Dependencies")]
    public WeaponManager weaponManager;

    [Header("Current Active States")]
    public bool leftArmActive = true; 
    public bool rightArmActive = true;

    [Header("Loadout Capabilities")]
    // ADD THESE TWO LINES: The AimController needs these!
    public bool hasAimableLeftBackWeapon = true;
    public bool hasAimableRightBackWeapon = true;

    [Header("Player Control")]
    public bool isPlayerControlled = true;

    private int LSlot => leftArmActive ? 0 : 1;
    private int RSlot => rightArmActive ? 0 : 1;

    void Update()
    {
        if (!isPlayerControlled || weaponManager == null) return;

        if (Input.GetKeyDown(KeyCode.Q)) ToggleLeft();
        if (Input.GetKeyDown(KeyCode.E)) ToggleRight();

        weaponManager.FireWeapon(true, LSlot, 
            Input.GetKeyDown(KeyCode.Mouse0), 
            Input.GetKey(KeyCode.Mouse0), 
            Input.GetKeyUp(KeyCode.Mouse0));

        weaponManager.FireWeapon(false, RSlot, 
            Input.GetKeyDown(KeyCode.Mouse1), 
            Input.GetKey(KeyCode.Mouse1), 
            Input.GetKeyUp(KeyCode.Mouse1));
    }

    public void ToggleLeft()
    {
        weaponManager.ForceRelease(true, LSlot);
        leftArmActive = !leftArmActive;
    }

    public void ToggleRight()
    {
        weaponManager.ForceRelease(false, RSlot);
        rightArmActive = !rightArmActive;
    }
}