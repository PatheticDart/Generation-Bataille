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

    private Transform torsoIKProxyTarget;

    private Transform leftArmNode;
    private PartSync leftArmSync;
    private Transform rightArmNode;
    private PartSync rightArmSync;

    private PartSync leftLowerArmSync;
    private PartSync rightLowerArmSync;

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
            torsoIKProxyTarget = new GameObject("TorsoIK_ProxyTarget").transform;

            // THE FIX: Safe fallback if this mech doesn't have a UI Canvas!
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

        // THE FIX: Secure fallback for aim determination
        Vector3 targetPos;
        if (lockedTarget != null) targetPos = lockedTarget.position;
        else if (lockboxCenter != null) targetPos = lockboxCenter.position;
        else targetPos = transform.position + transform.forward * 100f;

        if (torsoIKProxyTarget != null)
        {
            torsoIKProxyTarget.position = Vector3.Lerp(torsoIKProxyTarget.position, targetPos, Time.deltaTime * aimSmoothSpeed);
        }

        if (leftArmSync != null)
        {
            leftArmSync.overrideRotation = weaponManager.leftArmActive;
            if (leftLowerArmSync != null) leftLowerArmSync.overrideRotation = weaponManager.leftArmActive;

            if (weaponManager.leftArmActive) AimArmAt(leftArmSync.transform, leftLowerArmSync != null ? leftLowerArmSync.transform : null, targetPos);
        }

        if (rightArmSync != null)
        {
            rightArmSync.overrideRotation = weaponManager.rightArmActive;
            if (rightLowerArmSync != null) rightLowerArmSync.overrideRotation = weaponManager.rightArmActive;

            if (weaponManager.rightArmActive) AimArmAt(rightArmSync.transform, rightLowerArmSync != null ? rightLowerArmSync.transform : null, targetPos);
        }

        bool leftBackActive = !weaponManager.leftArmActive;
        bool rightBackActive = !weaponManager.rightArmActive;

        bool leftBackAiming = leftBackActive && weaponManager.hasAimableLeftBackWeapon;
        bool rightBackAiming = rightBackActive && weaponManager.hasAimableRightBackWeapon;

        float targetTorsoWeight = 0f;
        if (leftBackAiming || rightBackAiming)
        {
            targetTorsoWeight = 1f;
        }
        else if (weaponManager.leftArmActive || weaponManager.rightArmActive)
        {
            targetTorsoWeight = 0.5f;
        }

        if (torsoAimConstraint != null)
        {
            torsoAimConstraint.weight = Mathf.Lerp(torsoAimConstraint.weight, targetTorsoWeight, Time.deltaTime * aimSmoothSpeed);
        }

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

        if (targetX > 180f) targetX -= 360f;
        if (targetX < -180f) targetX += 360f;

        targetX = Mathf.Clamp(targetX, -30f, 45f);

        Quaternion targetLocalRot = Quaternion.Euler(targetX, 0f, 0f);
        backNode.localRotation = Quaternion.Slerp(backNode.localRotation, targetLocalRot, Time.deltaTime * aimSmoothSpeed);
    }

    private void ResetBackWeaponPitch(Transform backNode)
    {
        backNode.localRotation = Quaternion.Slerp(backNode.localRotation, Quaternion.identity, Time.deltaTime * aimSmoothSpeed);
    }
}