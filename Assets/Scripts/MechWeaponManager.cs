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
    public bool leftBackActive = false;
    public bool rightBackActive = false;

    [Header("Transition Timings")]
    public float armTransitionTime = 0.3f;
    public float backWeaponTransitionTime = 0.6f;

    // --- DYNAMIC CAPABILITIES ---
    // Names matched exactly to your AimController to fix CS1061
    public bool hasAimableLeftBackWeapon { get; private set; }
    public bool hasAimableRightBackWeapon { get; private set; }

    public bool IsLeftTransitioning { get; private set; } = false;
    public bool IsRightTransitioning { get; private set; } = false;

    public int ActiveLeftSlot => leftArmTargetState ? 0 : 1;
    public int ActiveRightSlot => rightArmTargetState ? 0 : 1;

    public event Action<bool, int> OnWeaponSwapStarted;
    public event Action<bool, int> OnWeaponSwapCompleted;

    private bool canFireLeft = true;
    private bool canFireRight = true;
    private bool leftArmTargetState = true;
    private bool rightArmTargetState = true;

    void Start()
    {
        leftArmTargetState = leftArmActive;
        rightArmTargetState = rightArmActive;
        RefreshLoadoutCapabilities();
    }

    public void RefreshLoadoutCapabilities()
    {
        if (weaponManager == null) return;
        hasAimableLeftBackWeapon = CheckAimable(true);
        hasAimableRightBackWeapon = CheckAimable(false);
    }

    private bool CheckAimable(bool isLeft)
    {
        FunctionalWeapon backWep = weaponManager.GetWeapon(isLeft, 1);
        if (backWep != null && backWep.GetWeaponData() is WeaponPart weaponData)
        {
            return weaponData.isAimableBackWeapon;
        }
        return false;
    }

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

    private IEnumerator ToggleLeftSequence()
    {
        IsLeftTransitioning = true;
        canFireLeft = false;
        weaponManager.ForceRelease(true, ActiveLeftSlot);
        leftArmTargetState = !leftArmTargetState;
        OnWeaponSwapStarted?.Invoke(true, ActiveLeftSlot);

        if (!leftArmTargetState) {
            leftArmActive = false;
            yield return new WaitForSeconds(armTransitionTime);
            leftBackActive = true;
            yield return new WaitForSeconds(backWeaponTransitionTime);
        } else {
            leftBackActive = false;
            yield return new WaitForSeconds(backWeaponTransitionTime);
            leftArmActive = true;
            yield return new WaitForSeconds(armTransitionTime);
        }

        canFireLeft = true;
        IsLeftTransitioning = false;
        OnWeaponSwapCompleted?.Invoke(true, ActiveLeftSlot);
    }

    private IEnumerator ToggleRightSequence()
    {
        IsRightTransitioning = true;
        canFireRight = false;
        weaponManager.ForceRelease(false, ActiveRightSlot);
        rightArmTargetState = !rightArmTargetState;
        OnWeaponSwapStarted?.Invoke(false, ActiveRightSlot);

        if (!rightArmTargetState) {
            rightArmActive = false;
            yield return new WaitForSeconds(armTransitionTime);
            rightBackActive = true;
            yield return new WaitForSeconds(backWeaponTransitionTime);
        } else {
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