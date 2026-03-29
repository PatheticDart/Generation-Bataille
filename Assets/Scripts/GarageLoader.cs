using UnityEngine;
using System.Collections.Generic;

public class GarageLoader : MonoBehaviour
{
    public const float EMISSIONFACTOR = 32f;

    public static Dictionary<PartType, Part> ActiveLoadout = new Dictionary<PartType, Part>();

    [Header("System Reference")]
    public PartSystem partSystem;
    public BaseMaterialSetup[] globalBaseMaterials;
    public PlayerPaint[] globalPaintJob;

    [Header("Available Patterns & Decals")]
    public List<Texture2D> availableAlbedoTextures;
    public List<Texture2D> availableEmissionTextures;

    // --- INVENTORY REFERENCES ---
    public List<HeadPart> availableHeads => PlayerInventoryManager.Instance.ownedHeads;
    public List<TorsoPart> availableTorsos => PlayerInventoryManager.Instance.ownedTorsos;
    public List<ArmPart> availableArms => PlayerInventoryManager.Instance.ownedArms;
    public List<LegPart> availableLegs => PlayerInventoryManager.Instance.ownedLegs;
    public List<Booster> availableBoosters => PlayerInventoryManager.Instance.ownedBoosters;
    public List<Generator> availableGenerators => PlayerInventoryManager.Instance.ownedGenerators;
    public List<FCSPart> availableFCS => PlayerInventoryManager.Instance.ownedFCS;
    public List<WeaponPart> allAvailableWeapons => PlayerInventoryManager.Instance.ownedWeapons;

    public void InitializeStartupMech()
    {
        ValidateLoadout();
        RefreshVisualMech();
    }

    private void ValidateLoadout()
    {
        if (PlayerInventoryManager.Instance == null)
        {
            Debug.LogError("GarageLoader needs PlayerInventoryManager in the scene to load parts!");
            return;
        }

        // Helper to check standard body parts
        void CheckAndEquipDefault<T>(PartType slot, List<T> inventory) where T : Part
        {
            // If the part is missing, null, or NOT owned by the player, overwrite it with the default
            if (!ActiveLoadout.TryGetValue(slot, out Part currentPart) || currentPart == null || !PlayerInventoryManager.Instance.IsPartOwned(currentPart))
            {
                if (inventory.Count > 0) ActiveLoadout[slot] = inventory[0];
                else ActiveLoadout.Remove(slot); // Strip it if no default exists
            }
        }

        // Helper to check weapons (uses the WeaponLocation flags)
        void CheckAndEquipDefaultWeapon(PartType slot, WeaponLocation location)
        {
            if (!ActiveLoadout.TryGetValue(slot, out Part currentPart) || currentPart == null || !PlayerInventoryManager.Instance.IsPartOwned(currentPart))
            {
                WeaponPart defaultWeapon = GetFirstValidWeapon(location);
                if (defaultWeapon != null) ActiveLoadout[slot] = defaultWeapon;
                else ActiveLoadout.Remove(slot);
            }
        }

        // 1. Validate Body Parts
        CheckAndEquipDefault(PartType.Head, availableHeads);
        CheckAndEquipDefault(PartType.Torso, availableTorsos);
        CheckAndEquipDefault(PartType.Arms, availableArms);
        CheckAndEquipDefault(PartType.Legs, availableLegs);
        CheckAndEquipDefault(PartType.Booster, availableBoosters);
        CheckAndEquipDefault(PartType.Generator, availableGenerators);
        CheckAndEquipDefault(PartType.FCS, availableFCS);

        // 2. Validate Weapons
        CheckAndEquipDefaultWeapon(PartType.ArmL, WeaponLocation.ArmL);
        CheckAndEquipDefaultWeapon(PartType.ArmR, WeaponLocation.ArmR);
        CheckAndEquipDefaultWeapon(PartType.BackL, WeaponLocation.BackL);
        CheckAndEquipDefaultWeapon(PartType.BackR, WeaponLocation.BackR);
    }

    // --- HELPER METHODS FOR INGAME ALLOCATION & UI ---

    public List<WeaponPart> GetValidWeaponsForSlot(WeaponLocation targetLocation)
    {
        List<WeaponPart> validWeapons = new List<WeaponPart>();
        if (allAvailableWeapons == null) return validWeapons;

        foreach (WeaponPart weapon in allAvailableWeapons)
        {
            if (weapon.allowedLocations.HasFlag(targetLocation))
            {
                validWeapons.Add(weapon);
            }
        }
        return validWeapons;
    }

    private WeaponPart GetFirstValidWeapon(WeaponLocation targetLocation)
    {
        if (allAvailableWeapons == null) return null;

        foreach (WeaponPart weapon in allAvailableWeapons)
        {
            if (weapon.allowedLocations.HasFlag(targetLocation))
            {
                return weapon;
            }
        }
        return null;
    }

    public void EquipPart(PartType slot, Part newPart)
    {
        ActiveLoadout[slot] = newPart;
        RefreshVisualMech();
    }

    public void RefreshVisualMech()
    {
        if (partSystem == null) return;

        partSystem.equippedParts.Clear();
        partSystem.globalBaseMaterials = globalBaseMaterials;
        partSystem.currentPaintJob = globalPaintJob;

        foreach (var kvp in ActiveLoadout)
        {
            partSystem.equippedParts.Add(kvp.Key, kvp.Value);
        }

        partSystem.InitializeMech();
        FastApplyPaintToMech();
    }

    public void FastApplyPaintToMech()
    {
        if (partSystem == null) return;

        partSystem.currentPaintJob = globalPaintJob;
        Renderer[] allRenderers = partSystem.GetComponentsInChildren<Renderer>();

        foreach (Renderer rend in allRenderers)
        {
            Material[] mats = rend.materials;

            for (int i = 0; i < mats.Length; i++)
            {
                if (i < globalPaintJob.Length)
                {
                    Color safeAlbedo = globalPaintJob[i].albedoColor;
                    safeAlbedo.a = 1f;

                    Color safeEmission = globalPaintJob[i].emissionColor;
                    safeEmission.a = 1f;

                    if (i == 4) // Assuming index 4 is your emission slot
                    {
                        float maxRGB = Mathf.Max(safeEmission.r, safeEmission.g, safeEmission.b);
                        Color baseColor = safeEmission;
                        if (maxRGB > 0) baseColor = safeEmission / maxRGB;

                        // Force intensity 4
                        mats[i].SetColor("_EmissionColor", baseColor * EMISSIONFACTOR);
                    }
                    else
                    {
                        mats[i].SetColor("_Color", safeAlbedo);
                        mats[i].SetColor("_BaseColor", safeAlbedo);
                    }
                }
            }
            rend.materials = mats;
        }
    }
}