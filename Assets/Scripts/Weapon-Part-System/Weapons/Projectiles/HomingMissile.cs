using UnityEngine;

public class HomingMissile : BaseProjectile
{
    [Header("Curve Flight Dynamics")]
    public float curveDuration = 3f;
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 0.2f, 1, 1);
    public AnimationCurve turnCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float maxTurnSpeed = 180f;

    [Header("Explosion & Collision")]
    public LayerMask hitMask = ~0; 
    public float explosionRadius = 8f;
    public float explosionForce = 1000f;

    private Transform _target;
    private float _timeAlive;
    private float _flightSpeed = 100f; 

    protected override void OnEnable()
    {
        base.OnEnable(); 
        _timeAlive = 0f;
    }

    public void SetHomingData(Transform target)
    {
        _target = target;
    }

    public override void SetupStats(float newDamage, float newSpeed)
    {
        base.SetupStats(newDamage, newSpeed);
        _flightSpeed = newSpeed; 
    }

    protected override void Update()
    {
        if (isReturning) return; 
        base.Update(); 

        _timeAlive += Time.deltaTime;
        float curveProgress = Mathf.Clamp01(_timeAlive / curveDuration);

        float currentSpeed = speedCurve.Evaluate(curveProgress) * _flightSpeed;
        float currentTurnSpeed = turnCurve.Evaluate(curveProgress) * maxTurnSpeed;

        if (_target != null && currentTurnSpeed > 0f)
        {
            Vector3 directionToTarget = (_target.position - transform.position).normalized;
            Quaternion targetRotation = Quaternion.LookRotation(directionToTarget);
            
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, currentTurnSpeed * Time.deltaTime);
        }

        float moveDistance = currentSpeed * Time.deltaTime;
        
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, moveDistance, hitMask))
        {
            transform.position = hit.point; 
            Detonate(hit.normal); // Pass the normal here to align the explosion!
            return; 
        }

        transform.position += transform.forward * moveDistance;
    }

    public override void HandleHit(GameObject hitObject, Vector3 hitPoint, Vector3 hitNormal) { }

    private void Detonate(Vector3 hitNormal)
    {
        if (isReturning) return; 

        SpawnImpactEffect(transform.position, hitNormal); // Spawn universal VFX

        Collider[] caughtInBlast = Physics.OverlapSphere(transform.position, explosionRadius);
        foreach (Collider col in caughtInBlast)
        {
            if (col.TryGetComponent(out Rigidbody targetRb))
            {
                targetRb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }

        InitiateReturn(); 
    }
}