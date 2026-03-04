using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MechStats))]
public class MechLoader : MonoBehaviour
{
    [Header("Physical Part Prefabs")]
    public List<GameObject> legParts;
    public List<GameObject> torsoParts;
    public List<GameObject> headParts;
    public List<GameObject> leftArmParts;
    public List<GameObject> rightArmParts;
    public List<GameObject> boosterParts;

    [Header("Weapon Prefabs")]
    public List<GameObject> leftArmWeapons;
    public List<GameObject> rightArmWeapons;
    public List<GameObject> leftBackWeapons;
    public List<GameObject> rightBackWeapons;

    [Header("Internal Parts (Stats Only)")]
    public List<GameObject> fcsParts;
    public List<GameObject> generatorParts;

    [Header("Root Attachment")]
    [Tooltip("The base node on the Mech GameObject where the Legs will be spawned.")]
    public Transform legsNode;

    [Header("Current Loadout Indices")]
    public bool loadOnStart = true;
    public int legIndex = 0;
    public int torsoIndex = 0;
    public int headIndex = 0;
    public int armIndex = 0; // Controls both Left and Right arms
    public int boosterIndex = 0;
    public int lArmWepIndex = 0;
    public int rArmWepIndex = 0;
    public int lBackWepIndex = 0;
    public int rBackWepIndex = 0;
    public int fcsIndex = 0;
    public int generatorIndex = 0;

    [Header("FCS Targeting")]
    [Tooltip("The layer your FCSLockBox is looking for (e.g., 'Targetable').")]
    public int fcsTargetLayer = 8;
    public Transform targetObject { get; private set; }

    // --- Internal References to Instantiated Parts ---
    private GameObject currentLegs;
    private GameObject currentTorso;
    private GameObject currentHead;
    private GameObject currentLeftArm;
    private GameObject currentRightArm;
    private List<GameObject> currentBoosters = new List<GameObject>();
    private GameObject currentLArmWep;
    private GameObject currentRArmWep;
    private GameObject currentLBackWep;
    private GameObject currentRBackWep;

    private MechStats mechStats;

    void Start()
    {
        mechStats = GetComponent<MechStats>();

        if (loadOnStart)
        {
            BuildMech();
        }
    }

    public void BuildMech()
    {
        ClearMech();

        // ==========================================
        // 1. LEGS (The Foundation)
        // ==========================================
        currentLegs = InstantiatePart(legParts, legIndex, legsNode);
        if (currentLegs == null) return;

        // ==========================================
        // 2. TORSO
        // ==========================================
        Transform torsoNode = FindDeepChild(currentLegs.transform, "TorsoNode");
        if (torsoNode != null)
        {
            currentTorso = InstantiatePart(torsoParts, torsoIndex, torsoNode);

            // Register the TargetObject for the FCS
            Transform foundTarget = FindDeepChild(currentTorso.transform, "TargetObject");
            if (foundTarget != null)
            {
                targetObject = foundTarget;
                targetObject.gameObject.layer = fcsTargetLayer; // Ensure it can be locked onto
            }
            else
            {
                Debug.LogWarning("No 'TargetObject' found on the Torso prefab! FCS will not be able to lock onto this mech.");
            }
        }
        else { Debug.LogError("Legs prefab is missing 'TorsoNode'!"); return; }

        if (currentTorso == null) return;

        // ==========================================
        // 3. HEAD, ARMS, BOOSTERS & BACK WEAPONS
        // ==========================================

        // Head
        Transform headNode = FindDeepChild(currentTorso.transform, "HeadNode");
        if (headNode != null) currentHead = InstantiatePart(headParts, headIndex, headNode);

        // Arms
        Transform leftArmNode = FindDeepChild(currentTorso.transform, "LeftArmNode");
        if (leftArmNode != null) currentLeftArm = InstantiatePart(leftArmParts, armIndex, leftArmNode);

        Transform rightArmNode = FindDeepChild(currentTorso.transform, "RightArmNode");
        if (rightArmNode != null) currentRightArm = InstantiatePart(rightArmParts, armIndex, rightArmNode);

        // Boosters (Multiple Support)
        List<Transform> boosterNodes = new List<Transform>();
        FindAllDeepChildren(currentTorso.transform, "BoosterNode", boosterNodes);
        foreach (Transform bNode in boosterNodes)
        {
            GameObject booster = InstantiatePart(boosterParts, boosterIndex, bNode);
            if (booster != null) currentBoosters.Add(booster);
        }

        // Back Weapons
        Transform leftBackWepNode = FindDeepChild(currentTorso.transform, "LeftBackWeaponNode");
        if (leftBackWepNode != null) currentLBackWep = InstantiatePart(leftBackWeapons, lBackWepIndex, leftBackWepNode);

        Transform rightBackWepNode = FindDeepChild(currentTorso.transform, "RightBackWeaponNode");
        if (rightBackWepNode != null) currentRBackWep = InstantiatePart(rightBackWeapons, rBackWepIndex, rightBackWepNode);

        // ==========================================
        // 4. ARM WEAPONS
        // ==========================================
        if (currentLeftArm != null)
        {
            Transform lArmWepNode = FindDeepChild(currentLeftArm.transform, "LeftArmWeaponNode");
            if (lArmWepNode != null) currentLArmWep = InstantiatePart(leftArmWeapons, lArmWepIndex, lArmWepNode);
        }

        if (currentRightArm != null)
        {
            Transform rArmWepNode = FindDeepChild(currentRightArm.transform, "RightArmWeaponNode");
            if (rArmWepNode != null) currentRArmWep = InstantiatePart(rightArmWeapons, rArmWepIndex, rArmWepNode);
        }

        // ==========================================
        // 5. INTERNAL PARTS (Stats Only)
        // ==========================================
        ApplyInternalPartStats();
    }

    private void ApplyInternalPartStats()
    {
        // TODO: Extract stats from Generator and FCS prefabs and apply them to MechStats.cs
        // Example:
        // if (fcsParts.Count > fcsIndex && fcsParts[fcsIndex] != null) {
        //      FCSData data = fcsParts[fcsIndex].GetComponent<FCSData>();
        //      // push data to FCSLockBox or MechStats
        // }

        Debug.Log("Mech built successfully. Internal stats logic ready to be hooked up.");
    }

    // --- HELPER FUNCTIONS ---

    private GameObject InstantiatePart(List<GameObject> list, int index, Transform parent)
    {
        if (list == null || list.Count == 0 || index < 0 || index >= list.Count || list[index] == null)
            return null;

        GameObject obj = Instantiate(list[index], parent);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = Quaternion.identity;
        return obj;
    }

    private void ClearMech()
    {
        if (currentHead) DestroyImmediate(currentHead);
        if (currentLArmWep) DestroyImmediate(currentLArmWep);
        if (currentRArmWep) DestroyImmediate(currentRArmWep);
        if (currentLBackWep) DestroyImmediate(currentLBackWep);
        if (currentRBackWep) DestroyImmediate(currentRBackWep);
        if (currentLeftArm) DestroyImmediate(currentLeftArm);
        if (currentRightArm) DestroyImmediate(currentRightArm);

        foreach (GameObject booster in currentBoosters)
        {
            if (booster) DestroyImmediate(booster);
        }
        currentBoosters.Clear();

        if (currentTorso) DestroyImmediate(currentTorso);
        if (currentLegs) DestroyImmediate(currentLegs);

        targetObject = null;
    }

    // Recursive search to find nodes buried deep in armatures/hierarchies
    public static Transform FindDeepChild(Transform parent, string exactName)
    {
        foreach (Transform child in parent)
        {
            if (child.name == exactName) return child;

            Transform result = FindDeepChild(child, exactName);
            if (result != null) return result;
        }
        return null;
    }

    // Recursive search to find ALL nodes with a specific name (used for Boosters)
    public static void FindAllDeepChildren(Transform parent, string exactName, List<Transform> results)
    {
        foreach (Transform child in parent)
        {
            if (child.name == exactName) results.Add(child);
            FindAllDeepChildren(child, exactName, results);
        }
    }
}