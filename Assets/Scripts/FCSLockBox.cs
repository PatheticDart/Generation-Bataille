using UnityEngine;
using UnityEngine.UI;

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
    // We removed the public reticleImage variable. 
    // The script will now find all child images automatically!
    private Image[] reticleChildImages;

    // State Variables
    public Transform currentTarget { get; private set; }
    public bool isSoftLocked { get; private set; }
    public bool isHardLocked { get; private set; }
    private float currentLockTimer = 0f;

    void Start()
    {
        // 1. Calculate the static lockbox size
        if (isPlayer && lockBoxUI != null)
        {
            lockBoxUI.sizeDelta = new Vector2(fcsWidth * 15f, fcsHeight * 15f);
        }

        // 2. Automatically find all Image components on the Reticle's children
        if (isPlayer && reticleUI != null)
        {
            reticleChildImages = reticleUI.GetComponentsInChildren<Image>(true);
        }
    }

    void Update()
    {
        // 3. The Parallax Fix
        if (isPlayer && mainCamera != null)
        {
            transform.position = mainCamera.transform.position;
        }

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
        Collider[] potentialTargets = Physics.OverlapSphere(transform.position, fcsRange, targetLayer);
        Transform bestTarget = null;
        float closestDistance = Mathf.Infinity;

        foreach (Collider col in potentialTargets)
        {
            Transform targetTransform = col.transform;
            Vector3 localPos = transform.InverseTransformPoint(targetTransform.position);

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

        // 4. Move the static Orange Box
        Vector3 projectionPoint = transform.position + transform.forward * 100f;
        Vector3 boxScreenPos = mainCamera.WorldToScreenPoint(projectionPoint);

        if (boxScreenPos.z > 0)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, boxScreenPos, mainCamera, out Vector2 boxLocalPos);
            lockBoxUI.localPosition = boxLocalPos;
        }

        // 5. Handle the Reticle & Change Child Colors
        if (currentTarget != null)
        {
            Vector3 enemyScreenPos = mainCamera.WorldToScreenPoint(currentTarget.position);

            if (enemyScreenPos.z > 0)
            {
                reticleUI.gameObject.SetActive(true);

                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, enemyScreenPos, mainCamera, out Vector2 reticleLocalPos);
                reticleUI.localPosition = reticleLocalPos;

                // NEW: Loop through every child image and update its color!
                Color targetColor = isHardLocked ? Color.red : Color.green;
                foreach (Image img in reticleChildImages)
                {
                    if (img != null) img.color = targetColor;
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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position;

        Quaternion upLeft = Quaternion.Euler(-fcsHeight / 2f, -fcsWidth / 2f, 0);
        Quaternion upRight = Quaternion.Euler(-fcsHeight / 2f, fcsWidth / 2f, 0);
        Quaternion downLeft = Quaternion.Euler(fcsHeight / 2f, -fcsWidth / 2f, 0);
        Quaternion downRight = Quaternion.Euler(fcsHeight / 2f, fcsWidth / 2f, 0);

        Vector3 tl = origin + (transform.rotation * upLeft * Vector3.forward) * fcsRange;
        Vector3 tr = origin + (transform.rotation * upRight * Vector3.forward) * fcsRange;
        Vector3 bl = origin + (transform.rotation * downLeft * Vector3.forward) * fcsRange;
        Vector3 br = origin + (transform.rotation * downRight * Vector3.forward) * fcsRange;

        Gizmos.DrawLine(origin, tl);
        Gizmos.DrawLine(origin, tr);
        Gizmos.DrawLine(origin, bl);
        Gizmos.DrawLine(origin, br);

        Gizmos.DrawLine(tl, tr);
        Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl);
        Gizmos.DrawLine(bl, tl);
    }
}