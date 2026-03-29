using System.Collections.Generic;
using UnityEngine;

public class PlayerInventoryManager : MonoBehaviour
{
    public static PlayerInventoryManager Instance { get; private set; }

    [Header("Currency")]
    public int currentCredits = 5000; // Starting money for testing

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

    private void Awake()
    {
        // Simple Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // Keeps inventory across scenes
    }

    // --- Currency Logic ---
    public bool HasEnoughCredits(int amount) => currentCredits >= amount;

    public void AddCredits(int amount) => currentCredits += amount;

    public bool SpendCredits(int amount)
    {
        if (HasEnoughCredits(amount))
        {
            currentCredits -= amount;
            return true;
        }
        return false;
    }

    // --- Inventory Logic ---
    public bool IsPartOwned(Part part)
    {
        if (part == null) return false;

        // Route the check to the correct specific list
        if (part is HeadPart head) return ownedHeads.Contains(head);
        if (part is TorsoPart torso) return ownedTorsos.Contains(torso);
        if (part is ArmPart arm) return ownedArms.Contains(arm);
        if (part is LegPart leg) return ownedLegs.Contains(leg);
        if (part is Booster booster) return ownedBoosters.Contains(booster);
        if (part is Generator gen) return ownedGenerators.Contains(gen);
        if (part is FCSPart fcs) return ownedFCS.Contains(fcs);
        if (part is WeaponPart weapon) return ownedWeapons.Contains(weapon);

        return false;
    }

    public void UnlockPart(Part part)
    {
        if (part == null || IsPartOwned(part)) return;

        // Route the unlock to the correct specific list
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

        // Route the removal from the correct specific list
        if (part is HeadPart head) ownedHeads.Remove(head);
        else if (part is TorsoPart torso) ownedTorsos.Remove(torso);
        else if (part is ArmPart arm) ownedArms.Remove(arm);
        else if (part is LegPart leg) ownedLegs.Remove(leg);
        else if (part is Booster booster) ownedBoosters.Remove(booster);
        else if (part is Generator gen) ownedGenerators.Remove(gen);
        else if (part is FCSPart fcs) ownedFCS.Remove(fcs);
        else if (part is WeaponPart weapon) ownedWeapons.Remove(weapon);
    }
}