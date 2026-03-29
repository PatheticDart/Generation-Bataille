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

    [Header("Action Panel")]
    public Button masterBuyButton;
    public TextMeshProUGUI buyButtonText;

    [Header("Mode Toggles")]
    public Button buyModeButton;
    public Button sellModeButton;
    public Color activeModeColor = Color.white;
    public Color inactiveModeColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Button Spawning")]
    public GameObject shopButtonPrefab;
    public Transform buttonContainer;

    private ShopCategory[] categoryOrder = new ShopCategory[]
    {
        ShopCategory.Head, ShopCategory.Torso, ShopCategory.Arms, ShopCategory.Legs,
        ShopCategory.Booster, ShopCategory.Generator, ShopCategory.FCS, ShopCategory.Weapons
    };

    private int currentCategoryIndex = 0;

    private Part _previewedPart = null;
    private PartType _previewSlot;
    private Part _originalPartInSlot = null;
    public bool isSellMode = false;

    private bool _isShuttingDown = false;

    void Start()
    {
        if (masterBuyButton != null) masterBuyButton.onClick.AddListener(ExecuteAction);

        if (buyModeButton != null) buyModeButton.onClick.AddListener(() => SetShopMode(false));
        if (sellModeButton != null) sellModeButton.onClick.AddListener(() => SetShopMode(true));
    }

    // --- NEW: Block Missing Reference Exceptions during scene unloads ---
    private void OnApplicationQuit() { _isShuttingDown = true; }
    private void OnDestroy() { _isShuttingDown = true; }

    private void OnEnable()
    {
        // 1. AUTO-REASSIGN REFERENCES
        // If we lost our managers due to a scene reload, find them again instantly.
        if (shopManager == null) shopManager = Object.FindFirstObjectByType<MechShopManager>();
        if (garageLoader == null) garageLoader = Object.FindFirstObjectByType<GarageLoader>();
        if (cameraManager == null) cameraManager = Object.FindFirstObjectByType<GarageCameraManager>();

        if (shopManager != null && PlayerInventoryManager.Instance != null)
        {
            UpdateCategoryDisplay();
        }
    }

    private void OnDisable()
    {
        // If the game is closing, or the scene is actively unloading, DO NOT touch the mech!
        if (_isShuttingDown || !gameObject.scene.isLoaded) return;
        if (garageLoader == null) return;

        try
        {
            RestoreOriginalLoadout();
            ClearPreviewState();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("ShopUI suppressed a preview restore error during transition: " + e.Message);
        }
    }

    public void OpenBuyMenu()
    {
        SetShopMode(false);
    }

    public void OpenSellMenu()
    {
        SetShopMode(true);
    }

    public void SetShopMode(bool toSellMode)
    {
        isSellMode = toSellMode;

        if (buyModeButton != null) buyModeButton.image.color = !isSellMode ? activeModeColor : inactiveModeColor;
        if (sellModeButton != null) sellModeButton.image.color = isSellMode ? activeModeColor : inactiveModeColor;

        RestoreOriginalLoadout();
        ClearPreviewState();
        UpdateCategoryDisplay();
    }

    public void NextCategory()
    {
        RestoreOriginalLoadout();
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
        if (categoryTitleText != null) categoryTitleText.text = currentType.ToString().ToUpper();

        ClearPreviewState();

        if (PlayerInventoryManager.Instance != null && playerCreditsText != null)
        {
            playerCreditsText.text = $"CREDITS: {PlayerInventoryManager.Instance.currentCredits:N0}";
        }

        if (cameraManager != null)
        {
            cameraManager.SwitchCameraForPartCategory(MapCategoryToPartType(currentType));
        }

        foreach (Transform child in buttonContainer) Destroy(child.gameObject);

        List<Part> partsToShow = GetPartsToDisplay(currentType);
        if (partsToShow == null || partsToShow.Count == 0) return;

        foreach (Part partData in partsToShow)
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
                btn.onClick.AddListener(() => SelectAndPreviewPart(currentType, partToSelect));
            }
        }
    }

    private void SelectAndPreviewPart(ShopCategory category, Part part)
    {
        RestoreOriginalLoadout();

        _previewedPart = part;
        _previewSlot = MapCategoryToPartType(category, part);

        GarageLoader.ActiveLoadout.TryGetValue(_previewSlot, out _originalPartInSlot);

        if (garageLoader != null)
        {
            garageLoader.EquipPart(_previewSlot, part);
            if (cameraManager != null) cameraManager.SwitchCameraForPartCategory(_previewSlot);
        }

        UpdateActionButtonState();
    }

    private void UpdateActionButtonState()
    {
        if (masterBuyButton == null || buyButtonText == null) return;

        if (_previewedPart == null)
        {
            masterBuyButton.interactable = false;
            buyButtonText.text = isSellMode ? "SELECT TO SELL" : "SELECT AN ITEM";
            return;
        }

        if (isSellMode)
        {
            masterBuyButton.interactable = true;
            buyButtonText.text = $"SELL (+{_previewedPart.price:N0} C)";
        }
        else
        {
            bool canAfford = PlayerInventoryManager.Instance.HasEnoughCredits(_previewedPart.price);
            masterBuyButton.interactable = canAfford;
            buyButtonText.text = canAfford ? $"BUY ({_previewedPart.price:N0} C)" : $"NOT ENOUGH ({_previewedPart.price:N0} C)";
        }
    }

    private void ExecuteAction()
    {
        if (_previewedPart == null || shopManager == null) return;

        if (isSellMode)
        {
            shopManager.AttemptSell(_previewedPart);
            _originalPartInSlot = null;
        }
        else
        {
            shopManager.AttemptPurchase(_previewedPart);
            if (PlayerInventoryManager.Instance.IsPartOwned(_previewedPart))
            {
                _originalPartInSlot = _previewedPart;
            }
        }

        PlayerInventoryManager.Instance.SaveInventory();
        UpdateCategoryDisplay();
    }

    private void RestoreOriginalLoadout()
    {
        if (_previewedPart != null && garageLoader != null)
        {
            if (_originalPartInSlot != null)
                garageLoader.EquipPart(_previewSlot, _originalPartInSlot);
            else
                GarageLoader.ActiveLoadout.Remove(_previewSlot);
        }
    }

    private void ClearPreviewState()
    {
        _previewedPart = null;
        _originalPartInSlot = null;
        UpdateActionButtonState();
    }

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
                if (specificPart is WeaponPart weapon)
                {
                    if (weapon.allowedLocations.HasFlag(WeaponLocation.ArmR)) return PartType.ArmR;
                    if (weapon.allowedLocations.HasFlag(WeaponLocation.ArmL)) return PartType.ArmL;
                    if (weapon.allowedLocations.HasFlag(WeaponLocation.BackR)) return PartType.BackR;
                    if (weapon.allowedLocations.HasFlag(WeaponLocation.BackL)) return PartType.BackL;
                }
                return PartType.ArmR;
            default: return PartType.Torso;
        }
    }

    private int GetOwnedCountForCategory(ShopCategory category)
    {
        if (PlayerInventoryManager.Instance == null) return 0;
        switch (category)
        {
            case ShopCategory.Head: return PlayerInventoryManager.Instance.ownedHeads.Count;
            case ShopCategory.Torso: return PlayerInventoryManager.Instance.ownedTorsos.Count;
            case ShopCategory.Arms: return PlayerInventoryManager.Instance.ownedArms.Count;
            case ShopCategory.Legs: return PlayerInventoryManager.Instance.ownedLegs.Count;
            case ShopCategory.Booster: return PlayerInventoryManager.Instance.ownedBoosters.Count;
            case ShopCategory.Generator: return PlayerInventoryManager.Instance.ownedGenerators.Count;
            case ShopCategory.FCS: return PlayerInventoryManager.Instance.ownedFCS.Count;
            case ShopCategory.Weapons: return PlayerInventoryManager.Instance.ownedWeapons.Count;
            default: return 0;
        }
    }

    private List<Part> GetPartsToDisplay(ShopCategory category)
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

        List<Part> filteredList = new List<Part>();
        int ownedCount = GetOwnedCountForCategory(category);

        foreach (Part p in allPartsInCategory)
        {
            bool isOwned = PlayerInventoryManager.Instance.IsPartOwned(p);

            if (isSellMode)
            {
                if (isOwned && p.price > 0 && ownedCount > 1) filteredList.Add(p);
            }
            else
            {
                if (!isOwned) filteredList.Add(p);
            }
        }
        return filteredList;
    }
}