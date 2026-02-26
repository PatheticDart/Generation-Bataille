using UnityEngine;
using UnityEngine.UI;

public class FCSLockBox : MonoBehaviour
{
    [Header("FCS Stats (Loaded from MechLoader)")]
    [Tooltip("The horizontal FOV of the lockbox in degrees.")]
    public float fcsWidth = 45f;
    [Tooltip("The vertical FOV of the lockbox in degrees.")]
    public float fcsHeight = 30f;
    [Tooltip("Maximum lock-on range in meters.")]
    public float fcsRange = 300f;
    [Tooltip("Time in seconds to transition from Soft Lock to Hard Lock.")]
    public float lockSpeed = 1.5f;
    [Tooltip("How fast the FCS box rotates to catch up to the camera/aim direction.")]
    public float fcsTurnRate = 10f;

    [Header("Mechanics")]
    [Tooltip("If the angle between the camera and FCS is less than this, the box snaps to the center.")]
    public float stickyThreshold = 2f;
    public LayerMask targetLayer;
    public bool isPlayer = true; // Set to false for AI

    [Header("External References")]
    [Tooltip("For Player: CameraPivot. For AI: The direction the AI wants to look.")]
    public Transform aimMaster; 
    public Camera mainCamera;

    [Header("UI Elements (Player Only)")]
    public RectTransform lockBoxUI; // The Orange Box
    public RectTransform reticleUI; // The Target Crosshair
    public Image reticleImage;

    // State Variables
    public Transform currentTarget { get; private set; }
    public bool isSoftLocked { get; private set; }
    public bool isHardLocked { get; private set; }
    private float currentLockTimer = 0f;

    void Start()
    {
        // Setup initial UI size based on FCS stats
        if (isPlayer && lockBoxUI != null)
        {
            // Rough conversion from degrees to screen pixels (adjust multiplier as needed for your resolution)
            lockBoxUI.sizeDelta = new Vector2(fcsWidth * 15f, fcsHeight * 15f);
        }
    }

    void Update()
    {
        UpdateFCSTrailingRotation();
        DetectAndManageTargets();
        
        if (isPlayer)
        {
            UpdateUI();
        }
    }

    private void UpdateFCSTrailingRotation()
    {
        if (aimMaster == null) return;

        // Calculate how far behind the FCS is from the master aim direction
        float angleDifference = Quaternion.Angle(transform.rotation, aimMaster.rotation);

        if (angleDifference <= stickyThreshold)
        {
            // STICKY: It's close enough, snap to center
            transform.rotation = aimMaster.rotation;
        }
        else
        {
            // CHASING: The camera moved too fast, Slerp to catch up
            transform.rotation = Quaternion.Slerp(transform.rotation, aimMaster.rotation, fcsTurnRate * Time.deltaTime);
        }
    }

    private void DetectAndManageTargets()
    {
        // 1. Find all potential targets in range
        Collider[] potentialTargets = Physics.OverlapSphere(transform.position, fcsRange, targetLayer);
        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;

        // 2. The 3D Frustum Check
        foreach (Collider col in potentialTargets)
        {
            Transform targetTransform = col.transform;
            
            // Convert target position to the FCS Pivot's local space
            Vector3 localPos = transform.InverseTransformPoint(targetTransform.position);

            // Is it in front of us?
            if (localPos.z > 0)
            {
                // Calculate horizontal and vertical angles
                float angleX = Mathf.Abs(Mathf.Atan2(localPos.x, localPos.z) * Mathf.Rad2Deg);
                float angleY = Mathf.Abs(Mathf.Atan2(localPos.y, localPos.z) * Mathf.Rad2Deg);

                // Is it inside our specific FCS Width and Height bounds?
                if (angleX <= fcsWidth / 2f && angleY <= fcsHeight / 2f)
                {
                    // It is inside the 3D Frustum! Find the closest one.
                    float dist = localPos.z;
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        bestTarget = targetTransform;
                    }
                }
            }
        }

        // 3. Manage Lock States
        if (bestTarget != null)
        {
            if (currentTarget != bestTarget)
            {
                // New target acquired: Reset to Soft Lock
                currentTarget = bestTarget;
                isSoftLocked = true;
                isHardLocked = false;
                currentLockTimer = 0f;
            }
            else
            {
                // Maintain lock, progress to Hard Lock
                if (!isHardLocked)
                {
                    currentLockTimer += Time.deltaTime;
                    if (currentLockTimer >= lockSpeed)
                    {
                        isHardLocked = true;
                    }
                }
            }
        }
        else
        {
            // Target lost or left the box
            currentTarget = null;
            isSoftLocked = false;
            isHardLocked = false;
            currentLockTimer = 0f;
        }
    }

    private void UpdateUI()
    {
        if (mainCamera == null || lockBoxUI == null || reticleUI == null) return;

        // 1. Move the Orange Lockbox to match the trailing FCSPivot
        // We project a point far forward from the pivot to anchor the 2D UI
        Vector3 projectionPoint = transform.position + transform.forward * 100f;
        Vector2 screenPos = mainCamera.WorldToScreenPoint(projectionPoint);
        lockBoxUI.position = screenPos;

        // 2. Handle the Reticle
        if (currentTarget != null)
        {
            reticleUI.gameObject.SetActive(true);
            
            // Move reticle to enemy
            Vector2 enemyScreenPos = mainCamera.WorldToScreenPoint(currentTarget.position);
            reticleUI.position = enemyScreenPos;

            // Update color based on lock state
            reticleImage.color = isHardLocked ? Color.red : Color.green;
        }
        else
        {
            reticleUI.gameObject.SetActive(false);
        }
    }

    // Weapons will call this function to know where to shoot
    public Vector3 GetAimPosition(float projectileSpeed = 0f)
    {
        if (currentTarget == null) return transform.position + transform.forward * 100f;

        if (isHardLocked && projectileSpeed > 0f)
        {
            // HARD LOCK: Predict target position based on velocities
            // (We will implement the kinematic math here later when weapons are built)
            return currentTarget.position; // Stubbed for now
        }
        else
        {
            // SOFT LOCK: Just aim at their current exact position
            return currentTarget.position;
        }
    }
}