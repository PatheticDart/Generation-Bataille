using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UICustomization : MonoBehaviour
{
    [Header("UI Managers")]
    public MechUI mechUI;
    public FCSLockBox fcsLockBox;

    [Header("General UI Elements (Texts, Borders, etc.)")]
    [Tooltip("Drag all images and TMPro texts you want to color-match here.")]
    public Graphic[] generalUIElements;

    [Header("Togglable UI Groups")]
    public GameObject speedometerObject;
    public GameObject altimeterObject;
    public GameObject reticleAmmoCountersParent;

    [Header("Current Color Settings")]
    public Color currentGeneralColor = new Color(1f, 0.8f, 0f); // Default Orange/Yellow
    public Color currentEnergyColor = Color.green;
    public Color currentReticleColor = Color.green;

    // Call this to update all colors at once (e.g., from a settings menu later)
    public void ApplyCustomColors()
    {
        // 1. Update Energy Bar
        if (mechUI != null)
        {
            mechUI.normalColor = currentEnergyColor;
        }

        // 2. Update Reticle Base Color
        if (fcsLockBox != null)
        {
            fcsLockBox.softLockColor = currentReticleColor;
        }

        // 3. Update all generic UI elements (LockBox borders, Rangefinder, Flavor Text)
        foreach (Graphic element in generalUIElements)
        {
            if (element != null)
            {
                element.color = currentGeneralColor;
            }
        }
    }

    // --- TOGGLE FUNCTIONS --- 
    // These can be hooked up directly to Unity UI Toggles in your Options menu later

    public void ToggleSpeedometer(bool isVisible)
    {
        if (speedometerObject != null)
        {
            speedometerObject.SetActive(isVisible);
        }
    }

    public void ToggleAltimeter(bool isVisible)
    {
        if (altimeterObject != null)
        {
            altimeterObject.SetActive(isVisible);
        }
    }

    public void ToggleReticleAmmo(bool isVisible)
    {
        if (reticleAmmoCountersParent != null)
        {
            reticleAmmoCountersParent.SetActive(isVisible);
        }
    }

    // Optional test: Apply colors on start
    void Start()
    {
        ApplyCustomColors();
    }
}