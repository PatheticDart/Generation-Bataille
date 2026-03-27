using UnityEngine;
using System.Collections.Generic;

public class GarageLoader : MonoBehaviour
{
    // --- THE PERSISTENT LOADOUT ---
    public static Dictionary<PartType, Part> ActiveLoadout = new Dictionary<PartType, Part>();
    
    // --- NEW: THE PERSISTENT PAINT JOB ---
    // Indexes: 0=Primary, 1=Secondary, 2=Tertiary, 3=Accent, 4=Glow
    public static PlayerPaint[] ActivePaintJob = new PlayerPaint[5];

    [Header("System Reference")]
    public PartSystem partSystem;

    [Header("Master Base Materials")]
    public BaseMaterialSetup[] globalBaseMaterials; 
    public PlayerPaint[] defaultPaintJob; // Rename your old 'globalPaintJob' to this

    // ... [Keep your existing Available Parts Lists here] ...

    void Start()
    {
        // 1. Initialize Default Parts if empty
        if (ActiveLoadout.Count == 0) LoadDefaultMech();
        else RefreshVisualMech();

        // 2. Initialize Default Paint if empty
        if (ActivePaintJob[0] == null || ActivePaintJob[0].color == Color.clear)
        {
            for (int i = 0; i < 5; i++)
            {
                if (i < defaultPaintJob.Length) ActivePaintJob[i] = defaultPaintJob[i];
            }
        }
    }

    // ... [Keep LoadDefaultMech, EquipPart, and RefreshVisualMech exactly the same] ...

    // --- NEW: Fast Material Updater ---
    // This updates the colors WITHOUT rebuilding the 3D meshes
    public void FastApplyPaintToMech()
    {
        if (partSystem == null) return;

        // Update the PartSystem's reference
        partSystem.currentPaintJob = ActivePaintJob;

        // Note: You will need to call whatever function inside your PartSystem or BaseMaterialSetup 
        // that actually applies the 'currentPaintJob' array to the MeshRenderers. 
        // Example: partSystem.ApplyMaterialsToSpawnedParts();
    }
}