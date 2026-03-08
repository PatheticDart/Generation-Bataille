using UnityEngine;
using UnityEngine.UI;
using TMPro; // NEW: Required for the text displays

public class MechUI : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("Drag the Player/Mech object here to read its stats.")]
    public MechStats mechStats;
    [Tooltip("Drag the CharacterController of the mech here to read its speed.")]
    public CharacterController mechController;

    [Header("Energy Bar UI")]
    public Slider energySlider;
    public Image energyFillImage;
    public Color normalColor = Color.green;
    public Color depletedColor = Color.red;
    public float pulseSpeed = 6f;
    public float minAlpha = 0.2f;

    [Header("Telemetry UI (Speed & Altitude)")]
    [Tooltip("Drag your Speed TMPro Text object here.")]
    public TextMeshProUGUI speedText;
    [Tooltip("Drag your Altitude TMPro Text object here.")]
    public TextMeshProUGUI altitudeText;
    [Tooltip("What layers should the altimeter consider as the 'ground'?")]
    public LayerMask groundLayer;
    [Tooltip("If true, speed shows as KM/H. If false, shows as M/S.")]
    public bool displayAsKMH = true;

    void Start()
    {
        // Auto-grab components if they are on the same object
        if (mechStats == null) mechStats = GetComponent<MechStats>();
        if (mechController == null) mechController = GetComponent<CharacterController>();

        // Set the slider's max limit to match the mech's max energy right at the start
        if (energySlider != null && mechStats != null)
        {
            energySlider.maxValue = mechStats.maxEnergy;
        }
    }

    void Update()
    {
        UpdateEnergyUI();
        UpdateTelemetryUI();
    }

    private void UpdateEnergyUI()
    {
        if (mechStats == null || energySlider == null || energyFillImage == null) return;

        energySlider.value = mechStats.currentEnergy;

        if (mechStats.energyIsDepleted)
        {
            float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) / 2f;
            float currentAlpha = Mathf.Lerp(minAlpha, 1f, wave);

            Color pulseColor = depletedColor;
            pulseColor.a = currentAlpha;
            energyFillImage.color = pulseColor;
        }
        else
        {
            Color solidColor = normalColor;
            solidColor.a = 1f;
            energyFillImage.color = solidColor;
        }
    }

    private void UpdateTelemetryUI()
    {
        if (mechController == null) return;

        // --- 1. CALCULATE SPEED ---
        if (speedText != null)
        {
            // Get the true velocity magnitude from the physics engine
            float currentSpeed = mechController.velocity.magnitude;

            if (displayAsKMH)
            {
                // Convert Unity Units per Second (m/s) to Kilometers per Hour
                currentSpeed *= 3.6f;
                speedText.text = $"{currentSpeed:F0} KM/H";
            }
            else
            {
                speedText.text = $"{currentSpeed:F0} M/S";
            }
        }

        // --- 2. CALCULATE RADAR ALTITUDE ---
        if (altitudeText != null)
        {
            float altitude = 0f;

            // Fire a raycast straight down from the mech's center
            if (Physics.Raycast(mechController.transform.position, Vector3.down, out RaycastHit hit, 1000f, groundLayer))
            {
                // Distance to the ground
                altitude = hit.distance;
            }
            else
            {
                // Fallback: If no ground is found (falling in the void), just use absolute Y position
                altitude = mechController.transform.position.y;
            }

            altitudeText.text = $"ALT: {altitude:F0} M";
        }
    }
}