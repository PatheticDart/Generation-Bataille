using UnityEngine;

public class ArenaMechLoader : MonoBehaviour
{
    public static PlayerPaint[] TransitPaintJob;

    [Header("System References")]
    public PartSystem partSystem;
    public MechStats mechStats;

    void Start()
    {
        if (partSystem == null) return;
        if (mechStats == null) mechStats = partSystem.GetComponent<MechStats>();

        int calculatedTotalAP = 0;

        // 1. Load Parts & Calculate Armor Points
        if (GarageLoader.ActiveLoadout != null && GarageLoader.ActiveLoadout.Count > 0)
        {
            partSystem.equippedParts.Clear();
            foreach (var kvp in GarageLoader.ActiveLoadout)
            {
                partSystem.equippedParts.Add(kvp.Key, kvp.Value);

                if (kvp.Value is BodyPart bodyPart)
                {
                    calculatedTotalAP += bodyPart.armorPoints;
                }
            }
        }

        // 2. Inject Health Stats
        if (mechStats != null)
        {
            mechStats.totalArmorPoints = calculatedTotalAP;
            mechStats.currentArmorPoints = calculatedTotalAP;
        }

        // 3. Build the physical mech geometry
        partSystem.InitializeMech();

        // --- NEW: DYNAMIC TARGET CENTERING ---
        CenterTargetObject();

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

    private void CenterTargetObject()
    {
        Transform targetObj = null;

        // Find the TargetObject wherever it spawned in the hierarchy
        Transform[] allTransforms = partSystem.GetComponentsInChildren<Transform>();
        foreach (Transform t in allTransforms)
        {
            if (t.name == "TargetObject")
            {
                targetObj = t;
                break;
            }
        }

        if (targetObj != null)
        {
            // Find all the dynamic hitboxes we just generated
            MeshCollider[] hitboxes = partSystem.GetComponentsInChildren<MeshCollider>();

            if (hitboxes.Length > 0)
            {
                // Create a bounding box that encapsulates every single mech part
                Bounds combinedBounds = hitboxes[0].bounds;
                for (int i = 1; i < hitboxes.Length; i++)
                {
                    combinedBounds.Encapsulate(hitboxes[i].bounds);
                }

                // Snap the TargetObject to the true physical center of the assembled mech!
                targetObj.position = combinedBounds.center;
            }
        }
    }
}