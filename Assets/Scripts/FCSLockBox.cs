using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DefaultExecutionOrder(1000)]
public class FCSLockBox : MonoBehaviour
{
    [Header("FCS Stats (Loaded from MechLoader)")]
    public float fcsWidth = 45f;
    public float fcsHeight = 30f;
    public float fcsRange = 300f;
    public float lockSpeed = 1.5f;
    public float fcsTurnRate = 10f;

    [Header("Colors")]
    public Color softLockColor = Color.green;
    public Color hardLockColor = Color.red; // Kept separate so it always turns red on lock

    [Header("Mechanics")]
    public float stickyThreshold = 2f;
    public LayerMask targetLayer;
    public bool isPlayer = true;

    [Header("External References")]
    public Transform aimMaster;
    public Camera mainCamera;

    [Header("UI Elements (Player Only)")]
    public RectTransform lockBoxUI;
    public RectTransform reticleUI;
    [Tooltip("Drag your TMPro text object here.")]
    public TextMeshProUGUI rangefinderText;

    private Image[] reticleChildImages;
    private TextMeshProUGUI[] reticleChildTexts;

    public Transform currentTarget { get; private set; }
    public bool isSoftLocked { get; private set; }
    public bool isHardLocked { get; private set; }
    private float currentLockTimer = 0f;

    void Start()
    {
        if (isPlayer && lockBoxUI != null)
        {
            lockBoxUI.sizeDelta = new Vector2(fcsWidth * 15f, fcsHeight * 15f);
        }

        if (isPlayer && reticleUI != null)
        {
            reticleChildImages = reticleUI.GetComponentsInChildren<Image>(true);
            reticleChildTexts = reticleUI.GetComponentsInChildren<TextMeshProUGUI>(true);
        }
    }

    void LateUpdate()
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

        float angleDifference = Quaternion.Angle(transform.rotation, aimMaster.rotation);

        if (angleDifference <= stickyThreshold)
        {
            transform.rotation = aimMaster.rotation;
        }
        else
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, aimMaster.rotation, fcsTurnRate * Time.deltaTime);
        }
    }

    private void DetectAndManageTargets()
    {
        Vector3 origin = (isPlayer && mainCamera != null) ? mainCamera.transform.position : transform.position;

        // --- THE OVERSIZED PHYSICS SPHERE ---
        float oversizedPhysicsRadius = fcsRange * 1.5f;
        Collider[] potentialTargets = Physics.OverlapSphere(origin, oversizedPhysicsRadius, targetLayer);

        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in potentialTargets)
        {
            Transform targetTransform = col.transform;

            // --- THE STRICT MATH FILTER ---
            Vector3 distanceOrigin = (aimMaster != null) ? aimMaster.position : transform.position;
            float distanceToTarget = Vector3.Distance(distanceOrigin, targetTransform.position);

            if (distanceToTarget > fcsRange)
            {
                continue;
            }

            Vector3 toTarget = targetTransform.position - origin;
            Vector3 localPos = Quaternion.Inverse(transform.rotation) * toTarget;

            if (localPos.z > 0)
            {
                float angleX = Mathf.Abs(Mathf.Atan2(localPos.x, localPos.z) * Mathf.Rad2Deg);
                float angleY = Mathf.Abs(Mathf.Atan2(localPos.y, localPos.z) * Mathf.Rad2Deg);

                if (angleX <= fcsWidth / 2f && angleY <= fcsHeight / 2f)
                {
                    float dist = localPos.z;
                    if (dist < closestDistance)
                    {
                        closestDistance = dist;
                        bestTarget = targetTransform;
                    }
                }
            }
        }

        if (bestTarget != null)
        {
            if (currentTarget != bestTarget)
            {
                currentTarget = bestTarget;
                isSoftLocked = true;
                isHardLocked = false;
                currentLockTimer = 0f;
            }
            else
            {
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
            currentTarget = null;
            isSoftLocked = false;
            isHardLocked = false;
            currentLockTimer = 0f;
        }
    }

    private void UpdateUI()
    {
        if (mainCamera == null || lockBoxUI == null || reticleUI == null) return;

        RectTransform canvasRect = (RectTransform)lockBoxUI.parent;

        Vector3 origin = mainCamera.transform.position;
        Vector3 projectionPoint = origin + transform.forward * 100f;
        Vector3 boxScreenPos = mainCamera.WorldToScreenPoint(projectionPoint);

        if (boxScreenPos.z > 0)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, boxScreenPos, mainCamera, out Vector2 boxLocalPos);
            lockBoxUI.localPosition = boxLocalPos;
        }

        if (currentTarget != null)
        {
            Vector3 enemyScreenPos = mainCamera.WorldToScreenPoint(currentTarget.position);

            if (enemyScreenPos.z > 0)
            {
                reticleUI.gameObject.SetActive(true);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, enemyScreenPos, mainCamera, out Vector2 reticleLocalPos);
                reticleUI.localPosition = reticleLocalPos;

                // --- NEW: Using the exposed variables for customization ---
                Color targetColor = isHardLocked ? hardLockColor : softLockColor;

                foreach (Image img in reticleChildImages)
                {
                    if (img != null) img.color = targetColor;
                }

                foreach (TextMeshProUGUI txt in reticleChildTexts)
                {
                    if (txt != null) txt.color = targetColor;
                }

                if (rangefinderText != null)
                {
                    Vector3 distanceOrigin = (aimMaster != null) ? aimMaster.position : transform.position;
                    float distanceToTarget = Vector3.Distance(distanceOrigin, currentTarget.position);
                    rangefinderText.text = $"{distanceToTarget:F0}m";
                }
            }
            else
            {
                reticleUI.gameObject.SetActive(false);
            }
        }
        else
        {
            reticleUI.gameObject.SetActive(false);
        }
    }

    // --- GIZMO DRAWING LOGIC ---
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

        // Calculate the origin point to match the detection logic
        Vector3 origin = transform.position;
        if (Application.isPlaying && isPlayer && mainCamera != null)
        {
            origin = mainCamera.transform.position;
        }
        else if (!Application.isPlaying && isPlayer && mainCamera != null)
        {
            origin = mainCamera.transform.position;
        }

        float halfWidth = fcsWidth / 2f;
        float halfHeight = fcsHeight / 2f;

        // Calculate the local direction of the 4 corners of the frustum
        Vector3 topLeftLocal = Quaternion.Euler(-halfHeight, -halfWidth, 0) * Vector3.forward * fcsRange;
        Vector3 topRightLocal = Quaternion.Euler(-halfHeight, halfWidth, 0) * Vector3.forward * fcsRange;
        Vector3 bottomLeftLocal = Quaternion.Euler(halfHeight, -halfWidth, 0) * Vector3.forward * fcsRange;
        Vector3 bottomRightLocal = Quaternion.Euler(halfHeight, halfWidth, 0) * Vector3.forward * fcsRange;

        // Convert the local corners to world space based on the FCS rotation
        Vector3 topLeftWorld = origin + transform.rotation * topLeftLocal;
        Vector3 topRightWorld = origin + transform.rotation * topRightLocal;
        Vector3 bottomLeftWorld = origin + transform.rotation * bottomLeftLocal;
        Vector3 bottomRightWorld = origin + transform.rotation * bottomRightLocal;

        // Draw the frustum lines extending from the origin
        Gizmos.DrawLine(origin, topLeftWorld);
        Gizmos.DrawLine(origin, topRightWorld);
        Gizmos.DrawLine(origin, bottomLeftWorld);
        Gizmos.DrawLine(origin, bottomRightWorld);

        // Draw the far plane rectangle (the end of the locking box)
        Gizmos.DrawLine(topLeftWorld, topRightWorld);
        Gizmos.DrawLine(topRightWorld, bottomRightWorld);
        Gizmos.DrawLine(bottomRightWorld, bottomLeftWorld);
        Gizmos.DrawLine(bottomLeftWorld, topLeftWorld);

        // Draw a faint wire sphere to represent the strict distance cutoff
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f); // Faint cyan
        Vector3 distanceOrigin = (aimMaster != null) ? aimMaster.position : transform.position;
        Gizmos.DrawWireSphere(distanceOrigin, fcsRange);
    }
}