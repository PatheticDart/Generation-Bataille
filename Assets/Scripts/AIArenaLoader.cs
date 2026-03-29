using UnityEngine;

[RequireComponent(typeof(PartSystem))]
[RequireComponent(typeof(PrototypeAIBrain))]
public class AIArenaLoader : MonoBehaviour
{
    // --- THE FIX: Master Base Materials ---
    [Header("Master Base Materials")]
    public BaseMaterialSetup[] globalBaseMaterials;

    private PartSystem partSystem;
    private PrototypeAIBrain aiBrain;
    private MechStats mechStats; // --- THE FIX: Added MechStats ---

    void Start()
    {
        partSystem = GetComponent<PartSystem>();
        aiBrain = GetComponent<PrototypeAIBrain>();
        mechStats = GetComponent<MechStats>();

        if (mechStats == null) mechStats = partSystem.GetComponent<MechStats>();

        if (EnemyDataSO.ActiveEnemy != null)
        {
            LoadEnemy(EnemyDataSO.ActiveEnemy);
        }
        else
        {
            Debug.LogWarning("AIArenaLoader: No ActiveEnemy was set! Loading default prefab layout.");
        }
    }

    private void LoadEnemy(EnemyDataSO data)
    {
        // 1. CLEAR AND APPLY MECH PARTS
        partSystem.equippedParts.Clear();

        if (data.head != null) partSystem.equippedParts.Add(PartType.Head, data.head);
        if (data.torso != null) partSystem.equippedParts.Add(PartType.Torso, data.torso);
        if (data.arms != null) partSystem.equippedParts.Add(PartType.Arms, data.arms);
        if (data.legs != null) partSystem.equippedParts.Add(PartType.Legs, data.legs);
        if (data.booster != null) partSystem.equippedParts.Add(PartType.Booster, data.booster);
        if (data.generator != null) partSystem.equippedParts.Add(PartType.Generator, data.generator);
        if (data.fcs != null) partSystem.equippedParts.Add(PartType.FCS, data.fcs);

        if (data.armL != null) partSystem.equippedParts.Add(PartType.ArmL, data.armL);
        if (data.armR != null) partSystem.equippedParts.Add(PartType.ArmR, data.armR);
        if (data.backL != null) partSystem.equippedParts.Add(PartType.BackL, data.backL);
        if (data.backR != null) partSystem.equippedParts.Add(PartType.BackR, data.backR);

        // --- THE FIX: Calculate Armor Points ---
        int calculatedTotalAP = 0;
        foreach (var part in partSystem.equippedParts.Values)
        {
            if (part is BodyPart bodyPart)
            {
                calculatedTotalAP += bodyPart.armorPoints;
            }
        }

        if (mechStats != null)
        {
            mechStats.totalArmorPoints = calculatedTotalAP;
            mechStats.currentArmorPoints = calculatedTotalAP;
        }

        // --- THE FIX: Assign Master Materials BEFORE building! ---
        if (globalBaseMaterials != null && globalBaseMaterials.Length > 0)
        {
            partSystem.globalBaseMaterials = globalBaseMaterials;
        }

        // Build the physical mesh
        partSystem.InitializeMech();

        // --- THE FIX: Center the hitbox target for proper aiming ---
        CenterTargetObject();

        // Apply Advanced Paint Mapping
        ApplyAIPaint(data.primaryColor, data.secondaryColor, data.tertiaryColor, data.accentColor, data.glowColor);

        // 2. INJECT THE AI BRAIN STATS
        aiBrain.energyEfficiency = data.energyEfficiency;
        aiBrain.approachType = data.approachType;
        aiBrain.approachRange = data.approachRange;
        aiBrain.approachCriticalENRate = data.approachCriticalENRate;
        aiBrain.approachRequiredENRate = data.approachRequiredENRate;
        aiBrain.boostChance = data.boostChance;
        aiBrain.quickBoostChance = data.quickBoostChance;
        aiBrain.perfectQuickBoostChance = data.perfectQuickBoostChance;

        aiBrain.movementOrderList = new System.Collections.Generic.List<MovementChip>(data.movementChips);
        aiBrain.leftWeaponConditions = new System.Collections.Generic.List<WeaponConditionChip>(data.leftWeaponChips);
        aiBrain.rightWeaponConditions = new System.Collections.Generic.List<WeaponConditionChip>(data.rightWeaponChips);

        Debug.Log($"<color=red>Enemy Loaded: {data.enemyName} | Total AP: {calculatedTotalAP}</color>");
    }

    // --- RESTORED: Dynamic Target Centering ---
    private void CenterTargetObject()
    {
        Transform targetObj = null;
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
            MeshCollider[] hitboxes = partSystem.GetComponentsInChildren<MeshCollider>();

            if (hitboxes.Length > 0)
            {
                Bounds combinedBounds = hitboxes[0].bounds;
                for (int i = 1; i < hitboxes.Length; i++)
                {
                    combinedBounds.Encapsulate(hitboxes[i].bounds);
                }
                targetObj.position = combinedBounds.center;
            }
        }
    }

    private void ApplyAIPaint(Color primary, Color secondary, Color tertiary, Color accent, Color glow)
    {
        Renderer[] allRenderers = partSystem.GetComponentsInChildren<Renderer>();
        foreach (Renderer rend in allRenderers)
        {
            Material[] mats = rend.materials;
            for (int i = 0; i < mats.Length; i++)
            {
                string matName = mats[i].name;
                Color targetAlbedo = Color.white;
                bool isGlow = false;

                // Match the material name (using Contains because Unity adds "(Instance)")
                if (matName.Contains("MechBase1")) targetAlbedo = primary;
                else if (matName.Contains("MechBase2")) targetAlbedo = secondary;
                else if (matName.Contains("MechBase3")) targetAlbedo = tertiary;
                else if (matName.Contains("MechRed")) targetAlbedo = accent;
                else if (matName.Contains("MechEye"))
                {
                    targetAlbedo = glow;
                    isGlow = true;
                }
                else continue; // Skip materials that don't match our specific setup

                targetAlbedo.a = 1f;
                mats[i].SetColor("_Color", targetAlbedo);
                mats[i].SetColor("_BaseColor", targetAlbedo);

                if (isGlow)
                {
                    Color targetEmission = glow;
                    targetEmission.a = 1f;

                    float maxRGB = Mathf.Max(targetEmission.r, targetEmission.g, targetEmission.b);
                    Color baseEmission = targetEmission;
                    if (maxRGB > 0) baseEmission = targetEmission / maxRGB;

                    mats[i].SetColor("_EmissionColor", baseEmission * 32f); // Using your EMISSIONFACTOR of 32
                }
            }
            rend.materials = mats;
        }
    }
}