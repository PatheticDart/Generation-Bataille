using UnityEngine;

public class MechWeaponManager : MonoBehaviour
{
    [Header("Current Active States")]
    public bool leftArmActive = true;  // True = Arm, False = Back
    public bool rightArmActive = true; // True = Arm, False = Back

    [Header("Loadout Capabilities")]
    // The Loader or Loadout system will set these to true if the equipped back weapon CAN aim.
    public bool hasAimableLeftBackWeapon = true;
    public bool hasAimableRightBackWeapon = true;

    [Header("Player Control")]
    public bool isPlayerControlled = true;


    void Update()
    {
        // Only listen to keyboard if this is the player
        if (isPlayerControlled)
        {
            if (Input.GetKeyDown(KeyCode.Q)) ToggleLeftWeapon();
            if (Input.GetKeyDown(KeyCode.E)) ToggleRightWeapon();
        }
    }

    public void ToggleLeftWeapon()
    {
        leftArmActive = !leftArmActive;
            Debug.Log("Left Weapon Switched! Arm Active: " + leftArmActive);
    }

    public void ToggleRightWeapon()
    {
        rightArmActive = !rightArmActive;
        Debug.Log("Right Weapon Switched! Arm Active: " + rightArmActive);
    }
}