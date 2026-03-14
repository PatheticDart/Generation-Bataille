using UnityEngine;

public class DEBUG_ManualLoader : MonoBehaviour
{
    [Header("System Reference")]
    public PartSystem partSystem;

    [Header("Physical Parts")]
    public Part headPart;
    public Part torsoPart;
    public Part armsPart; 
    public Part legsPart;
    public Part boosterPart;
    public Part generatorPart;
    public Part fcsPart;

    [Header("Weapons")]
    public WeaponPart leftArmWeapon;
    public WeaponPart rightArmWeapon;
    public WeaponPart leftBackWeapon;
    public WeaponPart rightBackWeapon;

    [Header("Debug Controls")]
    public KeyCode reloadKey = KeyCode.F5;
    public bool loadOnStart = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (loadOnStart)
        {
            LoadMech();
        }
    }

    // Update is called once per frame
    void Update()
    {
        // Rebuilds the mech on the fly if you press the hotkey
        if (Input.GetKeyDown(reloadKey))
        {
            LoadMech();
        }
    }

    private void LoadMech()
    {
        if (partSystem == null)
        {
            Debug.LogError("DEBUG_ManualLoader: PartSystem reference is missing!");
            return;
        }

        // Clear the dictionary to ensure a clean slate
        partSystem.equippedParts.Clear();

        // Assign physical parts using your PartType enum
        if (headPart != null) partSystem.equippedParts.Add(PartType.Head, headPart);
        if (torsoPart != null) partSystem.equippedParts.Add(PartType.Torso, torsoPart);
        if (armsPart != null) partSystem.equippedParts.Add(PartType.Arms, armsPart);
        if (legsPart != null) partSystem.equippedParts.Add(PartType.Legs, legsPart);
        if (boosterPart != null) partSystem.equippedParts.Add(PartType.Booster, boosterPart);
        if (generatorPart != null) partSystem.equippedParts.Add(PartType.Generator, generatorPart);
        if (fcsPart != null) partSystem.equippedParts.Add(PartType.FCS, fcsPart);

        // Assign weapons using your PartType enum
        if (leftArmWeapon != null) partSystem.equippedParts.Add(PartType.ArmL, leftArmWeapon);
        if (rightArmWeapon != null) partSystem.equippedParts.Add(PartType.ArmR, rightArmWeapon);
        if (leftBackWeapon != null) partSystem.equippedParts.Add(PartType.BackL, leftBackWeapon);
        if (rightBackWeapon != null) partSystem.equippedParts.Add(PartType.BackR, rightBackWeapon);

        // Tell the PartSystem to spawn everything
        partSystem.InitializeMech();
        
        Debug.Log("DEBUG_ManualLoader: Mech dictionary packed and built.");
    }
}