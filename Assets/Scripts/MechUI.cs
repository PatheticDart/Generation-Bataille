using UnityEngine;
using UnityEngine.UI; // Required for Slider and Image components

public class MechUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the Player/Mech object here to read its stats.")]
    public MechStats mechStats;
    [Tooltip("Drag your Energy Slider UI object here.")]
    public Slider energySlider;
    [Tooltip("Drag the 'Fill' Image component of the slider here so we can change its color.")]
    public Image energyFillImage;

    [Header("Colors & Effects")]
    public Color normalColor = Color.green;
    public Color depletedColor = Color.red;
    [Tooltip("How fast the red bar pulses when depleted.")]
    public float pulseSpeed = 6f;
    [Tooltip("The lowest transparency the bar will hit while pulsing (0 is invisible, 1 is solid).")]
    public float minAlpha = 0.2f;

    void Start()
    {
        // Set the slider's max limit to match the mech's max energy right at the start
        if (energySlider != null && mechStats != null)
        {
            energySlider.maxValue = mechStats.maxEnergy;
        }
    }

    void Update()
    {
        if (mechStats == null || energySlider == null || energyFillImage == null) return;

        // 1. Update the actual bar length
        energySlider.value = mechStats.currentEnergy;

        // 2. Handle Color and Pulsing Effects
        if (mechStats.energyIsDepleted)
        {
            // Use a Sine wave based on time to smoothly bounce between 0 and 1
            float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;

            // Lerp the alpha transparency between our minimum limit and fully solid
            float currentAlpha = Mathf.Lerp(minAlpha, 1f, wave);

            // Apply the red color with the new fading alpha
            Color pulseColor = depletedColor;
            pulseColor.a = currentAlpha;
            energyFillImage.color = pulseColor;
        }
        else
        {
            // Lock it to solid normal color when fully operational
            Color solidColor = normalColor;
            solidColor.a = 1f;
            energyFillImage.color = solidColor;
        }
    }
}