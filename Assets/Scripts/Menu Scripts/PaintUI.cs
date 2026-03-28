using UnityEngine;
using TMPro;
using UnityEngine.UI;

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

        if (fcp != null) fcp.onColorChange.AddListener(OnColorDragged);
        UpdateCategoryDisplay();
    }

    void OnDisable()
    {
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
        isSwitchingCategory = true;

        if (categoryTitleText != null) categoryTitleText.text = categoryNames[currentCategoryIndex];

        if (garageLoader != null && garageLoader.globalPaintJob != null && currentCategoryIndex < garageLoader.globalPaintJob.Length)
        {
            Color colorToShow = Color.white;

            if (currentCategoryIndex == 4) colorToShow = garageLoader.globalPaintJob[currentCategoryIndex].emissionColor;
            else colorToShow = garageLoader.globalPaintJob[currentCategoryIndex].albedoColor;

            // Force Alpha to 1 so the FCP UI doesn't show a broken checkerboard
            colorToShow.a = 1f;

            if (fcp != null) fcp.color = colorToShow;
        }

        isSwitchingCategory = false;
    }

    private void OnColorDragged(Color newColor)
    {
        if (isSwitchingCategory || garageLoader == null || garageLoader.globalPaintJob == null) return;

        newColor.a = 1f; // Ensure colors drawn onto the mech are solid

        if (currentCategoryIndex < garageLoader.globalPaintJob.Length)
        {
            if (currentCategoryIndex == 4) garageLoader.globalPaintJob[currentCategoryIndex].emissionColor = newColor;
            else garageLoader.globalPaintJob[currentCategoryIndex].albedoColor = newColor;

            garageLoader.FastApplyPaintToMech();
        }
    }

    public void SaveColors()
    {
        // Colors are already saved in the global array; the MechCardManager can capture them directly.
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