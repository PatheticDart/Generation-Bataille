using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

[DefaultExecutionOrder(1000)]
public class FCSLockBox : MonoBehaviour
{
    [Header("FCS Stats")]
    public float fcsWidth = 45f;
    public float fcsHeight = 30f;
    public float fcsRange = 300f;
    public float lockSpeed = 1.5f;
    [Tooltip("Maximum tracking speed in degrees per second. Try 180 to 300 for a snappy feel.")]
    public float fcsTurnRate = 180f;

    // --- NEW MULTI-LOCK STATS ---
    [Tooltip("Maximum number of targets/missiles the FCS can lock onto at once.")]
    public int maxMultiLocks = 6;
    [Tooltip("How fast subsequent locks are acquired after the first hard lock.")]
    public float multiLockInterval = 0.3f;

    [Header("Colors")]
    public Color softLockColor = Color.green;
    public Color hardLockColor = Color.red;

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
    public TextMeshProUGUI rangefinderText;

    [Header("Weapon UI Integration")]
    public MechWeaponManager weaponManager;
    public float activeWeaponAlpha = 1.0f;
    public float inactiveWeaponAlpha = 0.5f;

    public List<TextMeshProUGUI> leftArmAmmoTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> rightArmAmmoTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> leftBackAmmoTexts = new List<TextMeshProUGUI>();
    public List<TextMeshProUGUI> rightBackAmmoTexts = new List<TextMeshProUGUI>();

    private Image[] reticleChildImages;
    private TextMeshProUGUI[] reticleChildTexts;

    public Transform currentTarget { get; private set; }
    public bool isSoftLocked { get; private set; }
    public bool isHardLocked { get; private set; }

    public int currentLockCount { get; private set; }
    private float currentLockTimer = 0f;

    void Start()
    {
        if (weaponManager == null && isPlayer) weaponManager = GetComponent<MechWeaponManager>();

        if (isPlayer && lockBoxUI != null)
            lockBoxUI.sizeDelta = new Vector2(fcsWidth * 15f, fcsHeight * 15f);

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

        if (isPlayer) UpdateUI();
    }

    public void ConsumeLocks()
    {
        currentLockCount = 0;
        currentLockTimer = 0f;
        isHardLocked = false;
    }

    private void UpdateFCSTrailingRotation()
    {
        if (aimMaster == null) return;
        float angleDifference = Quaternion.Angle(transform.rotation, aimMaster.rotation);

        if (angleDifference <= stickyThreshold) transform.rotation = aimMaster.rotation;
        else transform.rotation = Quaternion.RotateTowards(transform.rotation, aimMaster.rotation, fcsTurnRate * Time.deltaTime);
    }

    private void DetectAndManageTargets()
    {
        Vector3 origin = (isPlayer && mainCamera != null) ? mainCamera.transform.position : transform.position;
        float oversizedPhysicsRadius = fcsRange * 1.5f;

        // This already filters out everything EXCEPT the target layer (e.g., the enemy team)
        Collider[] potentialTargets = Physics.OverlapSphere(origin, oversizedPhysicsRadius, targetLayer);

        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in potentialTargets)
        {
            // --- THE FIX: Filter out everything that isn't explicitly tagged as the target center ---
            if (!col.CompareTag("TargetObject")) continue;

            Transform targetTransform = col.transform;
            Vector3 distanceOrigin = (aimMaster != null) ? aimMaster.position : transform.position;
            float distanceToTarget = Vector3.Distance(distanceOrigin, targetTransform.position);

            if (distanceToTarget > fcsRange) continue;

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
                currentLockCount = 0;
            }
            else
            {
                currentLockTimer += Time.deltaTime;

                if (currentLockTimer >= lockSpeed)
                {
                    isHardLocked = true;

                    float extraTime = currentLockTimer - lockSpeed;
                    currentLockCount = 1 + Mathf.FloorToInt(extraTime / multiLockInterval);
                    currentLockCount = Mathf.Clamp(currentLockCount, 1, maxMultiLocks);
                }
            }
        }
        else
        {
            currentTarget = null;
            isSoftLocked = false;
            isHardLocked = false;
            currentLockTimer = 0f;
            currentLockCount = 0;
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

                Color targetColor = isHardLocked ? hardLockColor : softLockColor;

                foreach (Image img in reticleChildImages) if (img != null) img.color = targetColor;
                foreach (TextMeshProUGUI txt in reticleChildTexts) if (txt != null) txt.color = targetColor;

                if (weaponManager != null)
                {
                    ApplyAmmoAlpha(leftArmAmmoTexts, targetColor, weaponManager.leftArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);
                    ApplyAmmoAlpha(leftBackAmmoTexts, targetColor, !weaponManager.leftArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);
                    ApplyAmmoAlpha(rightArmAmmoTexts, targetColor, weaponManager.rightArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);
                    ApplyAmmoAlpha(rightBackAmmoTexts, targetColor, !weaponManager.rightArmActive ? activeWeaponAlpha : inactiveWeaponAlpha);
                }

                if (rangefinderText != null)
                {
                    Vector3 distanceOrigin = (aimMaster != null) ? aimMaster.position : transform.position;
                    float distanceToTarget = Vector3.Distance(distanceOrigin, currentTarget.position);
                    rangefinderText.text = $"{distanceToTarget:F0}m";
                }
            }
            else reticleUI.gameObject.SetActive(false);
        }
        else reticleUI.gameObject.SetActive(false);
    }

    private void ApplyAmmoAlpha(List<TextMeshProUGUI> textList, Color baseColor, float targetAlpha)
    {
        foreach (TextMeshProUGUI txt in textList)
        {
            if (txt != null)
            {
                Color finalColor = baseColor;
                finalColor.a = targetAlpha;
                txt.color = finalColor;
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;

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

        Vector3 topLeftLocal = Quaternion.Euler(-halfHeight, -halfWidth, 0) * Vector3.forward * fcsRange;
        Vector3 topRightLocal = Quaternion.Euler(-halfHeight, halfWidth, 0) * Vector3.forward * fcsRange;
        Vector3 bottomLeftLocal = Quaternion.Euler(halfHeight, -halfWidth, 0) * Vector3.forward * fcsRange;
        Vector3 bottomRightLocal = Quaternion.Euler(halfHeight, halfWidth, 0) * Vector3.forward * fcsRange;

        Vector3 topLeftWorld = origin + transform.rotation * topLeftLocal;
        Vector3 topRightWorld = origin + transform.rotation * topRightLocal;
        Vector3 bottomLeftWorld = origin + transform.rotation * bottomLeftLocal;
        Vector3 bottomRightWorld = origin + transform.rotation * bottomRightLocal;

        Gizmos.DrawLine(origin, topLeftWorld);
        Gizmos.DrawLine(origin, topRightWorld);
        Gizmos.DrawLine(origin, bottomLeftWorld);
        Gizmos.DrawLine(origin, bottomRightWorld);

        Gizmos.DrawLine(topLeftWorld, topRightWorld);
        Gizmos.DrawLine(topRightWorld, bottomRightWorld);
        Gizmos.DrawLine(bottomRightWorld, bottomLeftWorld);
        Gizmos.DrawLine(bottomLeftWorld, topLeftWorld);

        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Vector3 distanceOrigin = (aimMaster != null) ? aimMaster.position : transform.position;
        Gizmos.DrawWireSphere(distanceOrigin, fcsRange);
    }
}