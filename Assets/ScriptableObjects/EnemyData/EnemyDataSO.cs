using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Mech/Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    public static EnemyDataSO ActiveEnemy;

    public enum EnemyRank { D, C, B, A, S, SS, SSS }

    [Header("UI & Match Info")]
    public string enemyName;
    [TextArea] public string description;
    public EnemyRank rank;
    public GameObject enemyPrefab;
    [Tooltip("How many credits the player earns for defeating this enemy.")]
    public int rewardCredits = 20000; // --- NEW: Custom Bounty ---

    [Header("Mech Loadout")]
    public HeadPart head;
    public TorsoPart torso;
    public ArmPart arms;
    public LegPart legs;
    public Booster booster;
    public Generator generator;
    public FCSPart fcs;

    [Header("Weapons")]
    public WeaponPart armL;
    public WeaponPart armR;
    public WeaponPart backL;
    public WeaponPart backR;

    [Header("Paint Job (Material Mapping)")]
    public Color primaryColor = Color.white;    // MechBase1
    public Color secondaryColor = Color.gray;   // MechBase2
    public Color tertiaryColor = Color.black;   // MechBase3
    public Color accentColor = Color.red;       // MechRed
    [ColorUsage(true, true)]
    public Color glowColor = Color.cyan;        // MechEye

    [Header("AI Brain: Stats & Probabilities")]
    [Range(0f, 100f)] public float energyEfficiency = 80f; // --- NEW: Energy Efficiency ---
    public ApproachActionType approachType = ApproachActionType.Boosting;
    public float approachRange = 300f;
    [Range(0f, 100f)] public float approachCriticalENRate = 30f;
    [Range(0f, 100f)] public float approachRequiredENRate = 70f;
    [Range(0f, 100f)] public float boostChance = 40f;
    [Range(0f, 100f)] public float quickBoostChance = 25f;
    [Range(0f, 100f)] public float perfectQuickBoostChance = 15f;

    [Header("AI Brain: Movement Chips")]
    public List<MovementChip> movementChips = new List<MovementChip>();

    [Header("AI Brain: Weapon Condition Chips")]
    public List<WeaponConditionChip> leftWeaponChips = new List<WeaponConditionChip>();
    public List<WeaponConditionChip> rightWeaponChips = new List<WeaponConditionChip>();
}