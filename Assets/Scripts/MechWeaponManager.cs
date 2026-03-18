using UnityEngine;
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

    private bool isLeftTransitioning = false;
    private bool isRightTransitioning = false;

    private bool canFireLeft = true;
    private bool canFireRight = true;

    private bool leftArmTargetState = true;
    private bool rightArmTargetState = true;

    private int LSlot => leftArmTargetState ? 0 : 1;
    private int RSlot => rightArmTargetState ? 0 : 1;

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
        if (!isLeftTransitioning) StartCoroutine(ToggleLeftSequence());
    }

    public void ToggleRightWeapon()
    {
        if (!isRightTransitioning) StartCoroutine(ToggleRightSequence());
    }

    public void ProcessLeftFire(bool pressed, bool held, bool released)
    {
        if (weaponManager == null) return;

        if (canFireLeft)
        {
            weaponManager.FireWeapon(true, LSlot, pressed, held, released);
        }
        else
        {
            weaponManager.ForceRelease(true, LSlot);
        }
    }

    public void ProcessRightFire(bool pressed, bool held, bool released)
    {
        if (weaponManager == null) return;

        if (canFireRight)
        {
            weaponManager.FireWeapon(false, RSlot, pressed, held, released);
        }
        else
        {
            weaponManager.ForceRelease(false, RSlot);
        }
    }

    // --- SEQUENCING COROUTINES ---

    private IEnumerator ToggleLeftSequence()
    {
        isLeftTransitioning = true;
        canFireLeft = false;

        weaponManager.ForceRelease(true, LSlot);

        if (leftArmTargetState)
        {
            leftArmTargetState = false;

            leftArmActive = false;
            yield return new WaitForSeconds(armTransitionTime);

            leftBackActive = true;
            yield return new WaitForSeconds(backWeaponTransitionTime);
        }
        else
        {
            leftArmTargetState = true;

            leftBackActive = false;
            yield return new WaitForSeconds(backWeaponTransitionTime);

            leftArmActive = true;
            yield return new WaitForSeconds(armTransitionTime);
        }

        canFireLeft = true;
        isLeftTransitioning = false;
    }

    private IEnumerator ToggleRightSequence()
    {
        isRightTransitioning = true;
        canFireRight = false;

        weaponManager.ForceRelease(false, RSlot);

        if (rightArmTargetState)
        {
            rightArmTargetState = false;

            rightArmActive = false;
            yield return new WaitForSeconds(armTransitionTime);

            rightBackActive = true;
            yield return new WaitForSeconds(backWeaponTransitionTime);
        }
        else
        {
            rightArmTargetState = true;

            rightBackActive = false;
            yield return new WaitForSeconds(backWeaponTransitionTime);

            rightArmActive = true;
            yield return new WaitForSeconds(armTransitionTime);
        }

        canFireRight = true;
        isRightTransitioning = false;
    }
}