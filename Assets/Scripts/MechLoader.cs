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

    [Header("Animation Skeleton References - Core")]
    public Transform animMasterBone;
    public Transform animTorsoMainBone;
    public Transform animHeadBone;

    [Header("Animation Skeleton References - Arms")]
    public Transform animLeftArmBone;
    public Transform animLowerArmBoneLeft;
    public Transform animRightArmBone;
    public Transform animLowerArmBoneRight;

    [Header("Animation Skeleton References - Legs")]
    public Transform animLegMainBoneLeft;
    public Transform animLowerLegBoneLeft;
    public Transform animFootBoneLeft;
    public Transform animLegMainBoneRight;
    public Transform animLowerLegBoneRight;
    public Transform animFootBoneRight;

    [Header("Animation Skeleton References - Boosters")]
    [Tooltip("Add your booster bones here. Spawned boosters will sync to these in order.")]
    public List<Transform> animBoosterBones;

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

    [Header("FCS Targeting")]
    public int fcsTargetLayer = 8;
    public Transform targetObject { get; private set; }

    // Internal References
    private GameObject currentLegs;
    private GameObject currentTorso;
    private GameObject currentHead;
    private GameObject currentLeftArm;
    private GameObject currentRightArm;
    private List<GameObject> currentBoosters = new List<GameObject>();

    void Start()
    {
        if (loadOnStart) BuildMech();
    }

    public void BuildMech()
    {
        ClearMech();

        // 1. LEGS 
        currentLegs = InstantiatePart(legParts, legIndex, legsNode);
        if (currentLegs != null)
        {
            // NEW: Pass 'true' to force the visual root to follow the skeleton's position!
            SyncPartToBone(currentLegs, animMasterBone, true);

            // Sub-bones only copy rotation
            SyncChildBone(currentLegs, "Leg Main Bone Left", animLegMainBoneLeft);
            SyncChildBone(currentLegs, "Lower Leg Bone Left", animLowerLegBoneLeft);
            SyncChildBone(currentLegs, "Foot Bone Left", animFootBoneLeft);

            SyncChildBone(currentLegs, "Leg Main Bone Right", animLegMainBoneRight);
            SyncChildBone(currentLegs, "Lower Leg Bone Right", animLowerLegBoneRight);
            SyncChildBone(currentLegs, "Foot Bone Right", animFootBoneRight);
        }
        else return;

        // 2. TORSO 
        Transform torsoNode = FindDeepChild(currentLegs.transform, "TorsoNode");
        if (torsoNode != null)
        {
            currentTorso = InstantiatePart(torsoParts, torsoIndex, torsoNode);
            SyncPartToBone(currentTorso, animTorsoMainBone);

            Transform foundTarget = FindDeepChild(currentTorso.transform, "TargetObject");
            if (foundTarget != null)
            {
                targetObject = foundTarget;
                targetObject.gameObject.layer = fcsTargetLayer;
            }
        }

        if (currentTorso == null) return;

        // 3. HEAD & ARMS
        Transform headNode = FindDeepChild(currentTorso.transform, "HeadNode");
        if (headNode != null)
        {
            currentHead = InstantiatePart(headParts, headIndex, headNode);
            SyncPartToBone(currentHead, animHeadBone);
        }

        Transform leftArmNode = FindDeepChild(currentTorso.transform, "LeftArmNode");
        if (leftArmNode != null)
        {
            currentLeftArm = InstantiatePart(leftArmParts, armIndex, leftArmNode);
            SyncPartToBone(currentLeftArm, animLeftArmBone);
            SyncChildBone(currentLeftArm, "Lower Arm Bone Left", animLowerArmBoneLeft);
        }

        Transform rightArmNode = FindDeepChild(currentTorso.transform, "RightArmNode");
        if (rightArmNode != null)
        {
            currentRightArm = InstantiatePart(rightArmParts, armIndex, rightArmNode);
            SyncPartToBone(currentRightArm, animRightArmBone);
            SyncChildBone(currentRightArm, "Lower Arm Bone Right", animLowerArmBoneRight);
        }

        // 4. BOOSTERS 
        List<Transform> boosterNodes = new List<Transform>();
        FindAllDeepChildren(currentTorso.transform, "BoosterNode", boosterNodes);

        int currentBoosterCount = 0;
        foreach (Transform bNode in boosterNodes)
        {
            GameObject booster = InstantiatePart(boosterParts, boosterIndex, bNode);
            if (booster != null)
            {
                currentBoosters.Add(booster);

                if (animBoosterBones != null && currentBoosterCount < animBoosterBones.Count)
                {
                    SyncPartToBone(booster, animBoosterBones[currentBoosterCount]);
                }

                currentBoosterCount++;
            }
        }

        // Back Weapons
        Transform leftBackWepNode = FindDeepChild(currentTorso.transform, "LeftBackWeaponNode");
        if (leftBackWepNode != null) InstantiatePart(leftBackWeapons, lBackWepIndex, leftBackWepNode);

        Transform rightBackWepNode = FindDeepChild(currentTorso.transform, "RightBackWeaponNode");
        if (rightBackWepNode != null) InstantiatePart(rightBackWeapons, rBackWepIndex, rightBackWepNode);

        // Arm Weapons
        if (currentLeftArm != null)
        {
            Transform lArmWepNode = FindDeepChild(currentLeftArm.transform, "LeftArmWeaponNode");
            if (lArmWepNode != null) InstantiatePart(leftArmWeapons, lArmWepIndex, lArmWepNode);
        }

        if (currentRightArm != null)
        {
            Transform rArmWepNode = FindDeepChild(currentRightArm.transform, "RightArmWeaponNode");
            if (rArmWepNode != null) InstantiatePart(rightArmWeapons, rArmWepIndex, rArmWepNode);
        }
    }

    // --- HELPER FUNCTIONS ---
    private void SyncChildBone(GameObject parentPart, string childName, Transform targetAnimBone, bool syncPos = false)
    {
        if (parentPart == null || targetAnimBone == null) return;

        Transform foundChild = FindDeepChild(parentPart.transform, childName);
        if (foundChild != null)
        {
            SyncPartToBone(foundChild.gameObject, targetAnimBone, syncPos);
        }
    }

    private void SyncPartToBone(GameObject spawnedPart, Transform targetAnimBone, bool syncPos = false)
    {
        if (spawnedPart == null || targetAnimBone == null) return;

        PartSync sync = spawnedPart.AddComponent<PartSync>();
        sync.targetBone = targetAnimBone;
        sync.syncPosition = syncPos;
    }

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
        if (currentLeftArm) DestroyImmediate(currentLeftArm);
        if (currentRightArm) DestroyImmediate(currentRightArm);
        foreach (GameObject booster in currentBoosters) { if (booster) DestroyImmediate(booster); }
        currentBoosters.Clear();
        if (currentTorso) DestroyImmediate(currentTorso);
        if (currentLegs) DestroyImmediate(currentLegs);
    }

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

    public static void FindAllDeepChildren(Transform parent, string exactName, List<Transform> results)
    {
        foreach (Transform child in parent)
        {
            if (child.name == exactName) results.Add(child);
            FindAllDeepChildren(child, exactName, results);
        }
    }
}