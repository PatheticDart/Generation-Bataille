using UnityEngine;
using System;
using System.Collections;

public class MechWeaponManager : MonoBehaviour
{
    [Header("Dependencies")]
    public WeaponManager weaponManager;

    [Header("Current Active States")]
    public bool leftArmActive = true;
    public bool rightArmActive = true;

    [Header("Back Weapon States (For Sequencing)")]
    public bool leftBackActive = false;
    public bool rightBackActive = false;

    [Header("Transition Timings")]
    public float armTransitionTime = 0.3f;
    public float backWeaponTransitionTime = 0.6f;

    [Header("Loadout Capabilities")]
    public bool hasAimableLeftBackWeapon = true;
    public bool hasAimableRightBackWeapon = true;

    // --- UI EXPOSED PROPERTIES ---
    // The UI can read these, but only this script can change them
    public bool IsLeftTransitioning { get; private set; } = false;
    public bool IsRightTransitioning { get; private set; } = false;

    public int ActiveLeftSlot => leftArmTargetState ? 0 : 1;
    public int ActiveRightSlot => rightArmTargetState ? 0 : 1;

    // --- UI EXPOSED EVENTS ---
    // UI scripts can subscribe to these to know exactly when to play animations or swap icons
    public event Action<bool, int> OnWeaponSwapStarted;   // Returns (isLeft, targetSlotIndex)
    public event Action<bool, int> OnWeaponSwapCompleted; // Returns (isLeft, activeSlotIndex)

    private bool canFireLeft = true;
    private bool canFireRight = true;

    private bool leftArmTargetState = true;
    private bool rightArmTargetState = true;

    void Start()
    {
        leftBackActive = !leftArmActive;
        rightBackActive = !rightArmActive;
        leftArmTargetState = leftArmActive;
        rightArmTargetState = rightArmActive;
    }

    // --- PUBLIC METHODS FOR BRAINS (PLAYER OR AI) TO CALL ---

    public void ToggleLeftWeapon()
    {
        if (!IsLeftTransitioning) StartCoroutine(ToggleLeftSequence());
    }

    public void ToggleRightWeapon()
    {
        if (!IsRightTransitioning) StartCoroutine(ToggleRightSequence());
    }

    public void ProcessLeftFire(bool pressed, bool held, bool released)
    {
        if (weaponManager == null) return;

        if (canFireLeft) weaponManager.FireWeapon(true, ActiveLeftSlot, pressed, held, released);
        else weaponManager.ForceRelease(true, ActiveLeftSlot);
    }

    public void ProcessRightFire(bool pressed, bool held, bool released)
    {
        if (weaponManager == null) return;

        if (canFireRight) weaponManager.FireWeapon(false, ActiveRightSlot, pressed, held, released);
        else weaponManager.ForceRelease(false, ActiveRightSlot);
    }

    // --- SEQUENCING COROUTINES ---

    private IEnumerator ToggleLeftSequence()
    {
        IsLeftTransitioning = true;
        canFireLeft = false;
        weaponManager.ForceRelease(true, ActiveLeftSlot);

        // Tell the UI a swap just started, and pass the slot we are moving TO
        leftArmTargetState = !leftArmTargetState; 
        OnWeaponSwapStarted?.Invoke(true, ActiveLeftSlot);

        if (!leftArmTargetState) // Switching to Back (Slot 1)
        {
            leftArmActive = false;
            yield return new WaitForSeconds(armTransitionTime);

            leftBackActive = true;
            yield return new WaitForSeconds(backWeaponTransitionTime);
        }
        else // Switching to Arm (Slot 0)
        {
            leftBackActive = false;
            yield return new WaitForSeconds(backWeaponTransitionTime);

            leftArmActive = true;
            yield return new WaitForSeconds(armTransitionTime);
        }

        canFireLeft = true;
        IsLeftTransitioning = false;
        
        // Tell the UI the swap is completely done
        OnWeaponSwapCompleted?.Invoke(true, ActiveLeftSlot);
    }

    private IEnumerator ToggleRightSequence()
    {
        IsRightTransitioning = true;
        canFireRight = false;
        weaponManager.ForceRelease(false, ActiveRightSlot);

        rightArmTargetState = !rightArmTargetState;
        OnWeaponSwapStarted?.Invoke(false, ActiveRightSlot);

        if (!rightArmTargetState)
        {
            rightArmActive = false;
            yield return new WaitForSeconds(armTransitionTime);

            rightBackActive = true;
            yield return new WaitForSeconds(backWeaponTransitionTime);
        }
        else
        {
            rightBackActive = false;
            yield return new WaitForSeconds(backWeaponTransitionTime);

            rightArmActive = true;
            yield return new WaitForSeconds(armTransitionTime);
        }

        canFireRight = true;
        IsRightTransitioning = false;
        
        OnWeaponSwapCompleted?.Invoke(false, ActiveRightSlot);
    }
}