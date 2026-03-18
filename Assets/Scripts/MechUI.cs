using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections.Generic;

public class MechUI : MonoBehaviour
{
    [Header("Component References")]
    public MechStats mechStats;
    public CharacterController mechController;
    public MechWeaponManager weaponManager;

    [Header("Energy Bar UI")]
    public Slider energySlider;
    public Image energyFillImage;
    public Color normalColor = Color.green;
    public Color depletedColor = Color.red;
    public float pulseSpeed = 6f;
    public float minAlpha = 0.2f;

    [Header("Telemetry UI (Speed & Altitude)")]
    public TextMeshProUGUI speedText;
    public List<TextMeshProUGUI> altitudeTexts = new List<TextMeshProUGUI>(); 
    public LayerMask groundLayer;
    public bool displayAsKMH = true;

    [Header("Static Canvas Weapon UI")]
    [Tooltip("Drag the Ammo Texts from your NORMAL Canvas here (NOT the Reticle ones).")]
    public List<TextMeshProUGUI> leftArmAmmoTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> rightArmAmmoTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> leftBackAmmoTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> rightBackAmmoTexts = new List<TextMeshProUGUI>();
    
    [Tooltip("Opacity level (0 to 1) for the weapon currently selected.")]
    public float activeWeaponAlpha = 1.0f;
    [Tooltip("Opacity level (0 to 1) for the weapon currently stowed/deactivated.")]
    public float inactiveWeaponAlpha = 0.5f;

    void Start()
    {
        if (mechStats == null) mechStats = GetComponent<MechStats>();
        if (mechController == null) mechController = GetComponent<CharacterController>();
        if (weaponManager == null) weaponManager = GetComponent<MechWeaponManager>();

        if (energySlider != null && mechStats != null)
        {
            energySlider.maxValue = mechStats.maxEnergy;
        }
    }

    void Update()
    {
        UpdateEnergyUI();
        UpdateTelemetryUI();
        UpdateStaticWeaponUI(); 
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

        // Speed
        if (speedText != null)
        {
            float currentSpeed = mechController.velocity.magnitude;
            if (displayAsKMH)
            {
                currentSpeed *= 3.6f;
                speedText.text = $"{currentSpeed:F0} KM/H";
            }
            else
            {
                speedText.text = $"{currentSpeed:F0} M/S";
            }
        }

        // Altitude
        if (altitudeTexts != null && altitudeTexts.Count > 0)
        {
            float altitude = 0f;
            if (Physics.Raycast(mechController.transform.position, Vector3.down, out RaycastHit hit, 1000f, groundLayer))
            {
                altitude = hit.distance;
            }
            else
            {
                altitude = mechController.transform.position.y;
            }

            string altString = $"{altitude:F0} M";
            foreach (TextMeshProUGUI altText in altitudeTexts)
            {
                if (altText != null) altText.text = altString;
            }
        }
    }

    // --- STATIC WEAPON UI LOGIC ---
    private void UpdateStaticWeaponUI()
    {
        if (weaponManager == null) return;

        SetTextAlpha(leftArmAmmoTexts, weaponManager.leftArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);
        SetTextAlpha(leftBackAmmoTexts, !weaponManager.leftArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);

        SetTextAlpha(rightArmAmmoTexts, weaponManager.rightArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);
        SetTextAlpha(rightBackAmmoTexts, !weaponManager.rightArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);
    }

    private void SetTextAlpha(List<TextMeshProUGUI> textElements, float targetAlpha)
    {
        if (textElements == null || textElements.Count == 0) return;

        foreach (TextMeshProUGUI textElement in textElements)
        {
            if (textElement != null)
            {
                Color c = textElement.color;
                c.a = targetAlpha;
                textElement.color = c;
            }
        }
    }
}