using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(MechWeaponManager))]
public class MechAimController : MonoBehaviour
{
    private MechWeaponManager weaponManager;

    [Header("Targeting References")]
    [Tooltip("The default center of the screen/HUD lockbox.")]
    public Transform lockboxCenter;

    [Tooltip("Reference to the FCS Lock Box to auto-assign targets.")]
    public FCSLockBox fcsLockBox;

    [Tooltip("The currently locked-on enemy (Read-Only).")]
    public Transform lockedTarget;

    [Header("Rigging References")]
    public MultiAimConstraint torsoAimConstraint;
    public float aimSmoothSpeed = 15f;

    // We use a proxy target to avoid rebuilding the rig every time you lock on
    private Transform torsoIKProxyTarget;

    // Cached references for Upper Arms
    private Transform leftArmNode;
    private PartSync leftArmSync;
    private Transform rightArmNode;
    private PartSync rightArmSync;

    // Cached references for Lower Arms
    private PartSync leftLowerArmSync;
    private PartSync rightLowerArmSync;

    // Cached references for Back Weapons
    private Transform leftBackNode;
    private Animator leftBackWeaponAnim;

    private Transform rightBackNode;
    private Animator rightBackWeaponAnim;

    void Start()
    {
        weaponManager = GetComponent<MechWeaponManager>();
        if (fcsLockBox == null) fcsLockBox = GetComponent<FCSLockBox>();

        SetupIKProxyTarget();
    }

    private void SetupIKProxyTarget()
    {
        if (torsoAimConstraint != null)
        {
            // Create a dedicated invisible target for the IK to permanently track
            torsoIKProxyTarget = new GameObject("TorsoIK_ProxyTarget").transform;
            torsoIKProxyTarget.position = lockboxCenter.position;

            // Assign it to the Multi-Aim Constraint
            var sources = torsoAimConstraint.data.sourceObjects;
            sources.Clear();
            sources.Add(new WeightedTransform(torsoIKProxyTarget, 1f));
            torsoAimConstraint.data.sourceObjects = sources;

            // Rebuild the rig once on startup to lock it in
            RigBuilder rig = torsoAimConstraint.GetComponentInParent<RigBuilder>();
            if (rig != null) rig.Build();
        }
    }

    public void CacheSpawnedNodes()
    {
        leftArmNode = MechLoader.FindDeepChild(transform, "LeftArmNode");
        rightArmNode = MechLoader.FindDeepChild(transform, "RightArmNode");
        leftBackNode = MechLoader.FindDeepChild(transform, "LeftBackWeaponNode");
        rightBackNode = MechLoader.FindDeepChild(transform, "RightBackWeaponNode");

        if (leftArmNode != null && leftArmNode.childCount > 0)
        {
            leftArmSync = leftArmNode.GetChild(0).GetComponent<PartSync>();
            Transform lower = MechLoader.FindDeepChild(leftArmNode, "Lower Arm Bone Left");
            if (lower != null) leftLowerArmSync = lower.GetComponent<PartSync>();
        }

        if (rightArmNode != null && rightArmNode.childCount > 0)
        {
            rightArmSync = rightArmNode.GetChild(0).GetComponent<PartSync>();
            Transform lower = MechLoader.FindDeepChild(rightArmNode, "Lower Arm Bone Right");
            if (lower != null) rightLowerArmSync = lower.GetComponent<PartSync>();
        }

        if (leftBackNode != null && leftBackNode.childCount > 0)
        {
            leftBackWeaponAnim = leftBackNode.GetChild(0).GetComponent<Animator>();
        }

        if (rightBackNode != null && rightBackNode.childCount > 0)
        {
            rightBackWeaponAnim = rightBackNode.GetChild(0).GetComponent<Animator>();
        }
    }

    void LateUpdate()
    {
        if (weaponManager == null) return;

        if (fcsLockBox != null) lockedTarget = fcsLockBox.currentTarget;

        if (leftArmNode == null) CacheSpawnedNodes();
        if (leftArmNode == null) return;

        // Determine final focus point
        Vector3 targetPos = (lockedTarget != null) ? lockedTarget.position : lockboxCenter.position;

        // Smoothly move the IK Proxy Target to the focus point
        if (torsoIKProxyTarget != null)
        {
            torsoIKProxyTarget.position = Vector3.Lerp(torsoIKProxyTarget.position, targetPos, Time.deltaTime * aimSmoothSpeed);
        }

        // --- LEFT ARM LOGIC ---
        if (leftArmSync != null)
        {
            leftArmSync.overrideRotation = weaponManager.leftArmActive;
            if (leftLowerArmSync != null) leftLowerArmSync.overrideRotation = weaponManager.leftArmActive;

            if (weaponManager.leftArmActive) AimArmAt(leftArmSync.transform, leftLowerArmSync != null ? leftLowerArmSync.transform : null, targetPos);
        }

        // --- RIGHT ARM LOGIC ---
        if (rightArmSync != null)
        {
            rightArmSync.overrideRotation = weaponManager.rightArmActive;
            if (rightLowerArmSync != null) rightLowerArmSync.overrideRotation = weaponManager.rightArmActive;

            if (weaponManager.rightArmActive) AimArmAt(rightArmSync.transform, rightLowerArmSync != null ? rightLowerArmSync.transform : null, targetPos);
        }

        // --- STATE SEPARATION LOGIC ---
        // 1. Is the weapon currently selected by the player?
        bool leftBackActive = !weaponManager.leftArmActive;
        bool rightBackActive = !weaponManager.rightArmActive;

        // 2. Is it selected AND capable of aiming?
        bool leftBackAiming = leftBackActive && weaponManager.hasAimableLeftBackWeapon;
        bool rightBackAiming = rightBackActive && weaponManager.hasAimableRightBackWeapon;

        // --- TORSO WEIGHT PRIORITY LOGIC ---
        float targetTorsoWeight = 0f;
        if (leftBackAiming || rightBackAiming)
        {
            targetTorsoWeight = 1f; // Aimable back weapons demand full torso twist
        }
        else if (weaponManager.leftArmActive || weaponManager.rightArmActive)
        {
            targetTorsoWeight = 0.5f; // Arms use half twist for natural posture
        }

        if (torsoAimConstraint != null)
        {
            torsoAimConstraint.weight = Mathf.Lerp(torsoAimConstraint.weight, targetTorsoWeight, Time.deltaTime * aimSmoothSpeed);
        }

        // --- BACK WEAPON ANIMATION & PITCHING ---
        // Animations now trigger simply if the weapon is active, regardless of aimability
        if (leftBackWeaponAnim != null) leftBackWeaponAnim.SetBool("IsDeployed", leftBackActive);

        if (leftBackNode != null)
        {
            if (leftBackAiming) AimBackWeaponPitch(leftBackNode, targetPos);
            else ResetBackWeaponPitch(leftBackNode);
        }

        if (rightBackWeaponAnim != null) rightBackWeaponAnim.SetBool("IsDeployed", rightBackActive);

        if (rightBackNode != null)
        {
            if (rightBackAiming) AimBackWeaponPitch(rightBackNode, targetPos);
            else ResetBackWeaponPitch(rightBackNode);
        }
    }

    private void AimArmAt(Transform upperArm, Transform lowerArm, Vector3 targetPosition)
    {
        if (lowerArm != null)
        {
            Quaternion targetLowerRot = Quaternion.Euler(-110f, 0f, 0f);
            lowerArm.localRotation = Quaternion.Slerp(lowerArm.localRotation, targetLowerRot, Time.deltaTime * aimSmoothSpeed);
        }

        if (upperArm != null)
        {
            Vector3 direction = (targetPosition - upperArm.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRot = Quaternion.LookRotation(direction);
                // Note: using the -70 offset as requested/established in your setup
                Quaternion offset = Quaternion.Euler(-70f, 0f, 0f);
                Quaternion finalRot = lookRot * offset;
                upperArm.rotation = Quaternion.Slerp(upperArm.rotation, finalRot, Time.deltaTime * aimSmoothSpeed);
            }
        }
    }

    private void AimBackWeaponPitch(Transform backNode, Vector3 targetPosition)
    {
        Vector3 localTargetDir = backNode.parent.InverseTransformDirection(targetPosition - backNode.position);
        float angle = Mathf.Atan2(localTargetDir.y, localTargetDir.z) * Mathf.Rad2Deg;

        float targetX = -angle;

        // MATHEMATICAL FIX: Force the angle into a standard -180 to 180 range before clamping
        if (targetX > 180f) targetX -= 360f;
        if (targetX < -180f) targetX += 360f;

        // Apply strict clamp: -30 (up) to 45 (down)
        targetX = Mathf.Clamp(targetX, -30f, 45f);

        Quaternion targetLocalRot = Quaternion.Euler(targetX, 0f, 0f);
        backNode.localRotation = Quaternion.Slerp(backNode.localRotation, targetLocalRot, Time.deltaTime * aimSmoothSpeed);
    }

    private void ResetBackWeaponPitch(Transform backNode)
    {
        backNode.localRotation = Quaternion.Slerp(backNode.localRotation, Quaternion.identity, Time.deltaTime * aimSmoothSpeed);
    }
}