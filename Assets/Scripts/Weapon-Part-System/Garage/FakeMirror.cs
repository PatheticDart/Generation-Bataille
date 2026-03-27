using UnityEngine;

public class FakeMirror : MonoBehaviour
{
    [Header("The Real World")]
    [Tooltip("Drag your actual, playable Mech here.")]
    public Transform realMechRoot;
    [Tooltip("Drag the floor plane here so we know exactly where the 'glass' is.")]
    public Transform floorPlane;

    [Header("The Reflection")]
    [Tooltip("Drag the material you want to use for the reflection (e.g., dark grey silhouette).")]
    public Material reflectionMaterial;

    private Transform _mirrorPivot;
    private Transform _reflectionMechRoot;
    private Transform[] _realBones;
    private Transform[] _reflectionBones;
    private int _lastTransformCount = -1;

    void Start()
    {
        // Create the inverted floor pivot
        _mirrorPivot = new GameObject("Mirror_Pivot").transform;
        _mirrorPivot.position = new Vector3(0, floorPlane.position.y, 0);
        _mirrorPivot.localScale = new Vector3(1, -1, 1);
    }

    void LateUpdate()
    {
        if (realMechRoot == null) return;

        // Check the total number of transforms. If the PartSystem spawned/destroyed parts, this changes!
        int currentTransformCount = realMechRoot.GetComponentsInChildren<Transform>().Length;

        if (currentTransformCount != _lastTransformCount)
        {
            RebuildReflection();
            _realBones = realMechRoot.GetComponentsInChildren<Transform>();
            _reflectionBones = _reflectionMechRoot.GetComponentsInChildren<Transform>();
            _lastTransformCount = currentTransformCount;
        }

        if (_realBones == null || _reflectionBones == null) return;

        // Sync the animation poses safely
        for (int i = 0; i < _realBones.Length; i++)
        {
            if (i >= _reflectionBones.Length) break;

            _reflectionBones[i].localPosition = _realBones[i].localPosition;
            _reflectionBones[i].localRotation = _realBones[i].localRotation;
            _reflectionBones[i].localScale = _realBones[i].localScale;
        }
    }

    private void RebuildReflection()
    {
        // 1. Destroy the old broken reflection
        if (_reflectionMechRoot != null)
        {
            DestroyImmediate(_reflectionMechRoot.gameObject);
        }

        // 2. Clone the newly built Real Mech
        GameObject clone = Instantiate(realMechRoot.gameObject, _mirrorPivot);
        _reflectionMechRoot = clone.transform;
        _reflectionMechRoot.localPosition = Vector3.zero;
        _reflectionMechRoot.localRotation = Quaternion.identity;

        // 3. Strip all logic so the reflection doesn't run code, shoot, or fight IK
        foreach (var comp in clone.GetComponentsInChildren<MonoBehaviour>()) DestroyImmediate(comp);
        foreach (var comp in clone.GetComponentsInChildren<Animator>()) DestroyImmediate(comp);

        // Strip Particle Systems and Audio so the floor doesn't make noise or smoke
        foreach (var comp in clone.GetComponentsInChildren<ParticleSystem>()) DestroyImmediate(comp.gameObject);
        foreach (var comp in clone.GetComponentsInChildren<AudioSource>()) DestroyImmediate(comp);

        // 4. Apply the silhouette reflection material
        if (reflectionMaterial != null)
        {
            foreach (Renderer r in clone.GetComponentsInChildren<Renderer>())
            {
                Material[] mats = new Material[r.materials.Length];
                for (int i = 0; i < mats.Length; i++) mats[i] = reflectionMaterial;
                r.materials = mats;
            }
        }
    }
}