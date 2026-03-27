using UnityEngine;

public class ArenaMechLoader : MonoBehaviour
{
    public PartSystem partSystem;

    void Start()
    {
        if (partSystem == null) return;

        // If we came from the Garage, copy the static loadout!
        if (GarageLoader.ActiveLoadout != null && GarageLoader.ActiveLoadout.Count > 0)
        {
            partSystem.equippedParts.Clear();
            foreach (var kvp in GarageLoader.ActiveLoadout)
            {
                partSystem.equippedParts.Add(kvp.Key, kvp.Value);
            }
        }

        // Build the physical mech
        partSystem.InitializeMech();
    }
}