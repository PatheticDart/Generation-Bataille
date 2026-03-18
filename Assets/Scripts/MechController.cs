using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(CharacterController), typeof(MechStats))]
public class MechController : MonoBehaviour
{
    private CharacterController controller;
    private MechStats stats;
    private PartSystem partSystem;
    private Animator animator;

    [Header("Visuals")]
    public Transform mechBody;

    [Header("Targeting & Rotation")]
    [Tooltip("Drag your FCSLockbox here so the mech chassis chases its rotation.")]
    public Transform fcsLockBox;

    [Header("Input (Driven by Player or AI)")]
    public Vector3 moveInput;
    public Vector3 lookTargetForward;
    public bool isBoosting;
    public bool isJumping;

    [Header("Movement Settings")]
    public bool restrictTo8Directions = true;

    [Header("Camera & Effects")]
    public CameraEffects cameraEffects;

    [Header("Quick Boost State")]
    public bool isQuickBoosting = false;
    public bool isPerfectQuickBoosting = false;
    private float currentQBDuration = 0f;
    private float currentQBCooldown = 0f;
    private Vector3 qbDirection;
    private Vector3 worldQBDirection;

    private List<GameObject> leftQBThrusters = new List<GameObject>();
    private List<GameObject> rightQBThrusters = new List<GameObject>();
    private List<GameObject> frontQBThrusters = new List<GameObject>();
    private List<GameObject> rearQBThrusters = new List<GameObject>();

    [Header("Jump & Landing States")]
    public bool isPreparingToJump { get; private set; }
    private float currentJumpDelayTimer = 0f;

    private float verticalVelocity;
    private Vector3 currentHorizontalVelocity;

    public bool isRecoveringFromLanding { get; private set; }
    public bool isBraking { get; private set; } // NEW: Differentiates a Brake from a Vertical Drop
    private float recoveryTimer = 0f;

    private Vector3 lastActiveMoveInput;

    [Header("Buffers & Timings")]
    public float thrusterBufferTime = 0.1f;
    private float lastMoveInputTime = -10f;

    public float bunnyHopWindow = 0.2f;
    private float lastJumpOrLandTime = -10f;

    public float boostToJumpDelay = 0.25f;
    private float boostStartTime = -10f;
    private float boostEndTime = -10f;
    private bool wasActuallyBoostingLastFrame = false;

    public float walkToJumpDelay = 0.15f;
    private float walkStartTime = -10f;
    private bool wasActuallyWalkingLastFrame = false;

    public float jumpInputBufferTime = 0.2f;
    private float lastJumpInputTime = -10f;

    // NEW: Braking Timers
    private bool pendingBrake = false;
    private float timeSinceStoppedBoosting = 0f;

    public bool HasRecentMovementInput => Time.time <= lastMoveInputTime + thrusterBufferTime;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        stats = GetComponent<MechStats>();
        partSystem = GetComponent<PartSystem>();
        animator = GetComponentInChildren<Animator>();

        FindQBThrusters(transform);
    }

    private void FindQBThrusters(Transform parent)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains("QBThrusterNodeLeft")) AddThrusterChildren(child, leftQBThrusters);
            else if (child.name.Contains("QBThrusterNodeRight")) AddThrusterChildren(child, rightQBThrusters);
            else if (child.name.Contains("QBThrusterNodeFront")) AddThrusterChildren(child, frontQBThrusters);
            else if (child.name.Contains("BoosterNode"))
            {
                Transform rearT = FindDeepChild(child, "QBThruster");
                if (rearT != null) rearQBThrusters.Add(rearT.gameObject);
            }

            FindQBThrusters(child);
        }
    }

    private void AddThrusterChildren(Transform parent, List<GameObject> list)
    {
        foreach (Transform child in parent) list.Add(child.gameObject);
        if (parent.childCount == 0) list.Add(parent.gameObject);
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name)) return child;
            Transform result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    public void TriggerQuickBoost(bool isPerfect)
    {
        if (currentQBCooldown > 0f || stats.energyIsDepleted) return;

        Vector3 inputDir = moveInput.sqrMagnitude > 0.1f ? moveInput.normalized : new Vector3(0, 0, 1);

        if (Mathf.Abs(inputDir.x) > Mathf.Abs(inputDir.z))
            qbDirection = new Vector3(Mathf.Sign(inputDir.x), 0, 0);
        else
            qbDirection = new Vector3(0, 0, Mathf.Sign(inputDir.z));

        bool isWalkingState = controller.isGrounded && !isBoosting;
        Vector3 refForward = (isWalkingState && mechBody != null) ? mechBody.forward : (fcsLockBox != null ? fcsLockBox.forward : lookTargetForward);
        if (refForward == Vector3.zero) refForward = transform.forward;
        refForward.y = 0f; refForward.Normalize();
        Vector3 refRight = Vector3.Cross(Vector3.up, refForward).normalized;

        worldQBDirection = (refForward * qbDirection.z + refRight * qbDirection.x).normalized;

        if (stats.ConsumeEnergy(stats.qbEnergyDrain))
        {
            isQuickBoosting = true;
            isPerfectQuickBoosting = isPerfect;
            currentQBDuration = stats.qbDuration;
            currentQBCooldown = stats.qbReloadTime;

            FireVisualThrusters(qbDirection);

            if (animator != null)
            {
                animator.SetBool("IsQuickBoosting", true);
                animator.SetBool("IsPerfectQuickBoosting", isPerfect);
                animator.SetFloat("QBX", qbDirection.x);
                animator.SetFloat("QBY", qbDirection.z);
            }
        }
    }

    private void FireVisualThrusters(Vector3 dir)
    {
        if (dir.x > 0.5f) StartCoroutine(FlashThrusters(leftQBThrusters));
        else if (dir.x < -0.5f) StartCoroutine(FlashThrusters(rightQBThrusters));
        else if (dir.z < -0.5f) StartCoroutine(FlashThrusters(frontQBThrusters));
        else if (dir.z > 0.5f) StartCoroutine(FlashThrusters(rearQBThrusters));
    }

    private IEnumerator FlashThrusters(List<GameObject> thrusters)
    {
        foreach (var t in thrusters) if (t != null) t.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        foreach (var t in thrusters) if (t != null) t.SetActive(false);
    }

    void Update()
    {
        HandleBodyRotation();
        ApplyMovement();

        if (currentQBCooldown > 0f) currentQBCooldown -= Time.deltaTime;
    }

    private void HandleBodyRotation()
    {
        Vector3 targetForward = fcsLockBox != null ? fcsLockBox.forward : lookTargetForward;
        targetForward.y = 0f;

        if (targetForward.sqrMagnitude > 0.01f && mechBody != null)
        {
            Quaternion targetRot = Quaternion.LookRotation(targetForward.normalized);
            mechBody.rotation = Quaternion.Slerp(mechBody.rotation, targetRot, stats.turnSpeed * Time.deltaTime);
        }
    }

    private void ApplyMovement()
    {
        if (isQuickBoosting)
        {
            currentQBDuration -= Time.deltaTime;
            if (currentQBDuration <= 0f)
            {
                isQuickBoosting = false;
                isPerfectQuickBoosting = false;
                if (animator != null)
                {
                    animator.SetBool("IsQuickBoosting", false);
                    animator.SetBool("IsPerfectQuickBoosting", false);
                }
            }
        }

        Vector3 currentMoveInput = moveInput;
        bool currentIsJumping = isJumping;
        bool currentIsBoosting = isBoosting;

        bool isWalkingState = controller.isGrounded && !currentIsBoosting;

        if (restrictTo8Directions && isWalkingState && currentMoveInput.magnitude > 0.1f)
        {
            float angle = Mathf.Atan2(currentMoveInput.x, currentMoveInput.z) * Mathf.Rad2Deg;
            angle = Mathf.Round(angle / 45f) * 45f;
            float mag = currentMoveInput.magnitude;
            currentMoveInput = new Vector3(Mathf.Sin(angle * Mathf.Deg2Rad) * mag, 0f, Mathf.Cos(angle * Mathf.Deg2Rad) * mag);
            moveInput = currentMoveInput;
        }

        if (currentMoveInput.magnitude > 0.1f)
        {
            lastMoveInputTime = Time.time;
            lastActiveMoveInput = currentMoveInput;
        }

        Vector3 effectiveMoveInput = (currentMoveInput.magnitude > 0.1f) ? currentMoveInput : (HasRecentMovementInput ? lastActiveMoveInput : Vector3.zero);

        // --- RECOVERY & BRAKING LOCK ---
        if (isRecoveringFromLanding)
        {
            recoveryTimer -= Time.deltaTime;
            if (recoveryTimer <= 0)
            {
                isRecoveringFromLanding = false;
                isBraking = false;
            }
            else
            {
                effectiveMoveInput = Vector3.zero;
                currentIsJumping = false;
                currentIsBoosting = false;
                isPreparingToJump = false;
            }
        }

        bool canHorizontalBoost = currentIsBoosting && !stats.energyIsDepleted && (effectiveMoveInput.magnitude > 0 || !controller.isGrounded);

        // --- BOOST STOP (BRAKE) DETECTION ---
        bool isActuallyBoostingOnGround = canHorizontalBoost && controller.isGrounded;
        bool isActuallyQuickBoostingOnGround = isQuickBoosting && controller.isGrounded;

        if (isActuallyBoostingOnGround || isActuallyQuickBoostingOnGround)
        {
            pendingBrake = true;
            timeSinceStoppedBoosting = 0f;
        }
        else if (pendingBrake)
        {
            if (!controller.isGrounded)
            {
                pendingBrake = false; // Cancel brake if we walk off a ledge or jump
            }
            else
            {
                timeSinceStoppedBoosting += Time.deltaTime;
                if (timeSinceStoppedBoosting >= stats.brakeBufferTime)
                {
                    // Trigger Brake!
                    isRecoveringFromLanding = true;
                    isBraking = true; // Flags it so we use the sliding friction instead of hard landing friction
                    isPreparingToJump = false;
                    lastJumpOrLandTime = -100f;

                    float speedFactor = Mathf.Clamp01(currentHorizontalVelocity.magnitude / stats.boostHorizontalSpeed);
                    float weightFactor = stats.totalWeight / stats.baselineWeight;

                    recoveryTimer = Mathf.Clamp(Mathf.Lerp(stats.baseBrakeTime, stats.maxBrakeTime, speedFactor) * weightFactor, stats.baseBrakeTime, stats.maxBrakeTime);

                    if (cameraEffects != null)
                    {
                        float shakeSeverity = Mathf.Lerp(0.5f, 1.5f, speedFactor) * weightFactor;
                        cameraEffects.TriggerImpactShake(shakeSeverity);
                    }

                    pendingBrake = false;
                }
            }
        }

        // --- TRACKING FOR JUMP DELAYS ---
        if (isActuallyBoostingOnGround && !wasActuallyBoostingLastFrame) boostStartTime = Time.time;
        else if (!isActuallyBoostingOnGround && wasActuallyBoostingLastFrame) boostEndTime = Time.time;
        wasActuallyBoostingLastFrame = isActuallyBoostingOnGround;

        bool isActuallyWalkingOnGround = !canHorizontalBoost && controller.isGrounded && (effectiveMoveInput.magnitude > 0);
        if (isActuallyWalkingOnGround && !wasActuallyWalkingLastFrame) walkStartTime = Time.time;
        wasActuallyWalkingLastFrame = isActuallyWalkingOnGround;

        float currentWalkSpeed = stats.walkSpeed;
        if (controller.isGrounded && effectiveMoveInput.z < -0.1f) currentWalkSpeed *= (1f - stats.backwardSpeedPenalty);

        float targetSpeed = canHorizontalBoost ? stats.boostHorizontalSpeed : currentWalkSpeed;

        Vector3 referenceForward;
        Vector3 referenceRight;

        if (isWalkingState && mechBody != null)
        {
            referenceForward = mechBody.forward;
            referenceRight = mechBody.right;
            referenceForward.y = 0f; referenceRight.y = 0f;
            referenceForward.Normalize(); referenceRight.Normalize();
        }
        else
        {
            referenceForward = fcsLockBox != null ? fcsLockBox.forward : lookTargetForward;
            if (referenceForward == Vector3.zero) referenceForward = transform.forward;
            referenceForward.y = 0f; referenceForward.Normalize();
            referenceRight = Vector3.Cross(Vector3.up, referenceForward).normalized;
        }

        Vector3 targetDirection = (referenceForward * effectiveMoveInput.z + referenceRight * effectiveMoveInput.x).normalized;
        Vector3 targetVelocity = targetDirection * targetSpeed;

        // --- ACCELERATION & SLIDING ---
        if (isRecoveringFromLanding && controller.isGrounded)
        {
            // Dynamically select the friction based on if we are Braking or vertically Hard Landing
            float slideFriction = isBraking ? stats.brakeSlideDeceleration : stats.hardLandingSlideDeceleration;
            currentHorizontalVelocity = Vector3.Lerp(currentHorizontalVelocity, Vector3.zero, slideFriction * Time.deltaTime);
        }
        else
        {
            float accelRate;
            if (!controller.isGrounded) accelRate = (effectiveMoveInput.magnitude > 0) ? stats.airAcceleration : stats.airDeceleration;
            else accelRate = effectiveMoveInput.magnitude > 0 ? (canHorizontalBoost ? stats.boostAcceleration : stats.walkAcceleration) : (canHorizontalBoost ? stats.boostDeceleration : stats.walkDeceleration);

            if (isQuickBoosting)
            {
                float thrust = isPerfectQuickBoosting ? stats.qbThrust : (stats.qbThrust * 0.70f);
                Vector3 combinedTargetVelocity = targetVelocity + (worldQBDirection * thrust);
                float qbAccelRate = thrust / Mathf.Max(stats.qbDuration, 0.01f);
                currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, combinedTargetVelocity, qbAccelRate * Time.deltaTime);
            }
            else
            {
                currentHorizontalVelocity = Vector3.MoveTowards(currentHorizontalVelocity, targetVelocity, accelRate * Time.deltaTime);
            }
        }

        bool energyUsedThisFrame = false;

        if (controller.isGrounded)
        {
            verticalVelocity = -2f;

            if (currentIsJumping) lastJumpInputTime = Time.time;
            bool hasBufferedJump = (Time.time <= lastJumpInputTime + jumpInputBufferTime);
            bool canBunnyHop = (Time.time <= lastJumpOrLandTime + bunnyHopWindow);

            bool isBoostJumpLocked = isActuallyBoostingOnGround && (Time.time < boostStartTime + boostToJumpDelay);
            bool isWalkJumpLocked = isActuallyWalkingOnGround && (Time.time < walkStartTime + walkToJumpDelay);
            bool isDeceleratingFromBoostLocked = !isActuallyBoostingOnGround && (Time.time < boostEndTime + walkToJumpDelay);

            bool isMovementJumpLocked = isBoostJumpLocked || isWalkJumpLocked || isDeceleratingFromBoostLocked;

            if (hasBufferedJump && !isPreparingToJump)
            {
                if (canBunnyHop)
                {
                    lastJumpInputTime = -10f;
                    verticalVelocity = stats.jumpForce;
                    lastJumpOrLandTime = Time.time;
                    isPreparingToJump = false;
                }
                else if (!isMovementJumpLocked)
                {
                    lastJumpInputTime = -10f;
                    isPreparingToJump = true;
                    currentJumpDelayTimer = stats.jumpDelay;
                }
            }

            if (isPreparingToJump)
            {
                currentJumpDelayTimer -= Time.deltaTime;
                if (currentJumpDelayTimer <= 0f)
                {
                    verticalVelocity = stats.jumpForce;
                    lastJumpOrLandTime = Time.time;
                    isPreparingToJump = false;
                }
            }
        }
        else
        {
            isPreparingToJump = false;

            if (currentIsJumping && !stats.energyIsDepleted)
            {
                verticalVelocity += (stats.boostVerticalSpeed * 2f) * Time.deltaTime;
                if (verticalVelocity > stats.boostVerticalSpeed) verticalVelocity = stats.boostVerticalSpeed;
                energyUsedThisFrame = true;
            }
            else
            {
                float weightFactor = stats.totalWeight / stats.baselineWeight;
                verticalVelocity -= 9.81f * weightFactor * 15f * Time.deltaTime;
            }
        }

        if (canHorizontalBoost && effectiveMoveInput.magnitude > 0) energyUsedThisFrame = true;
        if (energyUsedThisFrame) stats.ConsumeEnergy(stats.boostEnergyDrain * Time.deltaTime);

        Vector3 finalMove = new Vector3(currentHorizontalVelocity.x, verticalVelocity, currentHorizontalVelocity.z);
        bool wasGroundedBeforeMove = controller.isGrounded;
        controller.Move(finalMove * Time.deltaTime);

        // --- VERTICAL HARD LANDING IMPACT ---
        if (!wasGroundedBeforeMove && controller.isGrounded)
        {
            lastJumpOrLandTime = Time.time;

            if (verticalVelocity <= stats.minHardLandingThreshold)
            {
                isRecoveringFromLanding = true;
                isBraking = false; // Overrides a brake with a true Hard Landing
                isPreparingToJump = false;

                lastJumpOrLandTime = -100f;

                float weightFactor = stats.totalWeight / stats.baselineWeight;
                float speedFactor = Mathf.InverseLerp(stats.minHardLandingThreshold, stats.maxHardLandingThreshold, verticalVelocity);

                float baseCalculatedTime = Mathf.Lerp(stats.baseHardLandingTime, stats.maxHardLandingTime, speedFactor);
                float calculatedTime = baseCalculatedTime * weightFactor;
                recoveryTimer = Mathf.Clamp(calculatedTime, stats.baseHardLandingTime, stats.maxHardLandingTime);

                if (cameraEffects != null)
                {
                    float shakeSeverity = Mathf.Lerp(1.0f, 3.0f, speedFactor) * weightFactor;
                    cameraEffects.TriggerImpactShake(shakeSeverity);
                }
            }
        }

        if (partSystem != null)
        {
            partSystem.ToggleThrusters(energyUsedThisFrame);
        }
    }
}