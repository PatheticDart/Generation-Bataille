using System.Collections.Generic;
using UnityEngine;

public class PartSystem : MonoBehaviour
{
    [Header("Inventory Handling")]
    public Dictionary<PartType, Part> equippedParts = new();

    public void SetPart(PartType location, Part part)
    {
        // onUnequip
        equippedParts[location] = part;
        // onEquip
    }

    public void InitializeMech()
    {
        // Build Mech
        foreach(PartType key in equippedParts.Keys)
        {
            SpawnPartAtLocation(key);
        }

        // Initialize Weapon Manager

        // Initialize Movement Stats
    }

    void SpawnPartAtLocation(PartType location)
    {
        
    }
}