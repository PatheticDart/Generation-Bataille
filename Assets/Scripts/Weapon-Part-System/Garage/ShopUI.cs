using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum ShopCategory
{
    Head, Torso, Arms, Legs, Booster, Generator, FCS, Weapons
}

public class ShopUI : MonoBehaviour
{
    [Header("References")]
    public MechShopManager shopManager;
    public GarageLoader garageLoader;
    public GarageCameraManager cameraManager;
    
    [Header("UI Text Fields")]
    public TextMeshProUGUI categoryTitleText;
    public TextMeshProUGUI playerCreditsText;
    
    [Header("Purchase Panel")]
    public Button masterBuyButton;
    public TextMeshProUGUI buyButtonText;

    [Header("Button Spawning")]
    public GameObject shopButtonPrefab; 
    public Transform buttonContainer;

    private ShopCategory[] categoryOrder = new ShopCategory[]
    {
        ShopCategory.Head, ShopCategory.Torso, ShopCategory.Arms, ShopCategory.Legs,
        ShopCategory.Booster, ShopCategory.Generator, ShopCategory.FCS, ShopCategory.Weapons
    };

    private int currentCategoryIndex = 0;

    // --- PREVIEW STATE TRACKING ---
    private Part _previewedPart = null;
    private PartType _previewSlot;
    private Part _originalPartInSlot = null;

    void Start()
    {
        // Hook up the master buy button
        if (masterBuyButton != null)
        {
            masterBuyButton.onClick.AddListener(ExecutePurchase);
        }
        
        ClearPreviewState();
        UpdateCategoryDisplay();
    }

    // Call this if the player closes the shop menu!
    public void OnCloseShop()
    {
        RestoreOriginalLoadout();
        ClearPreviewState();
    }

    public void NextCategory()
    {
        RestoreOriginalLoadout(); // Put original part back before switching tabs
        currentCategoryIndex++;
        if (currentCategoryIndex >= categoryOrder.Length) currentCategoryIndex = 0;
        UpdateCategoryDisplay();
    }

    public void PreviousCategory()
    {
        RestoreOriginalLoadout();
        currentCategoryIndex--;
        if (currentCategoryIndex < 0) currentCategoryIndex = categoryOrder.Length - 1;
        UpdateCategoryDisplay();
    }

    private void UpdateCategoryDisplay()
    {
        ShopCategory currentType = categoryOrder[currentCategoryIndex];
        categoryTitleText.text = currentType.ToString().ToUpper();
        
        ClearPreviewState(); // Reset the buy button

        if (PlayerInventoryManager.Instance != null)
        {
            playerCreditsText.text = $"CREDITS: {PlayerInventoryManager.Instance.currentCredits:N0}";
        }

        // --- CAMERA PANNING ---
        if (cameraManager != null)
        {
            // Map the shop category to the closest PartType for the camera to look at
            PartType cameraTarget = MapCategoryToPartType(currentType);
            cameraManager.SwitchCameraForPartCategory(cameraTarget);
        }

        foreach (Transform child in buttonContainer) Destroy(child.gameObject);

        List<Part> unpurchasedParts = GetUnpurchasedPartsForCategory(currentType);
        if (unpurchasedParts == null || unpurchasedParts.Count == 0) return;

        foreach (Part partData in unpurchasedParts)
        {
            GameObject newBtnObj = Instantiate(shopButtonPrefab, buttonContainer);
            TextMeshProUGUI btnText = newBtnObj.GetComponentInChildren<TextMeshProUGUI>();
            
            if (btnText != null) 
            {
                string displayName = string.IsNullOrEmpty(partData.partName) ? partData.name : partData.partName;
                btnText.text = $"{displayName}";
            }

            Button btn = newBtnObj.GetComponent<Button>();
            if (btn != null)
            {
                Part partToSelect = partData;
                btn.onClick.AddListener(() =>
                {
                    SelectAndPreviewPart(currentType, partToSelect);
                });
            }
        }
    }

    // --- PREVIEW & PURCHASE LOGIC ---

    private void SelectAndPreviewPart(ShopCategory category, Part part)
    {
        RestoreOriginalLoadout(); // Revert the last thing they previewed

        _previewedPart = part;
        _previewSlot = MapCategoryToPartType(category, part);

        // Save what they currently have equipped in this slot
        GarageLoader.ActiveLoadout.TryGetValue(_previewSlot, out _originalPartInSlot);

        // Force GarageLoader to wear the preview item
        if (garageLoader != null)
        {
            garageLoader.EquipPart(_previewSlot, part);
            
            // Refocus camera to account for size changes (like a tall head or long gun)
            if (cameraManager != null) cameraManager.SwitchCameraForPartCategory(_previewSlot);
        }

        // Update the Master Buy Button
        if (masterBuyButton != null && buyButtonText != null)
        {
            masterBuyButton.interactable = PlayerInventoryManager.Instance.HasEnoughCredits(part.price);
            buyButtonText.text = $"Purchase\n({part.price:N0} C)";
        }
    }

    private void ExecutePurchase()
    {
        if (_previewedPart == null || shopManager == null) return;

        shopManager.AttemptPurchase(_previewedPart);

        // If successful, the preview part becomes their new permanent original part!
        if (PlayerInventoryManager.Instance.IsPartOwned(_previewedPart))
        {
            _originalPartInSlot = _previewedPart; 
            UpdateCategoryDisplay(); // Refresh list so it disappears
        }
    }

    private void RestoreOriginalLoadout()
    {
        if (_previewedPart != null && garageLoader != null)
        {
            if (_originalPartInSlot != null)
                garageLoader.EquipPart(_previewSlot, _originalPartInSlot);
            else
                GarageLoader.ActiveLoadout.Remove(_previewSlot); // If slot was originally empty
        }
    }

    private void ClearPreviewState()
    {
        _previewedPart = null;
        _originalPartInSlot = null;
        
        if (masterBuyButton != null && buyButtonText != null)
        {
            masterBuyButton.interactable = false;
            buyButtonText.text = "SELECT AN ITEM";
        }
    }

    // --- HELPER ROUTING ---

    private PartType MapCategoryToPartType(ShopCategory category, Part specificPart = null)
    {
        switch (category)
        {
            case ShopCategory.Head: return PartType.Head;
            case ShopCategory.Torso: return PartType.Torso;
            case ShopCategory.Arms: return PartType.Arms;
            case ShopCategory.Legs: return PartType.Legs;
            case ShopCategory.Booster: return PartType.Booster;
            case ShopCategory.Generator: return PartType.Generator;
            case ShopCategory.FCS: return PartType.FCS;
            case ShopCategory.Weapons:
                // For weapons, check where it is allowed to mount so we preview it in a valid spot
                if (specificPart is WeaponPart weapon)
                {
                    if (weapon.allowedLocations.HasFlag(WeaponLocation.ArmR)) return PartType.ArmR;
                    if (weapon.allowedLocations.HasFlag(WeaponLocation.ArmL)) return PartType.ArmL;
                    if (weapon.allowedLocations.HasFlag(WeaponLocation.BackR)) return PartType.BackR;
                    if (weapon.allowedLocations.HasFlag(WeaponLocation.BackL)) return PartType.BackL;
                }
                return PartType.ArmR; // Fallback for the camera
            default: return PartType.Torso;
        }
    }

    private List<Part> GetUnpurchasedPartsForCategory(ShopCategory category)
    {
        if (shopManager == null || PlayerInventoryManager.Instance == null) return new List<Part>();

        List<Part> allPartsInCategory = new List<Part>();

        switch (category)
        {
            case ShopCategory.Head: allPartsInCategory.AddRange(shopManager.shopHeads); break;
            case ShopCategory.Torso: allPartsInCategory.AddRange(shopManager.shopTorsos); break;
            case ShopCategory.Arms: allPartsInCategory.AddRange(shopManager.shopArms); break;
            case ShopCategory.Legs: allPartsInCategory.AddRange(shopManager.shopLegs); break;
            case ShopCategory.Booster: allPartsInCategory.AddRange(shopManager.shopBoosters); break;
            case ShopCategory.Generator: allPartsInCategory.AddRange(shopManager.shopGenerators); break;
            case ShopCategory.FCS: allPartsInCategory.AddRange(shopManager.shopFCS); break;
            case ShopCategory.Weapons: allPartsInCategory.AddRange(shopManager.shopWeapons); break;
        }

        List<Part> unpurchasedOnly = new List<Part>();
        foreach (Part p in allPartsInCategory)
        {
            if (!PlayerInventoryManager.Instance.IsPartOwned(p))
            {
                unpurchasedOnly.Add(p);
            }
        }
        return unpurchasedOnly;
    }



    // public void purchaseItem(Part part)
    // {
    //     if (shopManager != null)
    //     {
    //         shopManager.AttemptPurchase(part);
    //         UpdateCategoryDisplay(); // Refresh the UI after purchase
    //     }
    // }
}