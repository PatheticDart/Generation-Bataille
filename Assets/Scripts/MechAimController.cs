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

    // --- Smooth Transition Trackers ---
    private float leftArmAimTransition = 1f;
    private float rightArmAimTransition = 1f;
    
    // NEW: Tracks the Torso's transition between normal walking and rigid aiming
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
        {
            torsoSync = torsoNode.GetChild(0).GetComponent<PartSync>();
        }

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

    void LateUpdate()
    {
        if (weaponManager == null) return;
        if (fcsLockBox != null) lockedTarget = fcsLockBox.currentTarget;
        if (leftArmNode == null) CacheSpawnedNodes();
        if (leftArmNode == null) return;

        Vector3 targetPos;
        if (lockedTarget != null) targetPos = lockedTarget.position;
        else if (lockboxCenter != null) targetPos = lockboxCenter.position;
        else 
        {
            if (mainCam != null) targetPos = mainCam.transform.position + mainCam.transform.forward * 300f;
            else targetPos = transform.position + transform.forward * 300f;
        }

        if (torsoIKProxyTarget != null)
            torsoIKProxyTarget.position = Vector3.Lerp(torsoIKProxyTarget.position, targetPos, Time.deltaTime * aimSmoothSpeed);

        Vector3 smoothedTargetPos = torsoIKProxyTarget != null ? torsoIKProxyTarget.position : targetPos;

        // --- TRANSITION TIMERS ---
        float armSpeed = 1f / Mathf.Max(weaponManager.armTransitionTime, 0.01f);
        leftArmAimTransition = Mathf.MoveTowards(leftArmAimTransition, weaponManager.leftArmActive ? 1f : 0f, Time.deltaTime * armSpeed);
        rightArmAimTransition = Mathf.MoveTowards(rightArmAimTransition, weaponManager.rightArmActive ? 1f : 0f, Time.deltaTime * armSpeed);

        bool leftBackActive = weaponManager.leftBackActive;
        bool rightBackActive = weaponManager.rightBackActive;
        bool leftBackAiming = leftBackActive && weaponManager.hasAimableLeftBackWeapon;
        bool rightBackAiming = rightBackActive && weaponManager.hasAimableRightBackWeapon;
        bool torsoNeedsAiming = leftBackAiming || rightBackAiming;

        // NEW: Torso Transition Timer. It takes exactly the duration of the back weapon deploy animation to swing the torso into place!
        float torsoSpeed = 1f / Mathf.Max(weaponManager.backWeaponTransitionTime, 0.01f);
        torsoAimTransition = Mathf.MoveTowards(torsoAimTransition, torsoNeedsAiming ? 1f : 0f, Time.deltaTime * torsoSpeed);

        // If the torso transition is anywhere above 0, we must override it so the arms know to mathematically stick to it.
        bool isTorsoOverridden = torsoAimTransition > 0f;

        // --- 1. TORSO LOGIC (Fully Blended) ---
        if (torsoSync != null && torsoSync.targetBone != null)
        {
            torsoSync.overrideRotation = isTorsoOverridden;
            if (isTorsoOverridden)
            {
                Quaternion stowedTorsoRot = torsoSync.targetBone.rotation;
                Quaternion aimedTorsoRot = stowedTorsoRot;

                Vector3 worldDirection = (smoothedTargetPos - torsoSync.transform.position).normalized;
                if (worldDirection.sqrMagnitude > 0.01f && torsoSync.transform.parent != null)
                {
                    Vector3 localDirection = torsoSync.transform.parent.InverseTransformDirection(worldDirection);
                    float yawAngle = Mathf.Atan2(localDirection.x, localDirection.y) * Mathf.Rad2Deg;
                    aimedTorsoRot = torsoSync.transform.parent.rotation * Quaternion.Euler(0f, 0f, -yawAngle);
                }

                // Smoothly slide the torso from its bouncy skeleton animation to the rigid targeting angle
                torsoSync.transform.rotation = Quaternion.Slerp(stowedTorsoRot, aimedTorsoRot, torsoAimTransition);
            }
        }

        // --- 2. LEFT ARM LOGIC (Fully Blended) ---
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
            else
            {
                stowedRot = leftArmSync.targetBone.rotation;
            }

            Quaternion aimedRot = stowedRot; 
            if (leftArmSync.transform.parent != null)
            {
                Vector3 worldDirection = (smoothedTargetPos - leftArmSync.transform.position).normalized;
                if (worldDirection.sqrMagnitude > 0.01f)
                {
                    Vector3 localDirection = leftArmSync.transform.parent.InverseTransformDirection(worldDirection);
                    Quaternion localLookRot = Quaternion.LookRotation(localDirection);
                    Quaternion offset = Quaternion.Euler(-70f, 0f, 0f);
                    aimedRot = leftArmSync.transform.parent.rotation * (localLookRot * offset);
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
                else
                {
                    lowerStowedRot = leftLowerArmSync.targetBone.rotation;
                }

                Quaternion lowerAimedRot = leftArmSync.transform.rotation * Quaternion.Euler(-110f, 0f, 0f);
                leftLowerArmSync.transform.rotation = Quaternion.Slerp(lowerStowedRot, lowerAimedRot, leftArmAimTransition);
            }
        }

        // --- 3. RIGHT ARM LOGIC (Fully Blended) ---
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
            else
            {
                stowedRot = rightArmSync.targetBone.rotation;
            }

            Quaternion aimedRot = stowedRot; 
            if (rightArmSync.transform.parent != null)
            {
                Vector3 worldDirection = (smoothedTargetPos - rightArmSync.transform.position).normalized;
                if (worldDirection.sqrMagnitude > 0.01f)
                {
                    Vector3 localDirection = rightArmSync.transform.parent.InverseTransformDirection(worldDirection);
                    Quaternion localLookRot = Quaternion.LookRotation(localDirection);
                    Quaternion offset = Quaternion.Euler(-70f, 0f, 0f);
                    aimedRot = rightArmSync.transform.parent.rotation * (localLookRot * offset);
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
                else
                {
                    lowerStowedRot = rightLowerArmSync.targetBone.rotation;
                }

                Quaternion lowerAimedRot = rightArmSync.transform.rotation * Quaternion.Euler(-110f, 0f, 0f);
                rightLowerArmSync.transform.rotation = Quaternion.Slerp(lowerStowedRot, lowerAimedRot, rightArmAimTransition);
            }
        }

        // --- 4. BACK WEAPONS & RIGGING ---
        float targetTorsoWeight = 0f;
        if (!torsoNeedsAiming && (weaponManager.leftArmActive || weaponManager.rightArmActive))
        {
            targetTorsoWeight = 0.5f; 
        }

        if (torsoAimConstraint != null)
            torsoAimConstraint.weight = Mathf.Lerp(torsoAimConstraint.weight, targetTorsoWeight, Time.deltaTime * aimSmoothSpeed);

        UpdateBackWeapon(leftBackNode, leftBackWeaponAnim, leftBackActive, leftBackAiming, smoothedTargetPos);
        UpdateBackWeapon(rightBackNode, rightBackWeaponAnim, rightBackActive, rightBackAiming, smoothedTargetPos);
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