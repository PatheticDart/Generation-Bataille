using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using System.Collections;
using NaughtyAttributes;
using Unity.Cinemachine;

public class GarageGameTransition : MonoBehaviour
{
    [Header("Camera Transition")]
    public GameObject transitionVirtualCamera;
    public float targetOrbitHorizontal = 180f;
    public float targetOrbitVertical = 10f;

    [Header("Transition Elements")]
    public Volume bAndWVolume;
    public CanvasGroup whiteScreen;

    [Header("Timings")]
    public float lutAndRotationDuration = 1.5f;
    public float whiteFadeInDuration = 1.0f;
    public int gameplaySceneIndex = 1;

    private CinemachineOrbitalFollow _orbitalFollow;

    [Header("States")]
    public bool mapSelected = false;
    public bool enemySelected = false;

    private void Start()
    {
        Time.timeScale = 1f;

        if (whiteScreen != null)
        {
            whiteScreen.alpha = 0f;
            whiteScreen.blocksRaycasts = false;
        }

        if (bAndWVolume != null) bAndWVolume.weight = 0f;

        if (transitionVirtualCamera != null)
        {
            transitionVirtualCamera.SetActive(false);
            _orbitalFollow = transitionVirtualCamera.GetComponent<CinemachineOrbitalFollow>();
        }
    }

    [Button("Test Launch Sequence (Goes to test scene)")]
    public void BeginLaunchSequence()
    {
        // --- THE FIX: Force a save so loadout changes are permanent! ---
        if (PlayerInventoryManager.Instance != null) PlayerInventoryManager.Instance.SaveInventory();

        StartCoroutine(LaunchSequenceRoutine(gameplaySceneIndex));
    }

    public void BeginLaunchSequence(int sceneIndex)
    {
        // --- THE FIX: Force a save so loadout changes are permanent! ---
        if (PlayerInventoryManager.Instance != null) PlayerInventoryManager.Instance.SaveInventory();

        StartCoroutine(LaunchSequenceRoutine(sceneIndex));
    }

    private IEnumerator LaunchSequenceRoutine(int sceneIndex)
    {
        if (whiteScreen != null) whiteScreen.blocksRaycasts = true;
        if (transitionVirtualCamera != null) transitionVirtualCamera.SetActive(true);

        float startHorizontal = 0f;
        float startVertical = 0f;

        if (_orbitalFollow != null)
        {
            startHorizontal = _orbitalFollow.HorizontalAxis.Value;
            startVertical = _orbitalFollow.VerticalAxis.Value;
        }

        float timer = 0f;

        while (timer < lutAndRotationDuration)
        {
            timer += Time.unscaledDeltaTime;
            float progress = timer / lutAndRotationDuration;

            if (bAndWVolume != null) bAndWVolume.weight = Mathf.Lerp(0f, 1f, progress);
            if (_orbitalFollow != null)
            {
                _orbitalFollow.HorizontalAxis.Value = Mathf.Lerp(startHorizontal, targetOrbitHorizontal, progress);
                _orbitalFollow.VerticalAxis.Value = Mathf.Lerp(startVertical, targetOrbitVertical, progress);
            }

            yield return null;
        }

        if (bAndWVolume != null) bAndWVolume.weight = 1f;
        if (_orbitalFollow != null)
        {
            _orbitalFollow.HorizontalAxis.Value = targetOrbitHorizontal;
            _orbitalFollow.VerticalAxis.Value = targetOrbitVertical;
        }

        timer = 0f;
        if (whiteScreen != null)
        {
            while (timer < whiteFadeInDuration)
            {
                timer += Time.unscaledDeltaTime;
                whiteScreen.alpha = Mathf.Lerp(0f, 1f, timer / whiteFadeInDuration);
                yield return null;
            }
            whiteScreen.alpha = 1f;
        }

        SceneManager.LoadScene(sceneIndex);
    }
}