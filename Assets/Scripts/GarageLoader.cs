using UnityEngine;
using System.Collections.Generic;

public class GarageLoader : MonoBehaviour
{
    // --- THE PERSISTENT LOADOUT ---
    // Because this is static, it survives scene changes! 
    // Your Arena scene can just read GarageLoader.ActiveLoadout to build the mech.
    public static Dictionary<PartType, Part> ActiveLoadout = new Dictionary<PartType, Part>();

    [Header("System Reference")]
    public PartSystem partSystem;

    [Header("Master Base Materials")]
    public BaseMaterialSetup[] globalBaseMaterials;
    public PlayerPaint[] globalPaintJob;

    [Header("Available Parts Inventory")]
    // The UI will read these lists to generate the buttons!
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
        // If the loadout is completely empty (first time booting the game), load defaults
        if (ActiveLoadout.Count == 0)
        {
            LoadDefaultMech();
        }
        else
        {
            // Otherwise, build whatever is currently saved in the static dictionary
            RefreshVisualMech();
        }
    }

    private void LoadDefaultMech()
    {
        // Safely grabs the first item in your lists to build a starter mech
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

    // The UI will call this whenever you click a part button
    public void EquipPart(PartType slot, Part newPart)
    {
        ActiveLoadout[slot] = newPart;
        RefreshVisualMech();
    }

    private void RefreshVisualMech()
    {
        if (partSystem == null) return;

        partSystem.equippedParts.Clear();

        partSystem.globalBaseMaterials = globalBaseMaterials;
        partSystem.currentPaintJob = globalPaintJob;

        // Copy our static persistent loadout into the visual PartSystem
        foreach (var kvp in ActiveLoadout)
        {
            partSystem.equippedParts.Add(kvp.Key, kvp.Value);
        }

        partSystem.InitializeMech();
    }
}
//tite