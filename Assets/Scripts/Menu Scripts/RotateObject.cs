using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class RotateObject : MonoBehaviour
{

    public Transform spawnPoint;
    public float rotationSpeed = 20f;
    public GameObject objectToRotate;
    private GameObject currentRotatingObject;
    private GameObject lastObjectToRotate;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        spawnPoint = this.transform;
    }

    // Update is called once per frame
    void Update()
    {
        // Check if a new object should be spawned
        if (objectToRotate != lastObjectToRotate && objectToRotate != null)
        {
            // Destroy all existing children
            foreach (Transform child in spawnPoint)
            {
                Destroy(child.gameObject);
            }
            
            // Instantiate the new prefab
            currentRotatingObject = Instantiate(objectToRotate, spawnPoint.position, Quaternion.identity, spawnPoint);
            lastObjectToRotate = objectToRotate;
        }
        
        // Rotate the current object
        if (currentRotatingObject != null)
        {
            currentRotatingObject.transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }
    }
}
