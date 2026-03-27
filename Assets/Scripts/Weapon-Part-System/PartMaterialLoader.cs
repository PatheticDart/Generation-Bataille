using System;
using System.Collections.Generic;
using UnityEngine;

public class PartMaterialLoader : MonoBehaviour
{
    [Serializable]
    public struct MeshMapping
    {
        public MeshRenderer mesh;
        public PartMaterialLocation[] materialLocations;
    }

    [Header("Prefab Setup")]
    public MeshMapping[] meshes;

    // Cache Shader IDs
    private readonly int baseColorId = Shader.PropertyToID("_BaseColor");
    private readonly int baseMapId = Shader.PropertyToID("_BaseMap");
    private readonly int emissionColorId = Shader.PropertyToID("_EmissionColor"); // NEW
    private readonly int emissionMapId = Shader.PropertyToID("_EmissionMap");     // NEW

    private List<Material> _instancedMaterials = new List<Material>();

    public void AssignMaterials(PlayerPaint[] playerPaintJobs, BaseMaterialSetup[] baseMaterials)
    {
        CleanupMaterials();

        if (meshes == null) return;

        foreach (MeshMapping m in meshes)
        {
            if (m.mesh == null) continue;

            Material[] currentMaterials = m.mesh.sharedMaterials;
            Material[] newMaterials = new Material[currentMaterials.Length];

            for (int i = 0; i < currentMaterials.Length; i++)
            {
                newMaterials[i] = currentMaterials[i]; // Fallback

                if (i >= m.materialLocations.Length) continue;

                PartMaterialLocation myLocation = m.materialLocations[i];
                Material sourceMat = currentMaterials[i];
                bool hasBaseOverride = false;

                // 1. Check for Global Base Material
                if (baseMaterials != null)
                {
                    foreach (var b in baseMaterials)
                    {
                        if (b.map == myLocation && b.material != null)
                        {
                            sourceMat = b.material;
                            hasBaseOverride = true;
                            break;
                        }
                    }
                }

                if (sourceMat == null) continue;

                // 2. Check for Player Customization
                bool playerPainted = false;
                if (playerPaintJobs != null)
                {
                    foreach (PlayerPaint paint in playerPaintJobs)
                    {
                        if (paint.map == myLocation && paint.HasOverrides) 
                        {
                            Material instancedMat = new Material(sourceMat);
                            
                            // Apply Albedo
                            if (paint.albedoColor.a > 0 && paint.albedoColor != Color.clear)
                                instancedMat.SetColor(baseColorId, paint.albedoColor);
                                
                            if (paint.albedoTexture != null)
                                instancedMat.SetTexture(baseMapId, paint.albedoTexture);

                            // NEW: Apply Emission
                            if (paint.emissionColor.a > 0 && paint.emissionColor != Color.clear && paint.emissionColor != Color.black)
                            {
                                instancedMat.SetColor(emissionColorId, paint.emissionColor);
                                instancedMat.EnableKeyword("_EMISSION"); // Tell the shader to turn the light on!
                            }

                            if (paint.emissionTexture != null)
                            {
                                instancedMat.SetTexture(emissionMapId, paint.emissionTexture);
                                instancedMat.EnableKeyword("_EMISSION");
                            }

                            newMaterials[i] = instancedMat;
                            _instancedMaterials.Add(instancedMat);
                            playerPainted = true;
                            break; 
                        }
                    }
                }

                // 3. Fallback
                if (!playerPainted && hasBaseOverride)
                {
                    newMaterials[i] = sourceMat;
                }
            }
            
            m.mesh.materials = newMaterials;
        }
    }

    private void CleanupMaterials()
    {
        foreach (Material mat in _instancedMaterials)
        {
            if (mat != null)
            {
                if (Application.isPlaying) Destroy(mat);
                else DestroyImmediate(mat);
            }
        }
        _instancedMaterials.Clear();
    }

    private void OnDestroy() { CleanupMaterials(); }
}