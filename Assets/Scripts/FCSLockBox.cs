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

    // Arrays to hold all our visual components
    private Image[] reticleChildImages;
    private TextMeshProUGUI[] reticleChildTexts; // NEW: Array for text components

    // State Variables
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
            // Automatically find all images AND text components inside the reticle
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

        Collider[] potentialTargets = Physics.OverlapSphere(origin, fcsRange, targetLayer);
        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in potentialTargets)
        {
            Transform targetTransform = col.transform;

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

                Color targetColor = isHardLocked ? Color.red : Color.green;

                // Color the Images
                foreach (Image img in reticleChildImages)
                {
                    if (img != null) img.color = targetColor;
                }

                // NEW: Color the Text elements
                foreach (TextMeshProUGUI txt in reticleChildTexts)
                {
                    if (txt != null) txt.color = targetColor;
                }

                if (rangefinderText != null)
                {
                    float distanceToTarget = Vector3.Distance(transform.position, currentTarget.position);
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
}