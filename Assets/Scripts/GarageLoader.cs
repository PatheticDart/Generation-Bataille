using UnityEngine;
using System.Collections.Generic;

public class GarageLoader : MonoBehaviour
{
    // --- THE PERSISTENT LOADOUT ---
    public static Dictionary<PartType, Part> ActiveLoadout = new Dictionary<PartType, Part>();

    [Header("System Reference")]
    public PartSystem partSystem;

    [Header("Master Base Materials")]
    public BaseMaterialSetup[] globalBaseMaterials;

    // Kept exactly as requested so MechCardManager can access it normally
    public PlayerPaint[] globalPaintJob;

    [Header("Available Patterns & Decals")]
    public List<Texture2D> availableAlbedoTextures;
    public List<Texture2D> availableEmissionTextures;

    [Header("Available Parts Inventory")]
    public List<HeadPart> availableHeads;
    public List<TorsoPart> availableTorsos;
    public List<ArmPart> availableArms;
    public List<LegPart> availableLegs;
    public List<Booster> availableBoosters;
    public List<Generator> availableGenerators;
    public List<FCSPart> availableFCS;

    [Header("Available Weapons")]
    public List<WeaponPart> availableLeftArmWeapons;
    public List<WeaponPart> availableRightArmWeapons;
    public List<WeaponPart> availableLeftBackWeapons;
    public List<WeaponPart> availableRightBackWeapons;

    void Start()
    {
        if (ActiveLoadout.Count == 0) LoadDefaultMech();
        else RefreshVisualMech();
    }

    private void LoadDefaultMech()
    {
        if (availableHeads.Count > 0) ActiveLoadout[PartType.Head] = availableHeads[0];
        if (availableTorsos.Count > 0) ActiveLoadout[PartType.Torso] = availableTorsos[0];
        if (availableArms.Count > 0) ActiveLoadout[PartType.Arms] = availableArms[0];
        if (availableLegs.Count > 0) ActiveLoadout[PartType.Legs] = availableLegs[0];
        if (availableBoosters.Count > 0) ActiveLoadout[PartType.Booster] = availableBoosters[0];
        if (availableGenerators.Count > 0) ActiveLoadout[PartType.Generator] = availableGenerators[0];
        if (availableFCS.Count > 0) ActiveLoadout[PartType.FCS] = availableFCS[0];

        if (availableLeftArmWeapons.Count > 0) ActiveLoadout[PartType.ArmL] = availableLeftArmWeapons[0];
        if (availableRightArmWeapons.Count > 0) ActiveLoadout[PartType.ArmR] = availableRightArmWeapons[0];
        if (availableLeftBackWeapons.Count > 0) ActiveLoadout[PartType.BackL] = availableLeftBackWeapons[0];
        if (availableRightBackWeapons.Count > 0) ActiveLoadout[PartType.BackR] = availableRightBackWeapons[0];

        RefreshVisualMech();
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
                    // --- THE BUG FIX: Force Inspector Colors to be 100% Solid ---
                    Color safeAlbedo = globalPaintJob[i].albedoColor;
                    safeAlbedo.a = 1f;

                    Color safeEmission = globalPaintJob[i].emissionColor;
                    safeEmission.a = 1f;

                    if (i == 4)
                    {
                        mats[i].SetColor("_EmissionColor", safeEmission);
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