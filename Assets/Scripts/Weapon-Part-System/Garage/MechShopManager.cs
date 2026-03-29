using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MechShopManager : MonoBehaviour
{
    [Header("System References")]
    public GarageLoader garageLoader;

    [Header("Shop Catalog (Explicit)")]
    public List<HeadPart> shopHeads;
    public List<TorsoPart> shopTorsos;
    public List<ArmPart> shopArms;
    public List<LegPart> shopLegs;
    public List<Booster> shopBoosters;
    public List<Generator> shopGenerators;
    public List<FCSPart> shopFCS;
    public List<WeaponPart> shopWeapons;

    private bool _hasInitializedThisScene = false;

    private void Awake()
    {
        // Hook into the scene load event to catch when we return from the Arena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        _hasInitializedThisScene = false;
        InitializeShopAndMech();
    }

    private void Start()
    {
        // Fallback for the very first time the game boots up
        if (!_hasInitializedThisScene)
        {
            InitializeShopAndMech();
        }
    }

    public void InitializeShopAndMech()
    {
        _hasInitializedThisScene = true;

        // 1. AUTOMATICALLY REASSIGN GARAGE LOADER
        if (garageLoader == null)
        {
            garageLoader = Object.FindFirstObjectByType<GarageLoader>();
        }

        // 2. Combine all parts into a master catalog instantly (No yield delay!)
        List<Part> allGameParts = new List<Part>();
        allGameParts.AddRange(shopHeads);
        allGameParts.AddRange(shopTorsos);
        allGameParts.AddRange(shopArms);
        allGameParts.AddRange(shopLegs);
        allGameParts.AddRange(shopBoosters);
        allGameParts.AddRange(shopGenerators);
        allGameParts.AddRange(shopFCS);
        allGameParts.AddRange(shopWeapons);

        // 3. Instruct the persistent inventory to rebuild itself using our catalog
        if (PlayerInventoryManager.Instance != null)
        {
            PlayerInventoryManager.Instance.ForceSyncFromDiskAndCatalog(allGameParts);
        }

        // 4. Grant starter parts (only happens if they own nothing)
        GrantStarterParts();

        // 5. Build the mech!
        if (garageLoader != null)
        {
            garageLoader.InitializeStartupMech();
        }
        else
        {
            Debug.LogWarning("MechShopManager: Could not find a GarageLoader in the scene to spawn the mech!");
        }
    }

    private void GrantStarterParts()
    {
        PlayerInventoryManager inv = PlayerInventoryManager.Instance;
        if (inv == null) return;

        bool grantedAnything = false;

        if (inv.ownedHeads.Count == 0 && shopHeads.Count > 0) { inv.UnlockPart(shopHeads[0]); grantedAnything = true; }
        if (inv.ownedTorsos.Count == 0 && shopTorsos.Count > 0) { inv.UnlockPart(shopTorsos[0]); grantedAnything = true; }
        if (inv.ownedArms.Count == 0 && shopArms.Count > 0) { inv.UnlockPart(shopArms[0]); grantedAnything = true; }
        if (inv.ownedLegs.Count == 0 && shopLegs.Count > 0) { inv.UnlockPart(shopLegs[0]); grantedAnything = true; }
        if (inv.ownedBoosters.Count == 0 && shopBoosters.Count > 0) { inv.UnlockPart(shopBoosters[0]); grantedAnything = true; }
        if (inv.ownedGenerators.Count == 0 && shopGenerators.Count > 0) { inv.UnlockPart(shopGenerators[0]); grantedAnything = true; }
        if (inv.ownedFCS.Count == 0 && shopFCS.Count > 0) { inv.UnlockPart(shopFCS[0]); grantedAnything = true; }

        if (inv.ownedWeapons.Count == 0)
        {
            UnlockFirstWeaponForSlot(WeaponLocation.ArmL, inv);
            UnlockFirstWeaponForSlot(WeaponLocation.ArmR, inv);
            UnlockFirstWeaponForSlot(WeaponLocation.BackL, inv);
            UnlockFirstWeaponForSlot(WeaponLocation.BackR, inv);
            grantedAnything = true;
        }

        if (grantedAnything)
        {
            inv.SaveInventory();
        }
    }

    private void UnlockFirstWeaponForSlot(WeaponLocation location, PlayerInventoryManager inv)
    {
        foreach (WeaponPart weapon in shopWeapons)
        {
            if (weapon.allowedLocations.HasFlag(location))
            {
                inv.UnlockPart(weapon);
                return;
            }
        }
    }

    public void AttemptPurchase(Part partToBuy)
    {
        if (partToBuy == null) return;
        PlayerInventoryManager inventory = PlayerInventoryManager.Instance;

        if (inventory.SpendCredits(partToBuy.price))
        {
            inventory.UnlockPart(partToBuy);
        }
    }

    public void AttemptSell(Part partToSell)
    {
        if (partToSell == null) return;
        PlayerInventoryManager inventory = PlayerInventoryManager.Instance;

        inventory.AddCredits(partToSell.price);
        inventory.RemovePart(partToSell);

        if (garageLoader != null)
        {
            garageLoader.InitializeStartupMech();
        }
    }
}