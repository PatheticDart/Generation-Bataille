using UnityEngine;

public class SimpleProjectile : MonoBehaviour
{
    public float speed = 100f;
    public float lifetime = 3f;

    void Start()
    {
        // Ensure the scene doesn't fill up with stray bullets
        Destroy(gameObject, lifetime);
    }

    void Update()
    {
        // Move forward along the local Z axis
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}