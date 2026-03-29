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
    public float fcsTurnRate = 180f;

    [Header("Multi-Lock Stats")]
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

    [Header("Missile Lock UI")]
    public GameObject leftMissileLockUI;
    public TextMeshProUGUI leftMissileLockText;
    public GameObject rightMissileLockUI;
    public TextMeshProUGUI rightMissileLockText;

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

    public Vector3 TargetVelocity { get; private set; }
    private Vector3 lastTargetPos;

    // --- NEW: Separated Timers to prevent UI glitches ---
    private float baseLockTimer = 0f;
    private float leftMissileTimer = 0f;
    private float rightMissileTimer = 0f;

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

        if (isPlayer)
        {
            if (leftMissileLockUI != null) leftMissileLockUI.SetActive(false);
            if (rightMissileLockUI != null) rightMissileLockUI.SetActive(false);
        }
    }

    void LateUpdate()
    {
        TrackVelocities();
        UpdateFCSTrailingRotation();
        DetectAndManageTargets();

        if (isPlayer) UpdateUI();
    }

    private void TrackVelocities()
    {
        if (Time.deltaTime > 0f && currentTarget != null)
        {
            Vector3 rawVelocity = (currentTarget.position - lastTargetPos) / Time.deltaTime;
            if (rawVelocity.magnitude > 500f) rawVelocity = rawVelocity.normalized * 500f;

            TargetVelocity = Vector3.Lerp(TargetVelocity, rawVelocity, Time.deltaTime * 15f);
            lastTargetPos = currentTarget.position;
        }
        else
        {
            TargetVelocity = Vector3.zero;
        }
    }

    // --- NEW: Checks if the specific side is active and not currently firing ---
    public bool CanAcquireMissileLocks(bool isLeft)
    {
        if (weaponManager == null || weaponManager.weaponManager == null) return false;

        bool isBackActive = isLeft ? weaponManager.leftBackActive : weaponManager.rightBackActive;
        if (!isBackActive) return false;

        FunctionalWeapon wep = weaponManager.weaponManager.GetWeapon(isLeft, 1);
        if (wep == null || !(wep.GetWeaponData() is MissileLauncherPart)) return false;

        if (wep is MissileArrayWeapon arrayWep && arrayWep.IsFiringBurst) return false;

        return true;
    }

    public int GetMissileLocks(bool isLeft, int maxLocks)
    {
        if (!isHardLocked || maxLocks <= 0 || !CanAcquireMissileLocks(isLeft)) return 0;
        float timer = isLeft ? leftMissileTimer : rightMissileTimer;
        return Mathf.Clamp(1 + Mathf.FloorToInt(timer / multiLockInterval), 1, maxLocks);
    }

    public void ConsumeMissileLocks(bool isLeft)
    {
        if (isLeft) leftMissileTimer = 0f;
        else rightMissileTimer = 0f;
    }

    public void ConsumeLocks()
    {
        isHardLocked = false;
        baseLockTimer = 0f;
        leftMissileTimer = 0f;
        rightMissileTimer = 0f;
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

        Collider[] potentialTargets = Physics.OverlapSphere(origin, oversizedPhysicsRadius, targetLayer);
        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in potentialTargets)
        {
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
                baseLockTimer = 0f;
                leftMissileTimer = 0f;
                rightMissileTimer = 0f;
                lastTargetPos = currentTarget.position;
            }
            else
            {
                if (!isHardLocked)
                {
                    baseLockTimer += Time.deltaTime;
                    if (baseLockTimer >= lockSpeed)
                    {
                        isHardLocked = true;
                    }
                }
                else
                {
                    // --- NEW: Only accumulate locks if the weapon is ready and active! ---
                    if (CanAcquireMissileLocks(true)) leftMissileTimer += Time.deltaTime;
                    else leftMissileTimer = 0f;

                    if (CanAcquireMissileLocks(false)) rightMissileTimer += Time.deltaTime;
                    else rightMissileTimer = 0f;
                }
            }
        }
        else
        {
            currentTarget = null;
            isSoftLocked = false;
            isHardLocked = false;
            baseLockTimer = 0f;
            leftMissileTimer = 0f;
            rightMissileTimer = 0f;
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

        // --- UPDATE MISSILE UI PANELS ---
        int leftMax = 0; int rightMax = 0;
        if (weaponManager != null && weaponManager.weaponManager != null)
        {
            var lw = weaponManager.weaponManager.GetWeapon(true, 1);
            if (lw != null && lw.GetWeaponData() is MissileLauncherPart lmp) leftMax = lmp.maxLocks;

            var rw = weaponManager.weaponManager.GetWeapon(false, 1);
            if (rw != null && rw.GetWeaponData() is MissileLauncherPart rmp) rightMax = rmp.maxLocks;
        }

        if (leftMissileLockUI != null)
        {
            bool showL = isHardLocked && leftMax > 0 && CanAcquireMissileLocks(true);
            if (leftMissileLockUI.activeSelf != showL) leftMissileLockUI.SetActive(showL);

            if (showL && leftMissileLockText != null)
            {
                int currentL = GetMissileLocks(true, leftMax);
                leftMissileLockText.text = currentL.ToString();
            }
        }

        if (rightMissileLockUI != null)
        {
            bool showR = isHardLocked && rightMax > 0 && CanAcquireMissileLocks(false);
            if (rightMissileLockUI.activeSelf != showR) rightMissileLockUI.SetActive(showR);

            if (showR && rightMissileLockText != null)
            {
                int currentR = GetMissileLocks(false, rightMax);
                rightMissileLockText.text = currentR.ToString();
            }
        }
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
}