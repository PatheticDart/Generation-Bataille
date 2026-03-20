using System.Collections.Generic;
using UnityEngine;

// Removed the RequireComponent so it can run naked in the Garage
public class PartSystem : MonoBehaviour
{
    [Header("Current Loadout")]
    [Tooltip("Populate this dictionary from your Garage or Save File before calling InitializeMech()")]
    public Dictionary<PartType, Part> equippedParts = new Dictionary<PartType, Part>();

    [Header("Root Attachment")]
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
    public List<Transform> animBoosterBones;

    [Header("FCS Targeting")]
    public int fcsTargetLayer = 8;
    public Transform targetObject { get; private set; }

    // Internal References
    private WeaponManager _weaponManager; // Now optional!
    private GameObject currentLegs;
    private GameObject currentTorso;
    private GameObject currentHead;
    private GameObject currentLeftArm;
    private GameObject currentRightArm;
    private List<GameObject> currentBoosters = new List<GameObject>();
    private List<GameObject> thrusterEffects = new List<GameObject>();

    private void Awake()
    {
        // Try to get it, but don't panic if it's missing
        _weaponManager = GetComponent<WeaponManager>();
    }

    public void InitializeMech()
    {
        ClearMech();

        // 1. LEGS 
        if (equippedParts.TryGetValue(PartType.Legs, out Part legPart))
        {
            currentLegs = InstantiatePartData(legPart, legsNode);
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
        }
        else return; // Cannot build the rest of the mech without legs

        // 2. TORSO 
        Transform torsoNode = FindDeepChild(currentLegs.transform, "TorsoNode");
        if (torsoNode != null && equippedParts.TryGetValue(PartType.Torso, out Part torsoPart))
        {
            currentTorso = InstantiatePartData(torsoPart, torsoNode);
            if (currentTorso != null)
            {
                SyncPartToBone(currentTorso, animTorsoMainBone);

                Transform foundTarget = FindDeepChild(currentTorso.transform, "TargetObject");
                if (foundTarget != null)
                {
                    targetObject = foundTarget;
                    targetObject.gameObject.layer = fcsTargetLayer;
                }
            }
        }
        if (currentTorso == null) return;

        // 3. HEAD 
        Transform headNode = FindDeepChild(currentTorso.transform, "HeadNode");
        if (headNode != null && equippedParts.TryGetValue(PartType.Head, out Part headPart))
        {
            currentHead = InstantiatePartData(headPart, headNode);
            SyncPartToBone(currentHead, animHeadBone);
        }

        // 4. ARMS (Wrapper Extraction Logic)
        Transform leftArmNode = FindDeepChild(currentTorso.transform, "LeftArmNode");
        Transform rightArmNode = FindDeepChild(currentTorso.transform, "RightArmNode");

        if ((leftArmNode != null || rightArmNode != null) && equippedParts.TryGetValue(PartType.Arms, out Part armsPart))
        {
            if (armsPart is VisiblePart visibleArms && visibleArms.prefab != null)
            {
                GameObject armsWrapper = Instantiate(visibleArms.prefab.gameObject, currentTorso.transform);

                if (armsWrapper != null)
                {
                    Transform extractedLeftArm = null;
                    Transform extractedRightArm = null;

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

                    if (extractedLeftArm == null && armsWrapper.transform.childCount > 0) extractedLeftArm = armsWrapper.transform.GetChild(0);
                    if (extractedRightArm == null && armsWrapper.transform.childCount > 1) extractedRightArm = armsWrapper.transform.GetChild(1);

                    if (extractedLeftArm != null && leftArmNode != null)
                    {
                        extractedLeftArm.SetParent(leftArmNode, false);
                        extractedLeftArm.localPosition = Vector3.zero;
                        extractedLeftArm.localRotation = Quaternion.identity;

                        currentLeftArm = extractedLeftArm.gameObject;
                        SyncPartToBone(currentLeftArm, animLeftArmBone);
                        SyncChildBone(currentLeftArm, "Lower Arm Bone Left", animLowerArmBoneLeft);

                        if (currentLeftArm.TryGetComponent<PartTemplate>(out PartTemplate lTemp)) lTemp.SpawnPart();
                    }

                    if (extractedRightArm != null && rightArmNode != null)
                    {
                        extractedRightArm.SetParent(rightArmNode, false);
                        extractedRightArm.localPosition = Vector3.zero;
                        extractedRightArm.localRotation = Quaternion.identity;

                        currentRightArm = extractedRightArm.gameObject;
                        SyncPartToBone(currentRightArm, animRightArmBone);
                        SyncChildBone(currentRightArm, "Lower Arm Bone Right", animLowerArmBoneRight);

                        if (currentRightArm.TryGetComponent<PartTemplate>(out PartTemplate rTemp)) rTemp.SpawnPart();
                    }

                    DestroyImmediate(armsWrapper);
                }
            }
        }

        // 5. BOOSTERS & THRUSTERS
        if (equippedParts.TryGetValue(PartType.Booster, out Part boosterPart))
        {
            List<Transform> boosterNodes = new List<Transform>();
            FindAllDeepChildren(currentTorso.transform, "BoosterNode", boosterNodes);

            int currentBoosterCount = 0;
            foreach (Transform bNode in boosterNodes)
            {
                GameObject booster = InstantiatePartData(boosterPart, bNode);
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
        }

        // 6. WEAPONS (Spawn & Register with Manager)
        Transform leftBackWepNode = FindDeepChild(currentTorso.transform, "LeftBackWeaponNode");
        if (leftBackWepNode != null && equippedParts.TryGetValue(PartType.BackL, out Part lBackPart))
        {
            GameObject wepObj = InstantiateWeaponData(lBackPart, leftBackWepNode, true);
            RegisterIfWeapon(PartType.BackL, wepObj, lBackPart);
        }

        Transform rightBackWepNode = FindDeepChild(currentTorso.transform, "RightBackWeaponNode");
        if (rightBackWepNode != null && equippedParts.TryGetValue(PartType.BackR, out Part rBackPart))
        {
            GameObject wepObj = InstantiateWeaponData(rBackPart, rightBackWepNode, false);
            RegisterIfWeapon(PartType.BackR, wepObj, rBackPart);
        }

        if (currentLeftArm != null && equippedParts.TryGetValue(PartType.ArmL, out Part lArmWepPart))
        {
            Transform lArmWepNode = FindDeepChild(currentLeftArm.transform, "LeftArmWeaponNode");
            if (lArmWepNode != null)
            {
                GameObject wepObj = InstantiateWeaponData(lArmWepPart, lArmWepNode, true);
                RegisterIfWeapon(PartType.ArmL, wepObj, lArmWepPart);
            }
        }

        if (currentRightArm != null && equippedParts.TryGetValue(PartType.ArmR, out Part rArmWepPart))
        {
            Transform rArmWepNode = FindDeepChild(currentRightArm.transform, "RightArmWeaponNode");
            if (rArmWepNode != null)
            {
                GameObject wepObj = InstantiateWeaponData(rArmWepPart, rArmWepNode, false);
                RegisterIfWeapon(PartType.ArmR, wepObj, rArmWepPart);
            }
        }
    }

    private GameObject InstantiatePartData(Part partData, Transform parent)
    {
        if (partData == null) return null;

        if (partData is VisiblePart visiblePart && visiblePart.prefab != null)
        {
            GameObject obj = Instantiate(visiblePart.prefab, parent);
            obj.transform.localPosition = Vector3.zero;
            obj.transform.localRotation = Quaternion.identity;

            if (obj.TryGetComponent<PartTemplate>(out PartTemplate temp)) temp.SpawnPart();

            return obj;
        }

        return null;
    }

    private GameObject InstantiateWeaponData(Part partData, Transform parent, bool isLeftVariant)
    {
        if (partData == null) return null;

        if (partData is VisiblePart visiblePart && visiblePart.prefab != null)
        {
            GameObject wrapper = Instantiate(visiblePart.prefab, parent);
            Transform extractedWeapon = null;

            foreach (Transform child in wrapper.transform)
            {
                string lowerName = child.name.ToLower();
                if (isLeftVariant && (lowerName.Contains("left") || lowerName.Contains("_l") || lowerName.Contains("l_")))
                {
                    extractedWeapon = child;
                    break;
                }
                else if (!isLeftVariant && (lowerName.Contains("right") || lowerName.Contains("_r") || lowerName.Contains("r_")))
                {
                    extractedWeapon = child;
                    break;
                }
            }

            if (extractedWeapon != null)
            {
                extractedWeapon.SetParent(parent, false);
                extractedWeapon.localPosition = Vector3.zero;
                extractedWeapon.localRotation = Quaternion.identity;

                DestroyImmediate(wrapper);

                GameObject finalObj = extractedWeapon.gameObject;

                if (finalObj.TryGetComponent<PartTemplate>(out PartTemplate temp)) temp.SpawnPart();
                return finalObj;
            }
            else
            {
                wrapper.transform.localPosition = Vector3.zero;
                wrapper.transform.localRotation = Quaternion.identity;

                if (wrapper.TryGetComponent<PartTemplate>(out PartTemplate temp)) temp.SpawnPart();
                return wrapper;
            }
        }
        return null;
    }

    private void RegisterIfWeapon(PartType slot, GameObject spawnedObj, Part partData)
    {
        if (spawnedObj == null) return;

        if (spawnedObj.TryGetComponent(out FunctionalWeapon weapon))
        {
            // Always initialize the weapon (sets ammo, limits, etc.) so it exists for Garage stats
            weapon.InitializeWeapon(partData);

            // Only hook up to the firing systems if they actually exist in this scene!
            if (_weaponManager != null)
            {
                switch (slot)
                {
                    case PartType.ArmL: _weaponManager.RegisterWeapon(true, 0, weapon); break;
                    case PartType.BackL: _weaponManager.RegisterWeapon(true, 1, weapon); break;
                    case PartType.ArmR: _weaponManager.RegisterWeapon(false, 0, weapon); break;
                    case PartType.BackR: _weaponManager.RegisterWeapon(false, 1, weapon); break;
                }
            }
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