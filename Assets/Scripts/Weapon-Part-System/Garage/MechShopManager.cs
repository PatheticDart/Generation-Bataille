using System.Collections.Generic;
using UnityEngine;

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
    
    [Header("Shop Catalog (Combined Weapons)")]
    public List<WeaponPart> shopWeapons; 

    private void Start()
    {
        // 1. Give the player the starter parts first
        GrantStarterParts(); 

        // 2. NOW explicitly tell GarageLoader to build the mech.
        if (garageLoader != null)
        {
            garageLoader.InitializeStartupMech();
        }
        else
        {
            Debug.LogWarning("ShopManager needs a reference to GarageLoader to spawn the starter mech!");
        }
    }

    private void GrantStarterParts()
    {
        PlayerInventoryManager inv = PlayerInventoryManager.Instance;
        if (inv == null) return;

        // Give the first index of every base body part if the player owns nothing
        if (inv.ownedHeads.Count == 0 && shopHeads.Count > 0) inv.UnlockPart(shopHeads[0]);
        if (inv.ownedTorsos.Count == 0 && shopTorsos.Count > 0) inv.UnlockPart(shopTorsos[0]);
        if (inv.ownedArms.Count == 0 && shopArms.Count > 0) inv.UnlockPart(shopArms[0]);
        if (inv.ownedLegs.Count == 0 && shopLegs.Count > 0) inv.UnlockPart(shopLegs[0]);
        if (inv.ownedBoosters.Count == 0 && shopBoosters.Count > 0) inv.UnlockPart(shopBoosters[0]);
        if (inv.ownedGenerators.Count == 0 && shopGenerators.Count > 0) inv.UnlockPart(shopGenerators[0]);
        if (inv.ownedFCS.Count == 0 && shopFCS.Count > 0) inv.UnlockPart(shopFCS[0]);

        // Give the first valid weapon for each of the 4 weapon slots
        if (inv.ownedWeapons.Count == 0)
        {
            UnlockFirstWeaponForSlot(WeaponLocation.ArmL, inv);
            UnlockFirstWeaponForSlot(WeaponLocation.ArmR, inv);
            UnlockFirstWeaponForSlot(WeaponLocation.BackL, inv);
            UnlockFirstWeaponForSlot(WeaponLocation.BackR, inv);
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

        if (inventory.IsPartOwned(partToBuy))
        {
            Debug.Log($"You already own {partToBuy.partName}.");
            return;
        }

        if (inventory.SpendCredits(partToBuy.price))
        {
            inventory.UnlockPart(partToBuy);
            Debug.Log($"Purchased {partToBuy.partName} for {partToBuy.price} credits! Remaining: {inventory.currentCredits}");
        }
        else
        {
            Debug.Log($"Not enough credits for {partToBuy.partName}. Need {partToBuy.price}.");
        }
    }

    // --- NEW SELLING LOGIC ---
    public void AttemptSell(Part partToSell)
    {
        if (partToSell == null) return;

        PlayerInventoryManager inventory = PlayerInventoryManager.Instance;

        if (!inventory.IsPartOwned(partToSell))
        {
            Debug.Log($"You don't own {partToSell.partName}, so you can't sell it.");
            return;
        }

        // Add credits back (100% refund as requested)
        inventory.AddCredits(partToSell.price);
        
        // Remove from inventory
        inventory.RemovePart(partToSell);
        
        Debug.Log($"Sold {partToSell.partName} for {partToSell.price} credits! Remaining: {inventory.currentCredits}");

        // Safety net: If they sold a part they were actively wearing, tell GarageLoader 
        // to re-evaluate the loadout so it strips the sold part and auto-equips the default!
        if (garageLoader != null)
        {
            garageLoader.InitializeStartupMech();
        }
    }
}