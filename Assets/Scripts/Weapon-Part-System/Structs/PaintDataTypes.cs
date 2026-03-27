using System;
using UnityEngine;

// Global definitions for your customization system

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
    public Color albedoColor;
    public Texture2D albedoTexture;
    
    // Helper to check if the player actually changed anything
    public bool HasOverrides => (albedoColor.a > 0 && albedoColor != Color.clear) || albedoTexture != null;
}