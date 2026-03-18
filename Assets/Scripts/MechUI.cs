using UnityEngine;
using UnityEngine.UI;
using TMPro; 
using System.Collections.Generic;

public class MechUI : MonoBehaviour
{
    [Header("Component References")]
    public MechStats mechStats;
    public CharacterController mechController;
    public MechWeaponManager mechWeaponManager; 
    public WeaponManager weaponManager;         

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

    [Header("Standard Ammo UI (Slot: XX / XX)")]
    public List<TextMeshProUGUI> leftArmAmmoTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> rightArmAmmoTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> leftBackAmmoTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> rightBackAmmoTexts = new List<TextMeshProUGUI>();

    [Header("Compact Ammo UI (XX Only)")]
    public List<TextMeshProUGUI> leftArmCompactTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> rightArmCompactTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> leftBackCompactTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> rightBackCompactTexts = new List<TextMeshProUGUI>();
    
    [Tooltip("Opacity level (0 to 1) for the weapon currently selected.")]
    public float activeWeaponAlpha = 1.0f;
    [Tooltip("Opacity level (0 to 1) for the weapon currently stowed/deactivated.")]
    public float inactiveWeaponAlpha = 0.5f;

    void Start()
    {
        if (mechStats == null) mechStats = GetComponent<MechStats>();
        if (mechController == null) mechController = GetComponent<CharacterController>();
        if (mechWeaponManager == null) mechWeaponManager = GetComponent<MechWeaponManager>();
        if (weaponManager == null) weaponManager = GetComponent<WeaponManager>();

        if (energySlider != null && mechStats != null)
        {
            energySlider.maxValue = mechStats.maxEnergy;
        }

        Invoke(nameof(InitializeWeaponSubscriptions), 0.1f);
    }

    private void InitializeWeaponSubscriptions()
    {
        if (weaponManager == null) return;

        // Pass the specific prefix for each slot
        SetupSlot(true, 0, "LA", leftArmAmmoTexts, leftArmCompactTexts);
        SetupSlot(true, 1, "LB", leftBackAmmoTexts, leftBackCompactTexts);
        SetupSlot(false, 0, "RA", rightArmAmmoTexts, rightArmCompactTexts);
        SetupSlot(false, 1, "RB", rightBackAmmoTexts, rightBackCompactTexts);
    }

    private void SetupSlot(bool isLeft, int slot, string prefix, List<TextMeshProUGUI> std, List<TextMeshProUGUI> compact)
    {
        FunctionalWeapon weapon = weaponManager.GetWeapon(isLeft, slot);
        if (weapon == null) return;

        // Initial update with the slot prefix
        UpdateAmmoDisplay(prefix, std, compact, weapon.currentResource, weapon.maxResource);

        // Subscribe to resource changes
        weapon.OnResourceChanged += (curr, max) => UpdateAmmoDisplay(prefix, std, compact, curr, max);
    }

    private void UpdateAmmoDisplay(string prefix, List<TextMeshProUGUI> stdList, List<TextMeshProUGUI> compactList, float current, float max)
    {
        // Update Standard texts: "RA: 15/30"
        string standardFormat = $"{prefix}: {current:F0}/{max:F0}";
        foreach (var txt in stdList)
            if (txt != null) txt.text = standardFormat;

        // Update Compact texts: "15"
        string compactFormat = $"{current:F0}";
        foreach (var txt in compactList)
            if (txt != null) txt.text = compactFormat;
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

    private void UpdateStaticWeaponUI()
    {
        if (mechWeaponManager == null) return;

        // Update Standard List Alpha
        SetListAlpha(leftArmAmmoTexts, mechWeaponManager.leftArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);
        SetListAlpha(leftBackAmmoTexts, !mechWeaponManager.leftArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);
        SetListAlpha(rightArmAmmoTexts, mechWeaponManager.rightArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);
        SetListAlpha(rightBackAmmoTexts, !mechWeaponManager.rightArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);

        // Update Compact List Alpha
        SetListAlpha(leftArmCompactTexts, mechWeaponManager.leftArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);
        SetListAlpha(leftBackCompactTexts, !mechWeaponManager.leftArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);
        SetListAlpha(rightArmCompactTexts, mechWeaponManager.rightArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);
        SetListAlpha(rightBackCompactTexts, !mechWeaponManager.rightArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);
    }

    private void SetListAlpha(List<TextMeshProUGUI> textElements, float targetAlpha)
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