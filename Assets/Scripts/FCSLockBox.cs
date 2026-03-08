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
    public float fcsTurnRate = 10f;

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
    [Tooltip("Drag the MechWeaponManager here to read the active weapon states.")]
    public MechWeaponManager weaponManager;

    [Tooltip("Opacity level (0 to 1) for the weapon currently selected.")]
    public float activeWeaponAlpha = 1.0f;
    [Tooltip("Opacity level (0 to 1) for the weapon currently stowed/deactivated.")]
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
    private float currentLockTimer = 0f;

    void Start()
    {
        if (weaponManager == null && isPlayer) weaponManager = GetComponent<MechWeaponManager>();

        if (isPlayer && lockBoxUI != null)
        {
            lockBoxUI.sizeDelta = new Vector2(fcsWidth * 15f, fcsHeight * 15f);
        }

        if (isPlayer && reticleUI != null)
        {
            reticleChildImages = reticleUI.GetComponentsInChildren<Image>(true);
            // Grab all texts normally again
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
        float oversizedPhysicsRadius = fcsRange * 1.5f;
        Collider[] potentialTargets = Physics.OverlapSphere(origin, oversizedPhysicsRadius, targetLayer);

        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in potentialTargets)
        {
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
            }
            else
            {
                if (!isHardLocked)
                {
                    currentLockTimer += Time.deltaTime;
                    if (currentLockTimer >= lockSpeed) isHardLocked = true;
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

                // 1. Apply the base color (Green or Red) to EVERYTHING inside the reticle
                Color targetColor = isHardLocked ? hardLockColor : softLockColor;

                foreach (Image img in reticleChildImages)
                {
                    if (img != null) img.color = targetColor;
                }

                foreach (TextMeshProUGUI txt in reticleChildTexts)
                {
                    if (txt != null) txt.color = targetColor;
                }

                // 2. NEW: Specifically override the Alpha for the Ammo texts based on Weapon Manager state
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

    // NEW: Helper method to apply the correct alpha over the base target color
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