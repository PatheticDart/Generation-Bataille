using UnityEngine;
using UnityEngine.Animations.Rigging;

[RequireComponent(typeof(MechWeaponManager))]
public class MechAimController : MonoBehaviour
{
    private MechWeaponManager weaponManager;

    [Header("Targeting References")]
    public Transform lockboxCenter;
    public FCSLockBox fcsLockBox;
    public Transform lockedTarget;

    [Header("Rigging References")]
    public MultiAimConstraint torsoAimConstraint;
    public float aimSmoothSpeed = 15f;

    private Transform torsoIKProxyTarget;

    // Node References
    private Transform leftArmNode, rightArmNode;
    private PartSync leftArmSync, rightArmSync;
    private PartSync leftLowerArmSync, rightLowerArmSync;
    private Transform leftBackNode, rightBackNode;
    private Animator leftBackWeaponAnim, rightBackWeaponAnim;

    void Start()
    {
        // Now getting the unified MechWeaponManager
        weaponManager = GetComponent<MechWeaponManager>();
        if (fcsLockBox == null) fcsLockBox = GetComponent<FCSLockBox>();

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
            leftBackWeaponAnim = leftBackNode.GetChild(0).GetComponent<Animator>();

        if (rightBackNode != null && rightBackNode.childCount > 0)
            rightBackWeaponAnim = rightBackNode.GetChild(0).GetComponent<Animator>();
    }

    void LateUpdate()
    {
        if (weaponManager == null) return;
        if (fcsLockBox != null) lockedTarget = fcsLockBox.currentTarget;
        if (leftArmNode == null) CacheSpawnedNodes();
        if (leftArmNode == null) return;

        // Determine Look-At Position
        Vector3 targetPos;
        if (lockedTarget != null) targetPos = lockedTarget.position;
        else if (lockboxCenter != null) targetPos = lockboxCenter.position;
        else targetPos = transform.position + transform.forward * 100f;

        // Update Torso Proxy
        if (torsoIKProxyTarget != null)
            torsoIKProxyTarget.position = Vector3.Lerp(torsoIKProxyTarget.position, targetPos, Time.deltaTime * aimSmoothSpeed);

        // --- LEFT ARM AIM ---
        if (leftArmSync != null)
        {
            leftArmSync.overrideRotation = weaponManager.leftArmActive;
            if (leftLowerArmSync != null) leftLowerArmSync.overrideRotation = weaponManager.leftArmActive;

            if (weaponManager.leftArmActive) 
                AimArmAt(leftArmSync.transform, leftLowerArmSync?.transform, targetPos);
        }

        // --- RIGHT ARM AIM ---
        if (rightArmSync != null)
        {
            rightArmSync.overrideRotation = weaponManager.rightArmActive;
            if (rightLowerArmSync != null) rightLowerArmSync.overrideRotation = weaponManager.rightArmActive;

            if (weaponManager.rightArmActive) 
                AimArmAt(rightArmSync.transform, rightLowerArmSync?.transform, targetPos);
        }

        // --- BACK WEAPON LOGIC ---
        bool leftBackActive = !weaponManager.leftArmActive;
        bool rightBackActive = !weaponManager.rightArmActive;

        bool leftBackAiming = leftBackActive && weaponManager.hasAimableLeftBackWeapon;
        bool rightBackAiming = rightBackActive && weaponManager.hasAimableRightBackWeapon;

        // Dynamic Torso Weight based on active systems
        float targetTorsoWeight = 0f;
        if (leftBackAiming || rightBackAiming) targetTorsoWeight = 1f;
        else if (weaponManager.leftArmActive || weaponManager.rightArmActive) targetTorsoWeight = 0.5f;

        if (torsoAimConstraint != null)
            torsoAimConstraint.weight = Mathf.Lerp(torsoAimConstraint.weight, targetTorsoWeight, Time.deltaTime * aimSmoothSpeed);

        // Handle Deployed Animations and Pitching
        UpdateBackWeapon(leftBackNode, leftBackWeaponAnim, leftBackActive, leftBackAiming, targetPos);
        UpdateBackWeapon(rightBackNode, rightBackWeaponAnim, rightBackActive, rightBackAiming, targetPos);
    }

    private void UpdateBackWeapon(Transform node, Animator anim, bool isActive, bool isAiming, Vector3 targetPos)
    {
        if (anim != null) anim.SetBool("IsDeployed", isActive);
        if (node == null) return;

        if (isAiming) AimBackWeaponPitch(node, targetPos);
        else ResetBackWeaponPitch(node);
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
                upperArm.rotation = Quaternion.Slerp(upperArm.rotation, lookRot * offset, Time.deltaTime * aimSmoothSpeed);
            }
        }
    }

    private void AimBackWeaponPitch(Transform backNode, Vector3 targetPosition)
    {
        Vector3 localTargetDir = backNode.parent.InverseTransformDirection(targetPosition - backNode.position);
        float angle = Mathf.Atan2(localTargetDir.y, localTargetDir.z) * Mathf.Rad2Deg;
        float targetX = Mathf.Clamp(-angle, -30f, 45f);

        backNode.localRotation = Quaternion.Slerp(backNode.localRotation, Quaternion.Euler(targetX, 0f, 0f), Time.deltaTime * aimSmoothSpeed);
    }

    private void ResetBackWeaponPitch(Transform backNode)
    {
        backNode.localRotation = Quaternion.Slerp(backNode.localRotation, Quaternion.identity, Time.deltaTime * aimSmoothSpeed);
    }
}