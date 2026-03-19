using UnityEngine;

public class FakeMirror : MonoBehaviour
{
    [Header("The Real World")]
    [Tooltip("Drag your actual, playable Mech here.")]
    public Transform realMechRoot;
    
    [Tooltip("Drag the floor plane here so we know exactly where the 'glass' is.")]
    public Transform floorPlane;

    [Header("The Reflection")]
    [Tooltip("Drag the stripped-down Mech_Reflection here.")]
    public Transform reflectionMechRoot;

    private Transform _mirrorPivot;
    private Transform[] _realBones;
    private Transform[] _reflectionBones;
    private int _lastChildCount = 0;

    void Start()
    {
        // 1. Create an invisible pivot point at the exact height of your floor
        _mirrorPivot = new GameObject("Mirror_Pivot").transform;
        _mirrorPivot.position = new Vector3(0, floorPlane.position.y, 0);
        
        // 2. THE MAGIC TRICK: Scale the Y-axis to -1. This turns the entire folder upside down!
        _mirrorPivot.localScale = new Vector3(1, -1, 1);

        // 3. Put the reflection mech inside this inverted folder
        if (reflectionMechRoot != null)
        {
            reflectionMechRoot.SetParent(_mirrorPivot);
        }
    }

    void LateUpdate()
    {
        if (realMechRoot == null || reflectionMechRoot == null) return;

        // --- DYNAMIC PART SUPPORT ---
        // Because your PartSystem spawns weapons at runtime, we check if the child count changed.
        // If it did, we re-map the bones so the reflection instantly equips the new weapons!
        int currentChildCount = realMechRoot.childCount;
        if (currentChildCount != _lastChildCount)
        {
            _realBones = realMechRoot.GetComponentsInChildren<Transform>();
            _reflectionBones = reflectionMechRoot.GetComponentsInChildren<Transform>();
            _lastChildCount = currentChildCount;
        }

        // --- SYNC LOOP ---
        // Copy the local transforms of every bone, weapon, and muzzle point.
        // Because the _mirrorPivot is scaled to -1, copying the local data perfectly reflects it!
        for (int i = 0; i < _realBones.Length; i++)
        {
            if (i >= _reflectionBones.Length) break; 

            _reflectionBones[i].localPosition = _realBones[i].localPosition;
            _reflectionBones[i].localRotation = _realBones[i].localRotation;
            _reflectionBones[i].localScale = _realBones[i].localScale;
        }
    }
}