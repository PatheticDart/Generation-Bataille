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

    public override void InitializeBullet()
    {
        base.InitializeBullet(); // Calls the lifetime destroy from BaseProjectile
        previousPosition = transform.position;
        currentVelocity = transform.forward * speed;
    }

    public override void SetupStats(float newDamage, float newSpeed)
    {
        base.SetupStats(newDamage, newSpeed);
        speed = newSpeed; 
        
        // Start moving bullet
        currentVelocity = transform.forward * speed; 
    }

    protected override void Update()
    {
        if (useGravity)
        {
            currentVelocity += Physics.gravity * gravityMultiplier * Time.deltaTime;
        }

        Vector3 displacement = currentVelocity * Time.deltaTime;
        float moveDistance = displacement.magnitude;

        // Cast a ray from where we were to where we are going
        if (Physics.Raycast(previousPosition, displacement.normalized, out RaycastHit hit, moveDistance, hitMask))
        {
            // Pass the hit data up to the BaseProjectile's abstract method
            HandleHit(hit.collider.gameObject, hit.point, hit.normal);
            return; 
        }

        // Move the visual model
        transform.position += displacement;
        
        // Dip the nose of the bullet along the arc
        if (currentVelocity != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(currentVelocity);
        }
        
        previousPosition = transform.position;
    }
}