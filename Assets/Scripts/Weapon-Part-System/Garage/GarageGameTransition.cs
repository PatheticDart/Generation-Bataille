using UnityEngine;
using UnityEngine.Rendering; 
using UnityEngine.SceneManagement;
using System.Collections;
using NaughtyAttributes;
using Unity.Cinemachine; // NEW: Required to access Cinemachine 3 components!

public class GarageGameTransition : MonoBehaviour
{
    [Header("Camera Transition")]
    [Tooltip("The Virtual Camera positioned behind the mech.")]
    public GameObject transitionVirtualCamera;
    
    [Tooltip("The final Horizontal Orbit angle (Left/Right) when the fade completes.")]
    public float targetOrbitHorizontal = 180f;
    [Tooltip("The final Vertical Orbit angle (Up/Down) when the fade completes.")]
    public float targetOrbitVertical = 10f;

    [Header("Transition Elements")]
    public Volume bAndWVolume;
    public CanvasGroup whiteScreen;
    
    [Header("Timings")]
    [Tooltip("How long it takes for the camera to orbit AND the color to drain.")]
    public float lutAndRotationDuration = 1.5f;
    public float whiteFadeInDuration = 1.0f;
    public int gameplaySceneIndex = 1; // Assuming "GameplayScene" is the second scene in your build settings

    private CinemachineOrbitalFollow _orbitalFollow;
    
    [Header("Timings")]
    public bool mapSelected = false;
    public bool enemySelected = false;

    private void Start()
    {
        if (whiteScreen != null) whiteScreen.alpha = 0f;
        if (bAndWVolume != null) bAndWVolume.weight = 0f;

        // Ensure the transition camera is off when we load the garage
        if (transitionVirtualCamera != null) 
        {
            transitionVirtualCamera.SetActive(false);
            
            // Grab the orbital follow component so we can manipulate it later
            _orbitalFollow = transitionVirtualCamera.GetComponent<CinemachineOrbitalFollow>();
        }
    }

    [Button("Test Launch Sequence (Goes to test scene)")]
    public void BeginLaunchSequence()
    {
        StartCoroutine(LaunchSequenceRoutine(gameplaySceneIndex));
    }

    public void BeginLaunchSequence(int sceneIndex)
    {
        StartCoroutine(LaunchSequenceRoutine(sceneIndex));
    }

    private IEnumerator LaunchSequenceRoutine(int sceneIndex)
    {
        // 1. Block raycasts so the player can't double-click the deploy button
        if (whiteScreen != null) whiteScreen.blocksRaycasts = true;

        // 2. Activate the transition Virtual Camera
        if (transitionVirtualCamera != null) transitionVirtualCamera.SetActive(true);

        // 3. Capture the starting angles exactly as they are right now
        float startHorizontal = 0f;
        float startVertical = 0f;

        if (_orbitalFollow != null)
        {
            startHorizontal = _orbitalFollow.HorizontalAxis.Value;
            startVertical = _orbitalFollow.VerticalAxis.Value;
        }

        float timer = 0f;

        // 4. Orbit the camera AND crank up the LUT simultaneously
        while (timer < lutAndRotationDuration)
        {
            timer += Time.deltaTime;
            
            // This goes from 0.0 to 1.0 over the duration
            float progress = timer / lutAndRotationDuration;

            // Fade the LUT
            if (bAndWVolume != null) bAndWVolume.weight = Mathf.Lerp(0f, 1f, progress);
            
            // Orbit the Camera
            if (_orbitalFollow != null)
            {
                _orbitalFollow.HorizontalAxis.Value = Mathf.Lerp(startHorizontal, targetOrbitHorizontal, progress);
                _orbitalFollow.VerticalAxis.Value = Mathf.Lerp(startVertical, targetOrbitVertical, progress);
            }

            yield return null;
        }

        // Snap everything precisely to the final values just in case frames skipped
        if (bAndWVolume != null) bAndWVolume.weight = 1f;
        if (_orbitalFollow != null)
        {
            _orbitalFollow.HorizontalAxis.Value = targetOrbitHorizontal;
            _orbitalFollow.VerticalAxis.Value = targetOrbitVertical;
        }

        // 5. Fade the screen to pure white
        timer = 0f;
        if (whiteScreen != null)
        {
            while (timer < whiteFadeInDuration)
            {
                timer += Time.deltaTime;
                whiteScreen.alpha = Mathf.Lerp(0f, 1f, timer / whiteFadeInDuration);
                yield return null;
            }
            whiteScreen.alpha = 1f;
        }

        // 6. Load the next scene
        SceneManager.LoadScene(sceneIndex);
    }
}