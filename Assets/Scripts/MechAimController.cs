using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(MechWeaponManager))]
[DefaultExecutionOrder(200)]
public class MechAimController : MonoBehaviour
{
    private MechWeaponManager weaponManager;

    [Header("Targeting References")]
    public Transform lockboxCenter;
    public FCSLockBox fcsLockBox;
    public Transform lockedTarget;

    [Header("Rigging References")]
    public MultiAimConstraint torsoAimConstraint;
    public float aimSmoothSpeed = 25f;

    private Transform torsoIKProxyTarget;

    private Transform leftArmNode, rightArmNode;
    private PartSync leftArmSync, rightArmSync;
    private PartSync leftLowerArmSync, rightLowerArmSync;
    private Transform leftBackNode, rightBackNode;
    private Animator leftBackWeaponAnim, rightBackWeaponAnim;

    private Transform torsoNode;
    private PartSync torsoSync;

    private Camera mainCam;

    private float leftArmAimTransition = 1f;
    private float rightArmAimTransition = 1f;
    private float torsoAimTransition = 0f;

    void Start()
    {
        weaponManager = GetComponent<MechWeaponManager>();
        if (fcsLockBox == null) fcsLockBox = GetComponent<FCSLockBox>();
        mainCam = Camera.main;

        SetupIKProxyTarget();
    }

    private void SetupIKProxyTarget()
    {
        if (torsoAimConstraint != null)
        {
            torsoIKProxyTarget = new GameObject("TorsoIK_ProxyTarget").transform;

            if (lockboxCenter != null) torsoIKProxyTarget.position = lockboxCenter.position;
            else torsoIKProxyTarget.position = transform.position + transform.forward * 100f;

            var sources = torsoAimConstraint.data.sourceObjects;
            sources.Clear();
            sources.Add(new WeightedTransform(torsoIKProxyTarget, 1f));
            torsoAimConstraint.data.sourceObjects = sources;

            RigBuilder rig = torsoAimConstraint.GetComponentInParent<RigBuilder>();
            if (rig != null) rig.Build();
        }
    }

    public void CacheSpawnedNodes()
    {
        leftArmNode = PartSystem.FindDeepChild(transform, "LeftArmNode");
        rightArmNode = PartSystem.FindDeepChild(transform, "RightArmNode");
        leftBackNode = PartSystem.FindDeepChild(transform, "LeftBackWeaponNode");
        rightBackNode = PartSystem.FindDeepChild(transform, "RightBackWeaponNode");

        torsoNode = PartSystem.FindDeepChild(transform, "TorsoNode");
        if (torsoNode != null && torsoNode.childCount > 0)
            torsoSync = torsoNode.GetChild(0).GetComponent<PartSync>();

        if (leftArmNode != null && leftArmNode.childCount > 0)
        {
            leftArmSync = leftArmNode.GetChild(0).GetComponent<PartSync>();
            Transform lower = PartSystem.FindDeepChild(leftArmNode, "Lower Arm Bone Left");
            if (lower != null) leftLowerArmSync = lower.GetComponent<PartSync>();
        }

        if (rightArmNode != null && rightArmNode.childCount > 0)
        {
            rightArmSync = rightArmNode.GetChild(0).GetComponent<PartSync>();
            Transform lower = PartSystem.FindDeepChild(rightArmNode, "Lower Arm Bone Right");
            if (lower != null) rightLowerArmSync = lower.GetComponent<PartSync>();
        }

        if (leftBackNode != null && leftBackNode.childCount > 0)
            leftBackWeaponAnim = leftBackNode.GetChild(0).GetComponent<Animator>();

        if (rightBackNode != null && rightBackNode.childCount > 0)
            rightBackWeaponAnim = rightBackNode.GetChild(0).GetComponent<Animator>();
    }

    // --- PREDICTIVE AIM MATH ---
    private float GetBulletSpeed(bool isLeft, int slot)
    {
        if (weaponManager == null || weaponManager.weaponManager == null) return -1f;

        FunctionalWeapon wep = weaponManager.weaponManager.GetWeapon(isLeft, slot);
        if (wep != null && wep.GetWeaponData() is ProjectileWeaponPart proj && !(proj is MissileLauncherPart))
        {
            return proj.bulletSpeed;
        }
        return -1f; // -1 means no prediction needed (missiles, hitscan, melee)
    }

    private Vector3 GetPredictedTargetPosition(Transform weaponNode, Vector3 baseTargetPos, bool isLeft, int slot)
    {
        if (lockedTarget == null || fcsLockBox == null || !fcsLockBox.isHardLocked) return baseTargetPos;

        float bulletSpeed = GetBulletSpeed(isLeft, slot);
        if (bulletSpeed <= 0f) return baseTargetPos;

        float distance = Vector3.Distance(weaponNode.position, lockedTarget.position);
        float timeToHit = distance / bulletSpeed;

        // Kinematic Prediction: Target Velocity - My Velocity 
        Vector3 predictedPos = lockedTarget.position + (fcsLockBox.TargetVelocity - fcsLockBox.MyVelocity) * timeToHit;
        return predictedPos;
    }

    void LateUpdate()
    {
        if (weaponManager == null) return;
        if (fcsLockBox != null) lockedTarget = fcsLockBox.currentTarget;
        if (leftArmNode == null) CacheSpawnedNodes();
        if (leftArmNode == null) return;

        Vector3 baseTargetPos;
        if (lockedTarget != null) baseTargetPos = lockedTarget.position;
        else if (lockboxCenter != null) baseTargetPos = lockboxCenter.position;
        else
        {
            if (mainCam != null) baseTargetPos = mainCam.transform.position + mainCam.transform.forward * 300f;
            else baseTargetPos = transform.position + transform.forward * 300f;
        }

        // --- GET PREDICTED POSITIONS FOR EACH WEAPON SLOT ---
        Vector3 leftArmTarget = (leftArmSync != null) ? GetPredictedTargetPosition(leftArmSync.transform, baseTargetPos, true, 0) : baseTargetPos;
        Vector3 rightArmTarget = (rightArmSync != null) ? GetPredictedTargetPosition(rightArmSync.transform, baseTargetPos, false, 0) : baseTargetPos;
        Vector3 leftBackTarget = (leftBackNode != null) ? GetPredictedTargetPosition(leftBackNode, baseTargetPos, true, 1) : baseTargetPos;
        Vector3 rightBackTarget = (rightBackNode != null) ? GetPredictedTargetPosition(rightBackNode, baseTargetPos, false, 1) : baseTargetPos;

        // Torso tracks the general base target so the mech faces the enemy naturally
        if (torsoIKProxyTarget != null)
            torsoIKProxyTarget.position = Vector3.Lerp(torsoIKProxyTarget.position, baseTargetPos, Time.deltaTime * aimSmoothSpeed);

        float armSpeed = 1f / Mathf.Max(weaponManager.armTransitionTime, 0.01f);
        leftArmAimTransition = Mathf.MoveTowards(leftArmAimTransition, weaponManager.leftArmActive ? 1f : 0f, Time.deltaTime * armSpeed);
        rightArmAimTransition = Mathf.MoveTowards(rightArmAimTransition, weaponManager.rightArmActive ? 1f : 0f, Time.deltaTime * armSpeed);

        bool leftBackAiming = weaponManager.leftBackActive && weaponManager.hasAimableLeftBackWeapon;
        bool rightBackAiming = weaponManager.rightBackActive && weaponManager.hasAimableRightBackWeapon;
        bool torsoNeedsAiming = leftBackAiming || rightBackAiming;

        float torsoSpeed = 1f / Mathf.Max(weaponManager.backWeaponTransitionTime, 0.01f);
        torsoAimTransition = Mathf.MoveTowards(torsoAimTransition, torsoNeedsAiming ? 1f : 0f, Time.deltaTime * torsoSpeed);

        bool isTorsoOverridden = torsoAimTransition > 0f;

        // --- 1. TORSO LOGIC ---
        if (torsoSync != null && torsoSync.targetBone != null)
        {
            torsoSync.overrideRotation = isTorsoOverridden;
            if (isTorsoOverridden)
            {
                Quaternion stowedTorsoRot = torsoSync.targetBone.rotation;
                Quaternion aimedTorsoRot = stowedTorsoRot;

                Vector3 worldDirection = (torsoIKProxyTarget != null ? torsoIKProxyTarget.position : baseTargetPos - torsoSync.transform.position).normalized;
                if (worldDirection.sqrMagnitude > 0.01f && torsoSync.transform.parent != null)
                {
                    Vector3 localDirection = torsoSync.transform.parent.InverseTransformDirection(worldDirection);
                    float yawAngle = Mathf.Atan2(localDirection.x, localDirection.y) * Mathf.Rad2Deg;
                    aimedTorsoRot = torsoSync.transform.parent.rotation * Quaternion.Euler(0f, 0f, -yawAngle);
                }

                torsoSync.transform.rotation = Quaternion.Slerp(stowedTorsoRot, aimedTorsoRot, torsoAimTransition);
            }
        }

        // --- 2. LEFT ARM LOGIC (Uses Predictive Target) ---
        if (leftArmSync != null && leftArmSync.targetBone != null)
        {
            leftArmSync.overrideRotation = true;
            if (leftLowerArmSync != null) leftLowerArmSync.overrideRotation = true;

            Quaternion stowedRot;
            if (isTorsoOverridden && torsoSync != null && torsoSync.targetBone != null)
            {
                Quaternion armLocalToTorso = Quaternion.Inverse(torsoSync.targetBone.rotation) * leftArmSync.targetBone.rotation;
                stowedRot = torsoSync.transform.rotation * armLocalToTorso;
            }
            else stowedRot = leftArmSync.targetBone.rotation;

            Quaternion aimedRot = stowedRot;
            if (leftArmSync.transform.parent != null)
            {
                Vector3 worldDirection = (leftArmTarget - leftArmSync.transform.position).normalized;
                if (worldDirection.sqrMagnitude > 0.01f)
                {
                    Vector3 localDirection = leftArmSync.transform.parent.InverseTransformDirection(worldDirection);
                    Quaternion localLookRot = Quaternion.LookRotation(localDirection);
                    aimedRot = leftArmSync.transform.parent.rotation * (localLookRot * Quaternion.Euler(-70f, 0f, 0f));
                }
            }

            leftArmSync.transform.rotation = Quaternion.Slerp(stowedRot, aimedRot, leftArmAimTransition);

            if (leftLowerArmSync != null && leftLowerArmSync.targetBone != null)
            {
                Quaternion lowerStowedRot;
                if (isTorsoOverridden && torsoSync != null && torsoSync.targetBone != null)
                {
                    Quaternion lowerLocalToUpper = Quaternion.Inverse(leftArmSync.targetBone.rotation) * leftLowerArmSync.targetBone.rotation;
                    lowerStowedRot = leftArmSync.transform.rotation * lowerLocalToUpper;
                }
                else lowerStowedRot = leftLowerArmSync.targetBone.rotation;

                Quaternion lowerAimedRot = leftArmSync.transform.rotation * Quaternion.Euler(-110f, 0f, 0f);
                leftLowerArmSync.transform.rotation = Quaternion.Slerp(lowerStowedRot, lowerAimedRot, leftArmAimTransition);
            }
        }

        // --- 3. RIGHT ARM LOGIC (Uses Predictive Target) ---
        if (rightArmSync != null && rightArmSync.targetBone != null)
        {
            rightArmSync.overrideRotation = true;
            if (rightLowerArmSync != null) rightLowerArmSync.overrideRotation = true;

            Quaternion stowedRot;
            if (isTorsoOverridden && torsoSync != null && torsoSync.targetBone != null)
            {
                Quaternion armLocalToTorso = Quaternion.Inverse(torsoSync.targetBone.rotation) * rightArmSync.targetBone.rotation;
                stowedRot = torsoSync.transform.rotation * armLocalToTorso;
            }
            else stowedRot = rightArmSync.targetBone.rotation;

            Quaternion aimedRot = stowedRot;
            if (rightArmSync.transform.parent != null)
            {
                Vector3 worldDirection = (rightArmTarget - rightArmSync.transform.position).normalized;
                if (worldDirection.sqrMagnitude > 0.01f)
                {
                    Vector3 localDirection = rightArmSync.transform.parent.InverseTransformDirection(worldDirection);
                    Quaternion localLookRot = Quaternion.LookRotation(localDirection);
                    aimedRot = rightArmSync.transform.parent.rotation * (localLookRot * Quaternion.Euler(-70f, 0f, 0f));
                }
            }

            rightArmSync.transform.rotation = Quaternion.Slerp(stowedRot, aimedRot, rightArmAimTransition);

            if (rightLowerArmSync != null && rightLowerArmSync.targetBone != null)
            {
                Quaternion lowerStowedRot;
                if (isTorsoOverridden && torsoSync != null && torsoSync.targetBone != null)
                {
                    Quaternion lowerLocalToUpper = Quaternion.Inverse(rightArmSync.targetBone.rotation) * rightLowerArmSync.targetBone.rotation;
                    lowerStowedRot = rightArmSync.transform.rotation * lowerLocalToUpper;
                }
                else lowerStowedRot = rightLowerArmSync.targetBone.rotation;

                Quaternion lowerAimedRot = rightArmSync.transform.rotation * Quaternion.Euler(-110f, 0f, 0f);
                rightLowerArmSync.transform.rotation = Quaternion.Slerp(lowerStowedRot, lowerAimedRot, rightArmAimTransition);
            }
        }

        // --- 4. BACK WEAPONS (Uses Predictive Targets) ---
        float targetTorsoWeight = 0f;
        if (!torsoNeedsAiming && (weaponManager.leftArmActive || weaponManager.rightArmActive)) targetTorsoWeight = 0.5f;

        if (torsoAimConstraint != null)
            torsoAimConstraint.weight = Mathf.Lerp(torsoAimConstraint.weight, targetTorsoWeight, Time.deltaTime * aimSmoothSpeed);

        UpdateBackWeapon(leftBackNode, leftBackWeaponAnim, weaponManager.leftBackActive, leftBackAiming, leftBackTarget);
        UpdateBackWeapon(rightBackNode, rightBackWeaponAnim, weaponManager.rightBackActive, rightBackAiming, rightBackTarget);
    }

    private void UpdateBackWeapon(Transform node, Animator anim, bool isActive, bool isAiming, Vector3 targetPos)
    {
        if (anim != null) anim.SetBool("IsDeployed", isActive);
        if (node == null) return;

        if (isAiming) AimBackWeaponPitch(node, targetPos);
        else ResetBackWeaponPitch(node);
    }

    private void AimBackWeaponPitch(Transform backNode, Vector3 targetPosition)
    {
        Vector3 localTargetDir = backNode.parent.InverseTransformDirection(targetPosition - backNode.position);
        float angle = Mathf.Atan2(localTargetDir.y, localTargetDir.z) * Mathf.Rad2Deg;
        float targetX = Mathf.Clamp(-angle, -30f, 45f);

        backNode.localRotation = Quaternion.Euler(targetX, 0f, 0f);
    }

    private void ResetBackWeaponPitch(Transform backNode)
    {
        backNode.localRotation = Quaternion.Slerp(backNode.localRotation, Quaternion.identity, Time.deltaTime * aimSmoothSpeed);
    }
}