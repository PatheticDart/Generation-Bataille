using UnityEngine;

public class DEBUG_ManualLoader : MonoBehaviour
{
    [Header("System Reference")]
    public PartSystem partSystem;

    [Header("Master Base Materials")]
    [Tooltip("Assign your ToonLightSafe materials here (Primary, Secondary, etc.)")]
    // FIX: Removed "PartMaterialLoader."
    public BaseMaterialSetup[] globalBaseMaterials; 

    [Header("Global Paint Job")]
    [Tooltip("Define the custom colors/textures for the player here.")]
    // FIX: Removed "PartMaterialLoader."
    public PlayerPaint[] globalPaintJob; 

    [Header("Physical Parts")]
    public HeadPart headPart;
    public TorsoPart torsoPart;
    public ArmPart armsPart; 
    public LegPart legsPart;
    public Booster boosterPart;
    public Generator generatorPart;
    public FCSPart fcsPart;

    [Header("Weapons")]
    public WeaponPart leftArmWeapon;
    public WeaponPart rightArmWeapon;
    public WeaponPart leftBackWeapon;
    public WeaponPart rightBackWeapon;

    [Header("Debug Controls")]
    public KeyCode reloadKey = KeyCode.F5;
    public bool loadOnStart = true;

    void Start()
    {
        if (loadOnStart) LoadMech();
    }

    void Update()
    {
        if (Input.GetKeyDown(reloadKey)) LoadMech();
    }

    private void LoadMech()
    {
        if (partSystem == null) return;

        partSystem.equippedParts.Clear();

        // Pass BOTH the master materials and the paint job to the Part System!
        partSystem.globalBaseMaterials = globalBaseMaterials;
        partSystem.currentPaintJob = globalPaintJob;

        if (headPart != null) partSystem.equippedParts.Add(PartType.Head, headPart);
        if (torsoPart != null) partSystem.equippedParts.Add(PartType.Torso, torsoPart);
        if (armsPart != null) partSystem.equippedParts.Add(PartType.Arms, armsPart);
        if (legsPart != null) partSystem.equippedParts.Add(PartType.Legs, legsPart);
        if (boosterPart != null) partSystem.equippedParts.Add(PartType.Booster, boosterPart);
        if (generatorPart != null) partSystem.equippedParts.Add(PartType.Generator, generatorPart);
        if (fcsPart != null) partSystem.equippedParts.Add(PartType.FCS, fcsPart);

        if (leftArmWeapon != null) partSystem.equippedParts.Add(PartType.ArmL, leftArmWeapon);
        if (rightArmWeapon != null) partSystem.equippedParts.Add(PartType.ArmR, rightArmWeapon);
        if (leftBackWeapon != null) partSystem.equippedParts.Add(PartType.BackL, leftBackWeapon);
        if (rightBackWeapon != null) partSystem.equippedParts.Add(PartType.BackR, rightBackWeapon);

        partSystem.InitializeMech();
        
        Debug.Log("DEBUG_ManualLoader: Mech dictionary packed, painted, and built.");
    }
}