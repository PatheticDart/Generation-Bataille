using UnityEngine;

public class ArenaMechLoader : MonoBehaviour
{
    // The static messenger
    public static PlayerPaint[] TransitPaintJob;

    [Header("System References")]
    public PartSystem partSystem;
    public MechStats mechStats;

    void Start()
    {
        if (partSystem == null) return;

        // Auto-grab the MechStats if you forget to assign it in the inspector
        if (mechStats == null) mechStats = partSystem.GetComponent<MechStats>();

        int calculatedTotalAP = 0;

        // 1. Load Parts & Calculate Armor Points
        if (GarageLoader.ActiveLoadout != null && GarageLoader.ActiveLoadout.Count > 0)
        {
            partSystem.equippedParts.Clear();
            foreach (var kvp in GarageLoader.ActiveLoadout)
            {
                partSystem.equippedParts.Add(kvp.Key, kvp.Value);

                // Check if this part is a BodyPart (Head, Torso, Arms, Legs). 
                // If it is, add its armor value to our total!
                if (kvp.Value is BodyPart bodyPart)
                {
                    calculatedTotalAP += bodyPart.armorPoints;
                }
            }
        }

        // 2. Inject Health Stats
        if (mechStats != null)
        {
            // Set both the max cap and the current health to the calculated total
            mechStats.totalArmorPoints = calculatedTotalAP;
            mechStats.currentArmorPoints = calculatedTotalAP;
        }

        // 3. Build the physical mech geometry
        partSystem.InitializeMech();

        // 4. Load and Apply Paint
        if (TransitPaintJob != null && TransitPaintJob.Length > 0)
        {
            partSystem.currentPaintJob = TransitPaintJob;

            Renderer[] allRenderers = partSystem.GetComponentsInChildren<Renderer>();
            foreach (Renderer rend in allRenderers)
            {
                Material[] mats = rend.materials;
                for (int i = 0; i < mats.Length; i++)
                {
                    if (i < TransitPaintJob.Length)
                    {
                        // Force Inspector Colors to be 100% Solid to prevent invisible textures
                        Color safeAlbedo = TransitPaintJob[i].albedoColor;
                        safeAlbedo.a = 1f;

                        Color safeEmission = TransitPaintJob[i].emissionColor;
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
}