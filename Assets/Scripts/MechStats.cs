using UnityEngine;

public class MechStats : MonoBehaviour
{
    [Header("Armor & Energy")]
    public float maxEnergy = 1000f;
    public float currentEnergy;
    public float energyRegenRate = 150f;
    public float depletedRegenRate = 50f;
    public bool energyIsDepleted = false;

    [Header("Movement Stats")]
    public float turnSpeed = 5f;
    public float walkSpeed = 15f;
    public float boostHorizontalSpeed = 35f;
    public float boostVerticalSpeed = 25f;
    public float boostEnergyDrain = 200f;
    public float jumpForce = 12f;
    [Tooltip("How long to wait after pressing jump before the mech actually leaves the ground.")]
    public float jumpDelay = 0.25f;
    public float totalWeight = 30000f;
    public float baselineWeight = 30000f;

    [Header("Quick Boost (QB) Stats")]
    public float qbThrust = 80f;
    public float qbEnergyDrain = 150f;
    public float qbDuration = 0.35f;
    public float qbReloadTime = 0.8f;

    [Header("Kinematics (Momentum)")]
    public float walkAcceleration = 25f;
    public float walkDeceleration = 30f;
    public float boostAcceleration = 10f;
    public float boostDeceleration = 15f;

    // --- MOVEMENT REFINEMENTS ---
    [Tooltip("Penalty applied to walk speed when moving backwards (0.3 = 30% slower).")]
    [Range(0f, 1f)]
    public float backwardSpeedPenalty = 0.3f;

    [Header("Braking (Boost Stop)")]
    [Tooltip("Time before the mech hard-brakes after letting go of boost on the ground.")]
    public float brakeBufferTime = 0.3f;
    [Tooltip("Friction multiplier during a brake slide.")]
    public float brakeSlideDeceleration = 5f;
    public float baseBrakeTime = 0.4f;
    public float maxBrakeTime = 1.0f;

    [Header("Landing & Impact")]
    [Tooltip("Friction multiplier during a hard landing slide. E.g., 2f for a long slide, 10f for a short stop.")]
    public float hardLandingSlideDeceleration = 5f;
    public float minHardLandingThreshold = -25f;
    public float maxHardLandingThreshold = -80f;
    public float baseHardLandingTime = 0.5f;
    public float maxHardLandingTime = 2.5f;

    [Header("Air Kinematics")]
    public float airAcceleration = 5f;
    public float airDeceleration = 1.5f;

    private void Start()
    {
        currentEnergy = maxEnergy;
    }

    private void Update()
    {
        if (currentEnergy <= 0.01f)
        {
            currentEnergy = 0;
            energyIsDepleted = true;
        }

        if (energyIsDepleted && currentEnergy >= maxEnergy)
        {
            energyIsDepleted = false;
        }

        float regen = energyIsDepleted ? depletedRegenRate : energyRegenRate;
        if (currentEnergy < maxEnergy)
        {
            currentEnergy += regen * Time.deltaTime;
        }

        currentEnergy = Mathf.Clamp(currentEnergy, 0, maxEnergy);
    }

    public bool ConsumeEnergy(float amount)
    {
        if (energyIsDepleted || currentEnergy <= 0) return false;

        currentEnergy -= amount;
        return true;
    }
}