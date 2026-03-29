using System.Collections.Generic;
using UnityEngine;

// --- ENUMS & CLASSES FOR THE CHIP SYSTEM ---
public enum MovementActionType { Range, MoveForward }
public enum ApproachActionType { Boosting, Flying }
public enum WeaponConditionSlot { Arm, Back }

[System.Serializable]
public class MovementChip
{
    public MovementActionType actionType;
    [Tooltip("How long the AI executes this specific move action (in seconds).")]
    public float duration = 5.0f;
    [Tooltip("Target distance from the enemy (in meters).")]
    public float targetRange = 200f;
    [Tooltip("Leeway given to Target Range. Randomly added/subtracted upon chip start.")]
    public float distanceUncertainty = 15f;
    [Tooltip("Elevation relative to the target. Higher = AI tries to get above target.")]
    public float relativeElevation = 0f;

    [Header("Move Forward Settings (Ignored if action is 'Range')")]
    [Tooltip("Angle relative to the FRONT of the target. (-90 = Right, 90 = Left, 180 = Behind)")]
    public float targetAngle = -30f;
    [Tooltip("Random leeway added/subtracted to the Target Angle.")]
    public float angleUncertainty = 5f;
}

[System.Serializable]
public class WeaponConditionChip
{
    [Tooltip("Which slot this chip controls (The list it is placed in determines Left vs Right).")]
    public WeaponConditionSlot installLocation = WeaponConditionSlot.Arm;

    [Header("Firing Conditions")]
    [Tooltip("Fires when target distance is GREATER than or equal to this (meters).")]
    public float minAttackRange = 0f;
    [Tooltip("Fires when target distance is LESS than or equal to this (meters).")]
    public float maxAttackRange = 150f;

    [Header("Active/Equip Conditions")]
    [Tooltip("Swaps to this weapon when target distance is GREATER than or equal to this (meters).")]
    public float minActiveRange = 0f;
    [Tooltip("Swaps to this weapon when target distance is LESS than or equal to this (meters).")]
    public float maxActiveRange = 200f;
}

[RequireComponent(typeof(MechController))]
public class PrototypeAIBrain : MonoBehaviour
{
    private MechController controller;
    private CharacterController charController;
    private MechStats stats;

    [Header("Weapon Systems")]
    public MechWeaponManager mechWeaponManager;
    public WeaponManager weaponManager;

    [Header("Aiming & Vision")]
    public Transform cameraPivot;
    public float aimTrackingSpeed = 15f;
    public float eyeHeight = 2f;

    [Header("Targeting")]
    public LayerMask targetLayer;
    public float detectionRadius = 400f;
    public Transform currentTarget;

    [Header("Movement Rule Set")]
    public float approachRange = 300f;
    public ApproachActionType approachType = ApproachActionType.Boosting;
    [Tooltip("Approach action will not execute if EN% is <= this value.")]
    [Range(0f, 100f)] public float approachCriticalENRate = 30f;
    [Tooltip("If Critical EN is hit, AI must wait until EN% reaches this value before approaching again.")]
    [Range(0f, 100f)] public float approachRequiredENRate = 70f;

    [Header("Default Action Probabilities")]
    [Tooltip("Chance to randomly perform an evasive ground boost.")]
    [Range(0f, 100f)] public float boostChance = 40f;
    [Tooltip("Chance AI will attempt a Quick Boost during combat.")]
    [Range(0f, 100f)] public float quickBoostChance = 25f;
    [Tooltip("If the AI Quick Boosts, this is the chance it will execute a Perfect QB.")]
    [Range(0f, 100f)] public float perfectQuickBoostChance = 15f;

    [Header("Movement Order (Chip System)")]
    public List<MovementChip> movementOrderList = new List<MovementChip>();

    [Header("Left Weapon Conditions (Chip System)")]
    public List<WeaponConditionChip> leftWeaponConditions = new List<WeaponConditionChip>();

    [Header("Right Weapon Conditions (Chip System)")]
    public List<WeaponConditionChip> rightWeaponConditions = new List<WeaponConditionChip>();

    [Header("Tactical Environment Maneuvering (TEM)")]
    public LayerMask environmentLayer;
    [Tooltip("Max height of an obstacle the AI will attempt to automatically jump over.")]
    public float maxObstacleJumpHeight = 6f;
    [Tooltip("How high the mech can naturally jump without needing to activate flight boosters.")]
    public float standardJumpHeight = 3f;

    [Header("Energy Management")]
    [Range(0f, 100f)] public float energyEfficiency = 80f;

    // --- STATE MACHINE & TIMERS ---
    private bool isApproachingState = false;
    private float outOfRangeBufferTimer = 0f;

    // Energy Locks
    private bool approachEnRecoveryLock = false;
    private bool isConservingEnergy = false;

    // Chip Execution State
    private int currentChipIndex = 0;
    private float currentChipTimer = 0f;
    private float activeTargetRange = 0f;
    private float activeTargetAngle = 0f;

    // TEM & Actions State
    private bool hasLineOfSight = true;
    private float strafeDirection = 1f;
    private float randomActionTickTimer = 1f;

    // Active Action Overrides
    private float activeBoostTimer = 0f;
    private float activeJumpTimer = 0f;
    private float activeFlightTimer = 0f;

    // Weapon Firing State Memory
    private bool wasFiringLeft = false;
    private bool wasFiringRight = false;

    void Start()
    {
        controller = GetComponent<MechController>();
        charController = GetComponent<CharacterController>();
        stats = GetComponent<MechStats>();

        if (mechWeaponManager == null) mechWeaponManager = GetComponent<MechWeaponManager>();
        if (weaponManager == null) weaponManager = GetComponent<WeaponManager>();

        LoadNextChip(0);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        FindTargetContinuously();

        // --- LOSING TARGET LOCK ---
        if (currentTarget == null)
        {
            controller.moveInput = Vector3.zero;
            controller.isBoosting = false;
            controller.isJumping = false;

            // Release triggers immediately if the target is fully lost
            ProcessWeaponFiring(true, false);
            ProcessWeaponFiring(false, false);
            return;
        }

        HandleAiming();
        HandleTEMContinuous();
        ManageEnergyHysteresis();
        ManageMovementRulesAndState();

        if (isApproachingState)
        {
            ExecuteApproachBehavior();
        }
        else
        {
            ExecuteChipBehavior();
        }

        HandleRandomActions();
        ApplyFinalActions();

        EvaluateWeaponConditions();
    }

    private void EvaluateWeaponConditions()
    {
        if (mechWeaponManager == null || weaponManager == null || currentTarget == null) return;

        float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);

        // 1. Evaluate Left Weapons
        bool wantsToFireLeft = false;
        foreach (WeaponConditionChip chip in leftWeaponConditions)
        {
            int targetSlot = (chip.installLocation == WeaponConditionSlot.Arm) ? 0 : 1;
            float maxAmmo = weaponManager.GetMaxResource(true, targetSlot);
            float currentAmmo = weaponManager.GetCurrentResource(true, targetSlot);

            // --- THE FIX: Ammo Depletion Fallback ---
            // If this is an ammo-based weapon and it's empty, skip this chip entirely to check backups
            if (maxAmmo > 0 && currentAmmo <= 0) continue;

            if (distanceToTarget >= chip.minActiveRange && distanceToTarget <= chip.maxActiveRange)
            {
                bool isArmTargeted = (chip.installLocation == WeaponConditionSlot.Arm);

                if (mechWeaponManager.leftArmActive != isArmTargeted)
                {
                    if (!mechWeaponManager.IsLeftTransitioning)
                    {
                        mechWeaponManager.SendMessage("ProcessLeftSwap", SendMessageOptions.DontRequireReceiver);
                    }
                }
                else if (distanceToTarget >= chip.minAttackRange && distanceToTarget <= chip.maxAttackRange)
                {
                    // --- THE FIX: Line of Sight Stop ---
                    // Only pull the trigger if the AI can actually see the enemy
                    if (hasLineOfSight) wantsToFireLeft = true;
                }

                break; // Stop evaluating lower-priority chips if we found a valid range match
            }
        }

        // 2. Evaluate Right Weapons
        bool wantsToFireRight = false;
        foreach (WeaponConditionChip chip in rightWeaponConditions)
        {
            int targetSlot = (chip.installLocation == WeaponConditionSlot.Arm) ? 0 : 1;
            float maxAmmo = weaponManager.GetMaxResource(false, targetSlot);
            float currentAmmo = weaponManager.GetCurrentResource(false, targetSlot);

            if (maxAmmo > 0 && currentAmmo <= 0) continue;

            if (distanceToTarget >= chip.minActiveRange && distanceToTarget <= chip.maxActiveRange)
            {
                bool isArmTargeted = (chip.installLocation == WeaponConditionSlot.Arm);

                if (mechWeaponManager.rightArmActive != isArmTargeted)
                {
                    if (!mechWeaponManager.IsRightTransitioning)
                    {
                        mechWeaponManager.SendMessage("ProcessRightSwap", SendMessageOptions.DontRequireReceiver);
                    }
                }
                else if (distanceToTarget >= chip.minAttackRange && distanceToTarget <= chip.maxAttackRange)
                {
                    if (hasLineOfSight) wantsToFireRight = true;
                }

                break;
            }
        }

        ProcessWeaponFiring(true, wantsToFireLeft);
        ProcessWeaponFiring(false, wantsToFireRight);
    }

    private void ProcessWeaponFiring(bool isLeft, bool shouldFire)
    {
        int activeSlot = isLeft ? mechWeaponManager.ActiveLeftSlot : mechWeaponManager.ActiveRightSlot;
        bool wasFiring = isLeft ? wasFiringLeft : wasFiringRight;

        bool pressed = shouldFire && !wasFiring;
        bool held = shouldFire;
        bool released = !shouldFire && wasFiring;

        weaponManager.FireWeapon(isLeft, activeSlot, pressed, held, released);

        if (isLeft) wasFiringLeft = shouldFire;
        else wasFiringRight = shouldFire;
    }

    private void FindTargetContinuously()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, detectionRadius, targetLayer);
        float closestDist = Mathf.Infinity;
        Transform bestTarget = null;

        foreach (Collider hit in hits)
        {
            if (hit.transform.IsChildOf(transform)) continue;

            float dist = Vector3.Distance(transform.position, hit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                bestTarget = hit.transform;
            }
        }
        currentTarget = bestTarget;
    }

    private void HandleAiming()
    {
        Vector3 dirToTarget3D = currentTarget.position - transform.position;

        Vector3 dirToTargetFlat = dirToTarget3D;
        dirToTargetFlat.y = 0f;
        if (dirToTargetFlat.sqrMagnitude > 0.1f)
        {
            controller.lookTargetForward = dirToTargetFlat.normalized;
        }

        if (cameraPivot != null && dirToTarget3D.sqrMagnitude > 0.1f)
        {
            Quaternion targetPivotRot = Quaternion.LookRotation(dirToTarget3D.normalized);
            cameraPivot.rotation = Quaternion.Slerp(cameraPivot.rotation, targetPivotRot, Time.deltaTime * aimTrackingSpeed);
        }
    }

    private void HandleTEMContinuous()
    {
        Vector3 aiEye = transform.position + Vector3.up * eyeHeight;
        Vector3 targetEye = currentTarget.position + Vector3.up * eyeHeight;

        hasLineOfSight = !Physics.Linecast(aiEye, targetEye, environmentLayer);

        if (!hasLineOfSight)
        {
            Vector3 rightWhisker = transform.right * 5f;
            bool leftClear = !Physics.Linecast(aiEye - rightWhisker, targetEye, environmentLayer);
            bool rightClear = !Physics.Linecast(aiEye + rightWhisker, targetEye, environmentLayer);

            if (leftClear && !rightClear)
            {
                strafeDirection = -1f;
            }
            else if (rightClear && !leftClear)
            {
                strafeDirection = 1f;
            }
        }
    }

    private void CheckObstaclesAndJump(Vector3 worldMoveDirection)
    {
        if (worldMoveDirection.sqrMagnitude < 0.1f) return;

        float checkDist = (charController != null ? charController.radius : 2f) + 2.5f;

        Vector3 shinHeight = transform.position + Vector3.up * 0.5f;
        Vector3 waistHeight = transform.position + Vector3.up * 1.5f;
        Vector3 clearanceHeight = transform.position + Vector3.up * maxObstacleJumpHeight;

        bool isBlockedLow = Physics.Raycast(shinHeight, worldMoveDirection, checkDist, environmentLayer);
        bool isBlockedMid = Physics.Raycast(waistHeight, worldMoveDirection, checkDist, environmentLayer);

        if (isBlockedLow || isBlockedMid)
        {
            if (!Physics.Raycast(clearanceHeight, worldMoveDirection, checkDist + 1f, environmentLayer))
            {
                Vector3 scanDownPos = transform.position + (worldMoveDirection.normalized * checkDist) + (Vector3.up * maxObstacleJumpHeight);
                float obstacleHeight = standardJumpHeight;

                if (Physics.Raycast(scanDownPos, Vector3.down, out RaycastHit roofHit, maxObstacleJumpHeight, environmentLayer))
                {
                    obstacleHeight = roofHit.point.y - transform.position.y;
                }

                if (charController != null && charController.isGrounded && activeJumpTimer <= 0f)
                {
                    activeJumpTimer = 0.1f;

                    if (obstacleHeight > standardJumpHeight && !isConservingEnergy)
                    {
                        activeFlightTimer = 0.3f + Mathf.Lerp(0.1f, 0.6f, (obstacleHeight - standardJumpHeight) / (maxObstacleJumpHeight - standardJumpHeight));
                    }
                }
                else if (charController != null && !charController.isGrounded && activeFlightTimer <= 0f && !isConservingEnergy)
                {
                    activeFlightTimer = 0.2f;
                }
            }
        }
    }

    private void ManageEnergyHysteresis()
    {
        float reserveThreshold = stats.maxEnergy * Mathf.Lerp(0f, 0.5f, energyEfficiency / 100f);
        float recoveryTarget = Mathf.Min(stats.maxEnergy, reserveThreshold + (stats.maxEnergy * 0.20f));

        if (stats.currentEnergy <= reserveThreshold || stats.energyIsDepleted)
        {
            isConservingEnergy = true;
        }
        else if (stats.currentEnergy >= recoveryTarget)
        {
            isConservingEnergy = false;
        }

        float currentEnPercentage = (stats.currentEnergy / stats.maxEnergy) * 100f;
        if (currentEnPercentage <= approachCriticalENRate || stats.energyIsDepleted)
        {
            approachEnRecoveryLock = true;
        }
        else if (currentEnPercentage >= approachRequiredENRate)
        {
            approachEnRecoveryLock = false;
        }
    }

    private void ManageMovementRulesAndState()
    {
        float currentDistance = Vector3.Distance(transform.position, currentTarget.position);

        if (currentDistance > approachRange)
        {
            outOfRangeBufferTimer += Time.deltaTime;
            if (outOfRangeBufferTimer >= 5f)
            {
                isApproachingState = true;
            }
        }
        else
        {
            isApproachingState = false;
            outOfRangeBufferTimer = 0f;
        }
    }

    private void ExecuteApproachBehavior()
    {
        Vector3 dirToTarget = (currentTarget.position - transform.position);
        dirToTarget.y = 0f;
        Vector3 desiredWorldMoveDir = dirToTarget.normalized;

        CheckObstaclesAndJump(desiredWorldMoveDir);

        Vector3 forward = controller.lookTargetForward;
        Vector3 right = Vector3.Cross(Vector3.up, forward);

        float localX = Vector3.Dot(desiredWorldMoveDir, right);
        float localZ = Vector3.Dot(desiredWorldMoveDir, forward);

        controller.moveInput = new Vector3(localX, 0f, localZ).normalized;

        if (!approachEnRecoveryLock)
        {
            if (approachType == ApproachActionType.Boosting)
            {
                controller.isBoosting = true;
            }
            else if (approachType == ApproachActionType.Flying)
            {
                activeFlightTimer = 0.5f;
                controller.isBoosting = true;
            }
        }
        else
        {
            controller.isBoosting = false;
        }
    }

    private void ExecuteChipBehavior()
    {
        if (movementOrderList.Count == 0) return;

        MovementChip currentChip = movementOrderList[currentChipIndex];
        currentChipTimer -= Time.deltaTime;

        if (currentChipTimer <= 0f)
        {
            LoadNextChip(currentChipIndex + 1);
            currentChip = movementOrderList[currentChipIndex];
        }

        Vector3 desiredWorldMoveDir = Vector3.zero;
        float currentDistance = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), new Vector3(currentTarget.position.x, 0, currentTarget.position.z));

        float myElevatedY = transform.position.y + eyeHeight;
        float targetY = currentTarget.root.position.y + currentChip.relativeElevation;

        if (targetY > myElevatedY + 3f && !isConservingEnergy)
        {
            if (charController.isGrounded && activeJumpTimer <= 0f)
            {
                activeJumpTimer = 0.1f;
            }
            else if (!charController.isGrounded)
            {
                activeFlightTimer = 0.2f;
            }
        }

        if (currentChip.actionType == MovementActionType.Range)
        {
            float zInput = 0f;

            if (currentDistance > activeTargetRange + 5f) zInput = 1f;
            else if (currentDistance < activeTargetRange - 5f) zInput = -1f;

            if (Random.Range(0f, 100f) < 2f)
            {
                strafeDirection *= -1f;
            }

            Vector3 localIntent = new Vector3(strafeDirection, 0f, zInput).normalized;
            Vector3 forward = controller.lookTargetForward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            desiredWorldMoveDir = (forward * localIntent.z + right * localIntent.x).normalized;
        }
        else if (currentChip.actionType == MovementActionType.MoveForward)
        {
            Vector3 targetForwardFlat = currentTarget.forward;
            targetForwardFlat.y = 0f;
            targetForwardFlat.Normalize();

            Vector3 angleOffsetDir = Quaternion.Euler(0, activeTargetAngle, 0) * targetForwardFlat;
            Vector3 idealWorldPosition = currentTarget.position + (angleOffsetDir * activeTargetRange);
            idealWorldPosition.y = transform.position.y;

            Vector3 dirToIdealPos = idealWorldPosition - transform.position;

            if (dirToIdealPos.magnitude > 3f)
            {
                desiredWorldMoveDir = dirToIdealPos.normalized;
            }
        }

        if (!hasLineOfSight && currentChip.actionType == MovementActionType.MoveForward)
        {
            Vector3 right = Vector3.Cross(Vector3.up, controller.lookTargetForward);
            desiredWorldMoveDir += (right * strafeDirection);
            desiredWorldMoveDir.Normalize();
        }

        CheckObstaclesAndJump(desiredWorldMoveDir);

        if (desiredWorldMoveDir != Vector3.zero)
        {
            Vector3 forward = controller.lookTargetForward;
            Vector3 right = Vector3.Cross(Vector3.up, forward);

            float localX = Vector3.Dot(desiredWorldMoveDir, right);
            float localZ = Vector3.Dot(desiredWorldMoveDir, forward);

            controller.moveInput = new Vector3(localX, 0f, localZ).normalized;
        }
        else
        {
            controller.moveInput = Vector3.zero;
        }
    }

    private void LoadNextChip(int nextIndex)
    {
        if (movementOrderList.Count == 0) return;

        currentChipIndex = nextIndex;
        if (currentChipIndex >= movementOrderList.Count)
        {
            currentChipIndex = 0;
        }

        MovementChip chip = movementOrderList[currentChipIndex];
        currentChipTimer = chip.duration;

        activeTargetRange = chip.targetRange + Random.Range(-chip.distanceUncertainty, chip.distanceUncertainty);

        if (chip.actionType == MovementActionType.MoveForward)
        {
            activeTargetAngle = chip.targetAngle + Random.Range(-chip.angleUncertainty, chip.angleUncertainty);
        }
    }

    private void HandleRandomActions()
    {
        randomActionTickTimer -= Time.deltaTime;
        if (randomActionTickTimer <= 0f)
        {
            randomActionTickTimer = 1f;

            if (!isConservingEnergy)
            {
                if (Random.Range(0f, 100f) <= boostChance)
                {
                    activeBoostTimer = Random.Range(1f, 3f);
                }

                if (Random.Range(0f, 100f) <= quickBoostChance)
                {
                    bool isPerfect = Random.Range(0f, 100f) <= perfectQuickBoostChance;
                    controller.TriggerQuickBoost(isPerfect);
                }
            }
        }
    }

    private void ApplyFinalActions()
    {
        bool hasSafeEnergy = !isConservingEnergy;

        bool shouldHoldJump = false;

        if (activeJumpTimer > 0f)
        {
            activeJumpTimer -= Time.deltaTime;
            shouldHoldJump = true;
        }

        if (activeFlightTimer > 0f)
        {
            activeFlightTimer -= Time.deltaTime;
            if (hasSafeEnergy)
            {
                shouldHoldJump = true;
            }
            else
            {
                activeFlightTimer = 0f;
            }
        }

        controller.isJumping = shouldHoldJump;

        if (!isApproachingState)
        {
            if (activeBoostTimer > 0f)
            {
                activeBoostTimer -= Time.deltaTime;
            }

            bool intendsToBoost = (activeBoostTimer > 0f) || (shouldHoldJump && !charController.isGrounded);

            if (intendsToBoost && hasSafeEnergy)
            {
                controller.isBoosting = true;
            }
            else
            {
                controller.isBoosting = false;
                activeBoostTimer = 0f;
            }
        }
    }
}