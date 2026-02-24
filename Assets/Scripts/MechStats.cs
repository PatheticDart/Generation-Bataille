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
    public float totalWeight = 5000f;

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