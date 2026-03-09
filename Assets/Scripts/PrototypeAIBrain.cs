using UnityEngine;

[RequireComponent(typeof(MechController))]
public class PrototypeAIBrain : MonoBehaviour
{
    private MechController controller;
    private CharacterController charController;
    private MechStats stats;

    [Header("Aiming & Vision")]
    public Transform cameraPivot;
    public float aimTrackingSpeed = 15f;

    [Header("Targeting")]
    public LayerMask targetLayer;
    public float detectionRadius = 400f;
    public Transform currentTarget;

    [Header("Behavior & Positioning")]
    public float preferredDistance = 200f;
    public float distanceMargin = 20f;

    [Tooltip("Minimum time before the AI changes its circling direction.")]
    public float minDirectionSwapInterval = 2f;
    [Tooltip("Maximum time before the AI changes its circling direction.")]
    public float maxDirectionSwapInterval = 6f;

    [Header("Action Probabilities")]
    public float decisionInterval = 1f;
    [Range(0f, 100f)] public float boostChance = 40f;
    [Range(0f, 100f)] public float jumpChance = 15f;
    [Range(0f, 100f)] public float flightChance = 25f;

    [Header("Energy Management")]
    [Tooltip("0 = Brainless (burns energy until depleted). 100 = Tactical (always maintains a safe reserve).")]
    [Range(0f, 100f)] public float energyEfficiency = 80f;

    // --- Internal Timers ---
    private float swapTimer;
    private float decisionTimer;
    private float strafeDirection = 1f;

    // --- Action Execution Timers ---
    private float activeBoostTimer = 0f;
    private float activeJumpTimer = 0f;
    private float activeFlightTimer = 0f;

    void Start()
    {
        controller = GetComponent<MechController>();
        charController = GetComponent<CharacterController>();
        stats = GetComponent<MechStats>();

        // Start with a randomized swap timer
        swapTimer = Random.Range(minDirectionSwapInterval, maxDirectionSwapInterval);
        decisionTimer = decisionInterval;
    }

    void Update()
    {
        if (Time.timeScale == 0f) return;

        FindTargetContinuously();

        if (currentTarget == null)
        {
            controller.moveInput = Vector3.zero;
            controller.isBoosting = false;
            controller.isJumping = false;
            return;
        }

        HandleAimingAndMovement();
        HandleDecisionMaking();
        ApplyActions();
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

    private void HandleAimingAndMovement()
    {
        Vector3 dirToTarget3D = currentTarget.position - transform.position;

        // 1. HORIZONTAL BODY ROTATION
        Vector3 dirToTargetFlat = dirToTarget3D;
        dirToTargetFlat.y = 0f;

        if (dirToTargetFlat.sqrMagnitude > 0.1f)
        {
            controller.lookTargetForward = dirToTargetFlat.normalized;
        }

        // 2. TRUE 3D AIMING
        if (cameraPivot != null && dirToTarget3D.sqrMagnitude > 0.1f)
        {
            Quaternion targetPivotRot = Quaternion.LookRotation(dirToTarget3D.normalized);
            cameraPivot.rotation = Quaternion.Slerp(cameraPivot.rotation, targetPivotRot, Time.deltaTime * aimTrackingSpeed);
        }

        // 3. DISTANCE & STRAFING
        float currentDistance = Vector3.Distance(transform.position, currentTarget.position);
        float zInput = 0f;

        // Approach if too far, back off if too close
        if (currentDistance > preferredDistance + distanceMargin) zInput = 1f;
        else if (currentDistance < preferredDistance - distanceMargin) zInput = -1f;

        swapTimer -= Time.deltaTime;
        if (swapTimer <= 0f)
        {
            strafeDirection *= -1f;
            // NEW: Randomize the next direction swap time!
            swapTimer = Random.Range(minDirectionSwapInterval, maxDirectionSwapInterval);
        }

        float xInput = strafeDirection;

        controller.moveInput = new Vector3(xInput, 0f, zInput).normalized;
    }

    private void HandleDecisionMaking()
    {
        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0f)
        {
            decisionTimer = decisionInterval;

            float reserveThreshold = stats.maxEnergy * Mathf.Lerp(0f, 0.5f, energyEfficiency / 100f);
            bool hasSufficientEnergy = stats.currentEnergy > reserveThreshold && !stats.energyIsDepleted;

            if (hasSufficientEnergy && Random.Range(0f, 100f) <= boostChance)
            {
                activeBoostTimer = Random.Range(1f, 3f);
            }

            if (charController != null && charController.isGrounded)
            {
                if (!stats.energyIsDepleted && Random.Range(0f, 100f) <= jumpChance)
                {
                    activeJumpTimer = 0.5f;
                }
            }
            else if (charController != null && !charController.isGrounded)
            {
                if (hasSufficientEnergy && Random.Range(0f, 100f) <= flightChance)
                {
                    activeFlightTimer = Random.Range(1f, 2.5f);
                }
            }
        }
    }

    private void ApplyActions()
    {
        float reserveThreshold = stats.maxEnergy * Mathf.Lerp(0f, 0.5f, energyEfficiency / 100f);
        bool hasSufficientEnergy = stats.currentEnergy > reserveThreshold && !stats.energyIsDepleted;

        // --- BOOST APPLICATION ---
        if (activeBoostTimer > 0f)
        {
            activeBoostTimer -= Time.deltaTime;
            controller.isBoosting = hasSufficientEnergy;

            if (!hasSufficientEnergy) activeBoostTimer = 0f;
        }
        else
        {
            // If the AI is falling heavily behind, override probabilities and sprint to catch up
            float currentDist = Vector3.Distance(transform.position, currentTarget.position);
            bool needsToCatchUp = currentDist > preferredDistance + 50f;
            controller.isBoosting = needsToCatchUp && hasSufficientEnergy;
        }

        // --- FLIGHT & JUMP APPLICATION ---
        bool shouldHoldJump = false;

        if (activeJumpTimer > 0f)
        {
            activeJumpTimer -= Time.deltaTime;
            shouldHoldJump = true;
        }

        if (activeFlightTimer > 0f)
        {
            activeFlightTimer -= Time.deltaTime;

            if (hasSufficientEnergy)
            {
                shouldHoldJump = true;
            }
            else
            {
                activeFlightTimer = 0f;
            }
        }

        controller.isJumping = shouldHoldJump;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}