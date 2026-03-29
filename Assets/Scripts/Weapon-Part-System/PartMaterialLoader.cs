using System;
using System.Collections.Generic;
using UnityEngine;

public class PartMaterialLoader : MonoBehaviour
{
    // --- SETTINGS ENUMS ---
    public enum ColorChannel { Albedo, Emission }

    // --- MAPPING STRUCTS ---
    [Serializable]
    public struct MeshMapping
    {
        public MeshRenderer mesh;
        public PartMaterialLocation[] materialLocations;
    }

    [Serializable]
    public struct LightMapping
    {
        public Light lightComponent;
        [Tooltip("Which paint slot should control this light's color?")]
        public PartMaterialLocation materialLocation;
    }

    [Serializable]
    public struct TrailMapping
    {
        public TrailRenderer trailComponent;

        [Header("Start Color")]
        public PartMaterialLocation startLocation;
        public ColorChannel startChannel;

        [Header("End Color")]
        public PartMaterialLocation endLocation;
        public ColorChannel endChannel;
    }

    [Header("Prefab Setup - Meshes")]
    public MeshMapping[] meshes;

    [Header("Prefab Setup - Lights")]
    public LightMapping[] lights;

    [Header("Prefab Setup - Trails")]
    public TrailMapping[] trails;

    // Cache Shader IDs
    private readonly int baseColorId = Shader.PropertyToID("_BaseColor");
    private readonly int baseMapId = Shader.PropertyToID("_BaseMap");
    private readonly int emissionColorId = Shader.PropertyToID("_EmissionColor"); 
    private readonly int emissionMapId = Shader.PropertyToID("_EmissionMap");     

    private List<Material> _instancedMaterials = new List<Material>();
    
    // Cache original colors so we can revert them if paint is removed
    private Dictionary<Light, Color> _originalLightColors = new Dictionary<Light, Color>();
    private Dictionary<TrailRenderer, Gradient> _originalTrails = new Dictionary<TrailRenderer, Gradient>();

    public void AssignMaterials(PlayerPaint[] playerPaintJobs, BaseMaterialSetup[] baseMaterials)
    {
        CleanupMaterials();

        // 1. --- HANDLE MESHES ---
        if (meshes != null)
        {
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

                    bool playerPainted = false;
                    if (playerPaintJobs != null)
                    {
                        foreach (PlayerPaint paint in playerPaintJobs)
                        {
                            if (paint.map == myLocation && paint.HasOverrides) 
                            {
                                Material instancedMat = new Material(sourceMat);
                                
                                if (paint.albedoColor.a > 0 && paint.albedoColor != Color.clear)
                                    instancedMat.SetColor(baseColorId, paint.albedoColor);
                                    
                                if (paint.albedoTexture != null)
                                    instancedMat.SetTexture(baseMapId, paint.albedoTexture);

                                if (paint.emissionColor.a > 0 && paint.emissionColor != Color.clear && paint.emissionColor != Color.black)
                                {
                                    // 1. Get the base color (normalize to 0-1 range)
                                    float maxRGB = Mathf.Max(paint.emissionColor.r, paint.emissionColor.g, paint.emissionColor.b);
                                    Color baseColor = paint.emissionColor;
                                    
                                    if (maxRGB > 0) baseColor = paint.emissionColor / maxRGB;

                                    // 2. Apply hardcoded intensity (2^4 = 16)
                                    Color finalHDRColor = baseColor * GarageLoader.EMISSIONFACTOR; 

                                    instancedMat.SetColor(emissionColorId, finalHDRColor);
                                    instancedMat.EnableKeyword("_EMISSION"); 
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

                    if (!playerPainted && hasBaseOverride)
                    {
                        newMaterials[i] = sourceMat;
                    }
                }
                m.mesh.materials = newMaterials;
            }
        }

        // 2. --- HANDLE LIGHTS ---
        if (lights != null)
        {
            foreach (LightMapping lMap in lights)
            {
                if (lMap.lightComponent == null) continue;

                if (!_originalLightColors.ContainsKey(lMap.lightComponent))
                    _originalLightColors[lMap.lightComponent] = lMap.lightComponent.color;

                Color finalLightColor = _originalLightColors[lMap.lightComponent];

                if (playerPaintJobs != null)
                {
                    foreach (PlayerPaint paint in playerPaintJobs)
                    {
                        if (paint.map == lMap.materialLocation && paint.HasOverrides)
                        {
                            if (paint.emissionColor.a > 0 && paint.emissionColor != Color.clear && paint.emissionColor != Color.black)
                            {
                                float maxColorComponent = Mathf.Max(paint.emissionColor.r, paint.emissionColor.g, paint.emissionColor.b);
                                finalLightColor = maxColorComponent > 1f ? paint.emissionColor / maxColorComponent : paint.emissionColor;
                            }
                            else if (paint.albedoColor.a > 0 && paint.albedoColor != Color.clear)
                            {
                                finalLightColor = paint.albedoColor;
                            }
                            break; 
                        }
                    }
                }
                lMap.lightComponent.color = finalLightColor;
            }
        }

        // 3. --- NEW: HANDLE TRAILS ---
        if (trails != null)
        {
            foreach (TrailMapping tMap in trails)
            {
                if (tMap.trailComponent == null) continue;

                // Cache original gradient to preserve alphas
                if (!_originalTrails.ContainsKey(tMap.trailComponent))
                {
                    // Gradients are reference types, so we must clone it, not just assign it
                    Gradient clone = new Gradient();
                    clone.SetKeys(tMap.trailComponent.colorGradient.colorKeys, tMap.trailComponent.colorGradient.alphaKeys);
                    _originalTrails[tMap.trailComponent] = clone;
                }

                Gradient origGrad = _originalTrails[tMap.trailComponent];
                
                // Extract the colors using our helper function
                Color startColor = GetColorForChannel(playerPaintJobs, tMap.startLocation, tMap.startChannel, origGrad.colorKeys[0].color);
                Color endColor = GetColorForChannel(playerPaintJobs, tMap.endLocation, tMap.endChannel, origGrad.colorKeys[origGrad.colorKeys.Length - 1].color);

                // Build a new gradient combining the custom colors with the original transparency
                Gradient newGrad = new Gradient();
                newGrad.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(startColor, 0f), new GradientColorKey(endColor, 1f) },
                    origGrad.alphaKeys // Preserve the original fade-out!
                );

                tMap.trailComponent.colorGradient = newGrad;
            }
        }
    }

    // Helper method to dig the exact color we want out of the Paint Job
    private Color GetColorForChannel(PlayerPaint[] paintJobs, PartMaterialLocation targetLoc, ColorChannel targetChannel, Color fallbackColor)
    {
        if (paintJobs == null) return fallbackColor;

        foreach (PlayerPaint paint in paintJobs)
        {
            if (paint.map == targetLoc && paint.HasOverrides)
            {
                if (targetChannel == ColorChannel.Emission && paint.emissionColor.a > 0 && paint.emissionColor != Color.clear && paint.emissionColor != Color.black)
                {
                    return paint.emissionColor;
                }
                
                if (targetChannel == ColorChannel.Albedo && paint.albedoColor.a > 0 && paint.albedoColor != Color.clear)
                {
                    return paint.albedoColor;
                }

                // Smart Fallback: If they asked for Emission but only painted Albedo, just use Albedo so it doesn't break
                if (paint.albedoColor.a > 0 && paint.albedoColor != Color.clear) return paint.albedoColor;
            }
        }
        return fallbackColor;
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