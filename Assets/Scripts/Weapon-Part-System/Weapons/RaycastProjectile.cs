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

    // Replaced InitializeBullet with OnEnable to catch the Object Pool wake-up
    protected override void OnEnable()
    {
        base.OnEnable(); // Calls the life timer reset in BaseProjectile
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