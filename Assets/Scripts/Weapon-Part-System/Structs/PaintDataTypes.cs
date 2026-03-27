using System;
using UnityEngine;
using NaughtyAttributes; // Don't forget this!

[Serializable]
public struct BaseMaterialSetup
{
    public PartMaterialLocation map;
    public Material material;
}

[Serializable]
public struct PlayerPaint
{
    public PartMaterialLocation map;
    
    [Header("Albedo (Base)")]
    public Color albedoColor;
    public Texture2D albedoTexture;

    // --- NAUGHTY ATTRIBUTES LOGIC ---
    // A hidden helper property that NaughtyAttributes checks to see if it should show the fields
    private bool IsGlow => map == PartMaterialLocation.Glow;

    [ShowIf("IsGlow"), AllowNesting] // AllowNesting is required for structs inside arrays!
    [ColorUsage(true, true)] 
    public Color emissionColor;

    [ShowIf("IsGlow"), AllowNesting]
    public Texture2D emissionTexture;
    
    // Helper to check if the player actually changed anything
    public bool HasOverrides => 
        (albedoColor.a > 0 && albedoColor != Color.clear) || 
        albedoTexture != null ||
        (emissionColor.a > 0 && emissionColor != Color.clear && emissionColor != Color.black) ||
        emissionTexture != null;
}