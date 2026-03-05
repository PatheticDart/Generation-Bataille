using UnityEngine;

public class PartSync : MonoBehaviour
{
    public Transform targetBone;

    void LateUpdate()
    {
        if (targetBone != null)
        {
            // We use absolute world rotation. This means even if the part is buried
            // 5 layers deep inside a prefab, it will perfectly match the skeleton's angle!
            transform.rotation = targetBone.rotation;
        }
    }
}