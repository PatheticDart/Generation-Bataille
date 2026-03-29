using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections; // NEW: Required for Coroutines

public class PaintUI : MonoBehaviour
{
    [Header("References")]
    public GarageLoader garageLoader;
    public FlexibleColorPicker fcp;
    public TextMeshProUGUI categoryTitleText;

    private int currentCategoryIndex = 0;
    private string[] categoryNames = { "PRIMARY", "SECONDARY", "TERTIARY", "ACCENT", "GLOW" };
    private PlayerPaint[] backupPaintJob = new PlayerPaint[5];
    private bool isSwitchingCategory = false;

    void OnEnable()
    {
        if (garageLoader == null || garageLoader.globalPaintJob == null) return;

        int countToBackup = Mathf.Min(5, garageLoader.globalPaintJob.Length);
        for (int i = 0; i < countToBackup; i++)
        {
            // Store a snapshot of the colors exactly as they are when the user opens the menu
            backupPaintJob[i] = garageLoader.globalPaintJob[i];
        }

        // --- THE FIX: Wait 1 frame before setting the FCP color to avoid its startup glitch ---
        StartCoroutine(DelayedSetup());
    }

    private IEnumerator DelayedSetup()
    {
        // Yielding null tells Unity to wait until the next frame
        yield return null; 

        // Now that the FCP is fully awake and done throwing its default red color around, we update it
        UpdateCategoryDisplay();

        // Attach the listener AFTER the delay so we don't accidentally save the default red to the mech
        if (fcp != null)
        {
            fcp.onColorChange.RemoveListener(OnColorDragged); // Safety clear
            fcp.onColorChange.AddListener(OnColorDragged);
        }
    }

    void OnDisable()
    {
        // Clean up our coroutines and listeners when the panel closes
        StopAllCoroutines();
        if (fcp != null) fcp.onColorChange.RemoveListener(OnColorDragged);
    }

    public void NextCategory()
    {
        currentCategoryIndex++;
        if (currentCategoryIndex >= categoryNames.Length) currentCategoryIndex = 0;
        UpdateCategoryDisplay();
    }

    public void PreviousCategory()
    {
        currentCategoryIndex--;
        if (currentCategoryIndex < 0) currentCategoryIndex = categoryNames.Length - 1;
        UpdateCategoryDisplay();
    }

    private void UpdateCategoryDisplay()
{
    // 1. Block the listener while we reset the UI values
    isSwitchingCategory = true;

    if (categoryTitleText != null) 
        categoryTitleText.text = categoryNames[currentCategoryIndex];

    if (garageLoader != null && garageLoader.globalPaintJob != null && currentCategoryIndex < garageLoader.globalPaintJob.Length)
    {
        Color colorToShow;

        // If index 4 is GLOW, grab the emission color
        if (currentCategoryIndex == 4) 
            colorToShow = garageLoader.globalPaintJob[currentCategoryIndex].emissionColor;
        else 
            colorToShow = garageLoader.globalPaintJob[currentCategoryIndex].albedoColor;

        colorToShow.a = 1f;

        if (fcp != null)
        {
            fcp.SetColor(colorToShow); // Some FCP versions prefer SetColor over .color
            fcp.color = colorToShow;
        }
    }

    // 2. Unblock the listener so the user can start dragging
    isSwitchingCategory = false;
}

private void OnColorDragged(Color newColor)
{
    // If we are currently in the middle of a category swap, ignore the event
    if (isSwitchingCategory) return;
    if (garageLoader == null || garageLoader.globalPaintJob == null) return;

    newColor.a = 1f; 

    if (currentCategoryIndex < garageLoader.globalPaintJob.Length)
    {
        // Update the DATA
        if (currentCategoryIndex == 4) 
            garageLoader.globalPaintJob[currentCategoryIndex].emissionColor = newColor;
        else 
            garageLoader.globalPaintJob[currentCategoryIndex].albedoColor = newColor;

        // Force the VISUAL update
        garageLoader.FastApplyPaintToMech();
    }
}

    public void SaveColors()
    {
        gameObject.SetActive(false);
    }

    public void CancelColors()
    {
        if (garageLoader == null || garageLoader.globalPaintJob == null) return;

        // Restore the original colors from our backup
        int countToRestore = Mathf.Min(5, garageLoader.globalPaintJob.Length);
        for (int i = 0; i < countToRestore; i++)
        {
            garageLoader.globalPaintJob[i] = backupPaintJob[i];
        }

        garageLoader.FastApplyPaintToMech();
        gameObject.SetActive(false);
    }
}