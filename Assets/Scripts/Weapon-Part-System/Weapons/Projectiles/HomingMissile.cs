using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HomingMissile : BaseProjectile
{
    [Header("Curve Flight Dynamics")]
    [Tooltip("How many seconds it takes for the curves below to read from left (0) to right (1).")]
    public float curveDuration = 3f;

    [Tooltip("X: Time (0 to 1). Y: Forward Speed Multiplier (0 to 1).")]
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0.2f, 1, 1);
    public float maxSpeed = 150f;

    [Tooltip("X: Time (0 to 1). Y: Turn Speed Multiplier (0 to 1). Keep Y at 0 at the start to simulate Clearance Time!")]
    public AnimationCurve turnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float maxTurnSpeed = 180f;

    [Header("Explosion")]
    public float explosionRadius = 8f;
    public float explosionForce = 1000f;
    public PooledVFX explosionEffectPrefab;

    private Rigidbody _rb;
    private Transform _target;
    private float _timeAlive;
    private bool _hasDetonated;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    protected override void OnEnable()
    {
        base.OnEnable(); 
        _hasDetonated = false;
        _timeAlive = 0f;

        if (_rb != null)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }
    }

    public void SetHomingData(Transform target)
    {
        _target = target;
    }

    public override void SetupStats(float newDamage, float newSpeed)
    {
        base.SetupStats(newDamage, newSpeed);
    }

    protected override void Update()
    {
        if (_hasDetonated) return;
        base.Update(); 
    }

    private void FixedUpdate()
    {
        if (_hasDetonated || _rb == null) return;

        _timeAlive += Time.fixedDeltaTime;
        float curveProgress = Mathf.Clamp01(_timeAlive / curveDuration);

        float currentSpeed = speedCurve.Evaluate(curveProgress) * maxSpeed;
        float currentTurnSpeed = turnCurve.Evaluate(curveProgress) * maxTurnSpeed;

        // Will safely skip turning if _target is null!
        if (_target != null && currentTurnSpeed > 0f)
        {
            Vector3 directionToTarget = (_target.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            
            _rb.MoveRotation(Quaternion.RotateTowards(transform.rotation, targetRotation, currentTurnSpeed * Time.fixedDeltaTime));
        }

        _rb.linearVelocity = transform.forward * currentSpeed;
    }

    public override void HandleHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal) { }

    private void OnCollisionEnter(Collision collision)
    {
        Detonate();
    }

    private void Detonate()
    {
        if (_hasDetonated) return;
        _hasDetonated = true;

        if (explosionEffectPrefab != null && GlobalVFXPool.Instance != null)
        {
            GlobalVFXPool.Instance.Spawn(explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        Collider[] caughtInBlast = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider col in caughtInBlast)
        {
            if (col.TryGetComponent<Rigidbody>(out Rigidbody targetRb))
            {
                targetRb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }

        ReturnToGlobalPool();
    }
}