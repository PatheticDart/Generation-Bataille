using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MechStats))]
public class MechLoader : MonoBehaviour
{
    [Header("Physical Part Prefabs")]
    public List<GameObject> legParts;
    public List<GameObject> torsoParts;
    public List<GameObject> headParts;
    public List<GameObject> armParts; // NEW: Single list for the combined Arms prefab
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
    public int armIndex = 0;
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
    private List<GameObject> thrusterEffects = new List<GameObject>();

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
            SyncPartToBone(currentLegs, animMasterBone, true);

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

        // 3. HEAD 
        Transform headNode = FindDeepChild(currentTorso.transform, "HeadNode");
        if (headNode != null)
        {
            currentHead = InstantiatePart(headParts, headIndex, headNode);
            SyncPartToBone(currentHead, animHeadBone);
        }

        // 4. ARMS (Wrapper Extraction Logic)
        Transform leftArmNode = FindDeepChild(currentTorso.transform, "LeftArmNode");
        Transform rightArmNode = FindDeepChild(currentTorso.transform, "RightArmNode");

        if (leftArmNode != null || rightArmNode != null)
        {
            // Spawn the combined arms wrapper parent temporarily on the torso
            GameObject armsWrapper = InstantiatePart(armParts, armIndex, currentTorso.transform);

            if (armsWrapper != null)
            {
                Transform extractedLeftArm = null;
                Transform extractedRightArm = null;

                // Identify which child is the left arm and which is the right arm by checking the name
                foreach (Transform child in armsWrapper.transform)
                {
                    string lowerName = child.name.ToLower();
                    if (lowerName.Contains("left") || lowerName.Contains("_l") || lowerName.Contains("l_"))
                    {
                        extractedLeftArm = child;
                    }
                    else if (lowerName.Contains("right") || lowerName.Contains("_r") || lowerName.Contains("r_"))
                    {
                        extractedRightArm = child;
                    }
                }

                // Fallback: If names don't match, just grab the first and second children
                if (extractedLeftArm == null && armsWrapper.transform.childCount > 0) extractedLeftArm = armsWrapper.transform.GetChild(0);
                if (extractedRightArm == null && armsWrapper.transform.childCount > 1) extractedRightArm = armsWrapper.transform.GetChild(1);

                // Slot and sync the Left Arm
                if (extractedLeftArm != null && leftArmNode != null)
                {
                    extractedLeftArm.SetParent(leftArmNode, false);
                    extractedLeftArm.localPosition = Vector3.zero;
                    extractedLeftArm.localRotation = Quaternion.identity;

                    currentLeftArm = extractedLeftArm.gameObject;
                    SyncPartToBone(currentLeftArm, animLeftArmBone);
                    SyncChildBone(currentLeftArm, "Lower Arm Bone Left", animLowerArmBoneLeft);
                }

                // Slot and sync the Right Arm
                if (extractedRightArm != null && rightArmNode != null)
                {
                    extractedRightArm.SetParent(rightArmNode, false);
                    extractedRightArm.localPosition = Vector3.zero;
                    extractedRightArm.localRotation = Quaternion.identity;

                    currentRightArm = extractedRightArm.gameObject;
                    SyncPartToBone(currentRightArm, animRightArmBone);
                    SyncChildBone(currentRightArm, "Lower Arm Bone Right", animLowerArmBoneRight);
                }

                // Destroy the empty wrapper parent now that the arms have been extracted
                DestroyImmediate(armsWrapper);
            }
        }

        // 5. BOOSTERS & THRUSTERS
        List<Transform> boosterNodes = new List<Transform>();
        FindAllDeepChildren(currentTorso.transform, "BoosterNode", boosterNodes);

        int currentBoosterCount = 0;
        foreach (Transform bNode in boosterNodes)
        {
            GameObject booster = InstantiatePart(boosterParts, boosterIndex, bNode);
            if (booster != null)
            {
                currentBoosters.Add(booster);

                Transform thrusterNode = FindDeepChild(booster.transform, "Thruster");
                if (thrusterNode != null)
                {
                    thrusterNode.gameObject.SetActive(false);
                    thrusterEffects.Add(thrusterNode.gameObject);
                }

                if (animBoosterBones != null && currentBoosterCount < animBoosterBones.Count)
                {
                    SyncPartToBone(booster, animBoosterBones[currentBoosterCount]);
                }

                currentBoosterCount++;
            }
        }

        // 6. WEAPONS
        Transform leftBackWepNode = FindDeepChild(currentTorso.transform, "LeftBackWeaponNode");
        if (leftBackWepNode != null) InstantiatePart(leftBackWeapons, lBackWepIndex, leftBackWepNode);

        Transform rightBackWepNode = FindDeepChild(currentTorso.transform, "RightBackWeaponNode");
        if (rightBackWepNode != null) InstantiatePart(rightBackWeapons, rBackWepIndex, rightBackWepNode);

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

    public void ToggleThrusters(bool isActive)
    {
        foreach (GameObject thruster in thrusterEffects)
        {
            if (thruster != null && thruster.activeSelf != isActive)
            {
                thruster.SetActive(isActive);
            }
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
        thrusterEffects.Clear();
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