using UnityEngine;

public abstract class RaycastProjectile : BaseProjectile
{
    [Header("Raycast Movement")]
    public float speed = 100f;
    public bool useGravity = false;
    public float gravityMultiplier = 1f;
    public LayerMask hitMask;

    protected Vector3 currentVelocity;
    protected Vector3 previousPosition;

    protected override void OnEnable()
    {
        base.OnEnable(); 
        previousPosition = transform.position;
    }

    public override void SetupStats(float newDamage, float newSpeed)
    {
        base.SetupStats(newDamage, newSpeed);
        speed = newSpeed; 
        
        currentVelocity = transform.forward * speed; 
        previousPosition = transform.position; 
    }

    protected override void Update()
    {
        base.Update(); 

        // THE FIX: If the bullet has already hit something and is just waiting 
        // for its trail to fade, stop moving and stop raycasting!
        if (isReturning) return;

        if (useGravity)
        {
            currentVelocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
        }

        Vector3 displacement = currentVelocity * Time.deltaTime;
        float moveDistance = displacement.magnitude;

        if (Physics.Raycast(previousPosition, displacement.normalized, out RaycastHit hit, moveDistance, hitMask))
        {
            HandleHit(hit.collider.gameObject, hit.point, hit.normal);
            return; 
        }

        transform.position += displacement;
        
        if (currentVelocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(currentVelocity);
        }
        
        previousPosition = transform.position;
    }
}