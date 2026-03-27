using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AssemblyUI : MonoBehaviour
{
    [Header("References")]
    public GarageLoader garageLoader;
    public GarageCameraManager cameraManager;
    public TextMeshProUGUI categoryTitleText;

    [Header("Button Spawning")]
    public GameObject partButtonPrefab;
    public Transform buttonContainer;

    private PartType[] categoryOrder = new PartType[]
    {
        PartType.Head, PartType.Torso, PartType.Arms, PartType.Legs,
        PartType.Booster, PartType.Generator, PartType.FCS,
        PartType.ArmL, PartType.ArmR, PartType.BackL, PartType.BackR
    };

    private int currentCategoryIndex = 0;

    void Start()
    {
        UpdateCategoryDisplay();
    }

    public void NextCategory()
    {
        currentCategoryIndex++;
        if (currentCategoryIndex >= categoryOrder.Length) currentCategoryIndex = 0;
        UpdateCategoryDisplay();
    }

    public void PreviousCategory()
    {
        currentCategoryIndex--;
        if (currentCategoryIndex < 0) currentCategoryIndex = categoryOrder.Length - 1;
        UpdateCategoryDisplay();
    }

    private void UpdateCategoryDisplay()
    {
        PartType currentType = categoryOrder[currentCategoryIndex];

        categoryTitleText.text = FormatCategoryName(currentType);

        if (cameraManager != null)
        {
            cameraManager.SwitchCameraForPartCategory(currentType);
        }

        foreach (Transform child in buttonContainer) Destroy(child.gameObject);

        List<Part> currentPartsList = GetPartsListForCategory(currentType);
        if (currentPartsList == null || currentPartsList.Count == 0) return;

        foreach (Part partData in currentPartsList)
        {
            GameObject newBtnObj = Instantiate(partButtonPrefab, buttonContainer);

            TextMeshProUGUI btnText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = string.IsNullOrEmpty(partData.partName) ? partData.name : partData.partName;

            Button btn = newBtnObj.GetComponent<Button>();
            if (btn != null)
            {
                Part partToEquip = partData;
                btn.onClick.AddListener(() =>
                {
                    garageLoader.EquipPart(currentType, partToEquip);

                    // --- THE FIX: Tell the camera to recalculate the height of the newly spawned part! ---
                    if (cameraManager != null) cameraManager.SwitchCameraForPartCategory(currentType);
                });
            }
        }
    }

    private List<Part> GetPartsListForCategory(PartType type)
    {
        if (garageLoader == null) return null;

        switch (type)
        {
            case PartType.Head: return new List<Part>(garageLoader.availableHeads);
            case PartType.Torso: return new List<Part>(garageLoader.availableTorsos);
            case PartType.Arms: return new List<Part>(garageLoader.availableArms);
            case PartType.Legs: return new List<Part>(garageLoader.availableLegs);
            case PartType.Booster: return new List<Part>(garageLoader.availableBoosters);
            case PartType.Generator: return new List<Part>(garageLoader.availableGenerators);
            case PartType.FCS: return new List<Part>(garageLoader.availableFCS);
            case PartType.ArmL: return new List<Part>(garageLoader.availableLeftArmWeapons);
            case PartType.ArmR: return new List<Part>(garageLoader.availableRightArmWeapons);
            case PartType.BackL: return new List<Part>(garageLoader.availableLeftBackWeapons);
            case PartType.BackR: return new List<Part>(garageLoader.availableRightBackWeapons);
            default: return new List<Part>();
        }
    }

    private string FormatCategoryName(PartType type)
    {
        switch (type)
        {
            case PartType.ArmL: return "LEFT ARM";
            case PartType.ArmR: return "RIGHT ARM";
            case PartType.BackL: return "LEFT BACK";
            case PartType.BackR: return "RIGHT BACK";
            default: return type.ToString().ToUpper();
        }
    }
}