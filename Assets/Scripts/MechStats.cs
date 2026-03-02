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
    public float totalWeight = 30000f;

    [Tooltip("The 'standard' weight of a medium mech at your current scale. Used as the 1.0x math baseline.")]
    public float baselineWeight = 30000f;

    [Header("Kinematics (Momentum)")]
    public float walkAcceleration = 25f;
    public float walkDeceleration = 30f;
    public float boostAcceleration = 10f; // Lower = heavier drifting when turning
    public float boostDeceleration = 15f;

    [Header("Air Kinematics")]
    public float airAcceleration = 5f;    // Hard to change direction mid-air
    public float airDeceleration = 1.5f;  // Takes a LONG time to stop moving mid-air

    [Header("Landing & Impact")]
    [Tooltip("How fast you must be falling to trigger a hard landing (Negative value).")]
    public float hardLandingThreshold = -25f;
    [Tooltip("The minimum stagger time when hitting the exact threshold at standard weight.")]
    public float baseHardLandingTime = 0.5f;
    [Tooltip("The absolute maximum time movement can be locked, regardless of weight/speed.")]
    public float maxHardLandingTime = 2.5f;
    private void Start()
    {
        currentEnergy = maxEnergy;
    }

    private void Update()
    {
        // If we are at or below 0, lock the mech into Depleted state
        if (currentEnergy <= 0.01f)
        {
            currentEnergy = 0;
            energyIsDepleted = true;
        }

        // Unlock Depleted state only at 100% charge
        if (energyIsDepleted && currentEnergy >= maxEnergy)
        {
            energyIsDepleted = false;
        }

        // Apply Regen
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