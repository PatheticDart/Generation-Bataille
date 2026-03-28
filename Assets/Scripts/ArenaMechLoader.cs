using UnityEngine;

public class ArenaMechLoader : MonoBehaviour
{
    // The static messenger
    public static PlayerPaint[] TransitPaintJob;

    public PartSystem partSystem;

    void Start()
    {
        if (partSystem == null) return;

        // 1. Load Parts
        if (GarageLoader.ActiveLoadout != null && GarageLoader.ActiveLoadout.Count > 0)
        {
            partSystem.equippedParts.Clear();
            foreach (var kvp in GarageLoader.ActiveLoadout)
            {
                partSystem.equippedParts.Add(kvp.Key, kvp.Value);
            }
        }

        partSystem.InitializeMech();

        // 2. Load and Apply Paint
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
                        // --- THE BUG FIX: Force Inspector Colors to be 100% Solid ---
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