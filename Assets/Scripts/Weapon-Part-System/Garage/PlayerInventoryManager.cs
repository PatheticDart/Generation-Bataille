using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement; // --- NEW: Required to restart the Garage! ---

public class PlayerInventoryManager : MonoBehaviour
{
    public static PlayerInventoryManager Instance { get; private set; }

    [Header("Currency")]
    public int currentCredits = 5000;

    [Header("Owned Parts (Explicit)")]
    public List<HeadPart> ownedHeads = new List<HeadPart>();
    public List<TorsoPart> ownedTorsos = new List<TorsoPart>();
    public List<ArmPart> ownedArms = new List<ArmPart>();
    public List<LegPart> ownedLegs = new List<LegPart>();
    public List<Booster> ownedBoosters = new List<Booster>();
    public List<Generator> ownedGenerators = new List<Generator>();
    public List<FCSPart> ownedFCS = new List<FCSPart>();

    [Header("Owned Parts (Combined Weapons)")]
    public List<WeaponPart> ownedWeapons = new List<WeaponPart>();

    private bool _hasLoadedLoadoutFromDisk = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        LoadInventory();
    }

    private void OnApplicationQuit()
    {
        SaveInventory();
    }

    // --- THE FIX: A true nuclear option that updates the UI immediately ---
    [ContextMenu("Hard Reset Progress")]
    public void HardResetProgress()
    {
        // 1. Wipe active memory FIRST
        currentCredits = 5000;

        ownedHeads.Clear(); ownedTorsos.Clear(); ownedArms.Clear();
        ownedLegs.Clear(); ownedBoosters.Clear(); ownedGenerators.Clear();
        ownedFCS.Clear(); ownedWeapons.Clear();

        if (GarageLoader.ActiveLoadout != null) GarageLoader.ActiveLoadout.Clear();

        _hasLoadedLoadoutFromDisk = false;

        // 2. Overwrite the hard drive with this blank state
        SaveInventory();

        Debug.Log("Player Progress wiped! Reloading scene to apply changes...");

        // 3. Force a scene reload so the UI updates and starter parts are regranted
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SaveInventory()
    {
        PlayerPrefs.SetInt("PlayerCredits", currentCredits);

        List<string> ownedPartNames = new List<string>();
        ownedPartNames.AddRange(ownedHeads.Select(p => p.name));
        ownedPartNames.AddRange(ownedTorsos.Select(p => p.name));
        ownedPartNames.AddRange(ownedArms.Select(p => p.name));
        ownedPartNames.AddRange(ownedLegs.Select(p => p.name));
        ownedPartNames.AddRange(ownedBoosters.Select(p => p.name));
        ownedPartNames.AddRange(ownedGenerators.Select(p => p.name));
        ownedPartNames.AddRange(ownedFCS.Select(p => p.name));
        ownedPartNames.AddRange(ownedWeapons.Select(p => p.name));

        PlayerPrefs.SetString("OwnedPartsData", string.Join("|", ownedPartNames));

        List<string> equippedParts = new List<string>();
        foreach (var kvp in GarageLoader.ActiveLoadout)
        {
            if (kvp.Value != null)
            {
                equippedParts.Add($"{kvp.Key.ToString()}:{kvp.Value.name}");
            }
        }
        PlayerPrefs.SetString("ActiveLoadoutData", string.Join("|", equippedParts));

        PlayerPrefs.Save();
        Debug.Log("Inventory & Loadout saved to disk!");
    }

    public void LoadInventory()
    {
        currentCredits = PlayerPrefs.GetInt("PlayerCredits", 5000);
    }

    public void ForceSyncFromDiskAndCatalog(List<Part> masterCatalog)
    {
        string joinedNames = PlayerPrefs.GetString("OwnedPartsData", "");

        ownedHeads.Clear(); ownedTorsos.Clear(); ownedArms.Clear();
        ownedLegs.Clear(); ownedBoosters.Clear(); ownedGenerators.Clear();
        ownedFCS.Clear(); ownedWeapons.Clear();

        if (!string.IsNullOrEmpty(joinedNames))
        {
            string[] savedNames = joinedNames.Split('|');
            foreach (Part catalogPart in masterCatalog)
            {
                if (savedNames.Contains(catalogPart.name)) UnlockPart(catalogPart);
            }
        }

        if (!_hasLoadedLoadoutFromDisk)
        {
            GarageLoader.ActiveLoadout.Clear();
            string loadoutData = PlayerPrefs.GetString("ActiveLoadoutData", "");

            if (!string.IsNullOrEmpty(loadoutData))
            {
                string[] pairs = loadoutData.Split('|');
                foreach (string pair in pairs)
                {
                    string[] split = pair.Split(':');
                    if (split.Length == 2)
                    {
                        if (System.Enum.TryParse(split[0], out PartType pType))
                        {
                            Part equippedPart = masterCatalog.FirstOrDefault(p => p.name == split[1]);
                            if (equippedPart != null && IsPartOwned(equippedPart))
                            {
                                GarageLoader.ActiveLoadout[pType] = equippedPart;
                            }
                        }
                    }
                }
            }
            _hasLoadedLoadoutFromDisk = true;
        }
    }

    public bool HasEnoughCredits(int amount) => currentCredits >= amount;
    public void AddCredits(int amount) => currentCredits += amount;
    public bool SpendCredits(int amount)
    {
        if (HasEnoughCredits(amount)) { currentCredits -= amount; return true; }
        return false;
    }

    public bool IsPartOwned(Part part)
    {
        if (part == null) return false;
        if (part is HeadPart) return ownedHeads.Exists(p => p.name == part.name);
        if (part is TorsoPart) return ownedTorsos.Exists(p => p.name == part.name);
        if (part is ArmPart) return ownedArms.Exists(p => p.name == part.name);
        if (part is LegPart) return ownedLegs.Exists(p => p.name == part.name);
        if (part is Booster) return ownedBoosters.Exists(p => p.name == part.name);
        if (part is Generator) return ownedGenerators.Exists(p => p.name == part.name);
        if (part is FCSPart) return ownedFCS.Exists(p => p.name == part.name);
        if (part is WeaponPart) return ownedWeapons.Exists(p => p.name == part.name);
        return false;
    }

    public void UnlockPart(Part part)
    {
        if (part == null || IsPartOwned(part)) return;
        if (part is HeadPart head) ownedHeads.Add(head);
        else if (part is TorsoPart torso) ownedTorsos.Add(torso);
        else if (part is ArmPart arm) ownedArms.Add(arm);
        else if (part is LegPart leg) ownedLegs.Add(leg);
        else if (part is Booster booster) ownedBoosters.Add(booster);
        else if (part is Generator gen) ownedGenerators.Add(gen);
        else if (part is FCSPart fcs) ownedFCS.Add(fcs);
        else if (part is WeaponPart weapon) ownedWeapons.Add(weapon);
    }

    public void RemovePart(Part part)
    {
        if (part == null || !IsPartOwned(part)) return;
        if (part is HeadPart head) ownedHeads.RemoveAll(p => p.name == head.name);
        else if (part is TorsoPart torso) ownedTorsos.RemoveAll(p => p.name == torso.name);
        else if (part is ArmPart arm) ownedArms.RemoveAll(p => p.name == arm.name);
        else if (part is LegPart leg) ownedLegs.RemoveAll(p => p.name == leg.name);
        else if (part is Booster booster) ownedBoosters.RemoveAll(p => p.name == booster.name);
        else if (part is Generator gen) ownedGenerators.RemoveAll(p => p.name == gen.name);
        else if (part is FCSPart fcs) ownedFCS.RemoveAll(p => p.name == fcs.name);
        else if (part is WeaponPart weapon) ownedWeapons.RemoveAll(p => p.name == weapon.name);
    }
}