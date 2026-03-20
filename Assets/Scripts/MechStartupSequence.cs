using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Events;
using System.Collections;

public class MechStartupSequence : MonoBehaviour
{
    [Header("Transition Elements")]
    public Volume bAndWVolume;
    public CanvasGroup whiteScreen;

    [Header("Timings")]
    [Tooltip("How long to hold the pure white screen before fading it out.")]
    public float initialWhiteHold = 0.5f;
    public float whiteFadeOutDuration = 1.5f;
    
    [Tooltip("How long to hold the Black & White view before returning to color.")]
    public float bAndWHold = 1.0f;
    public float lutFadeOutDuration = 2.5f;

    [Header("Events")]
    [Tooltip("Fires when the sequence is fully complete. Hook up PlayerBrain enable here!")]
    public UnityEvent OnStartupComplete;

    private void Start()
    {
        // 1. Instantly set the scene to match the end of the Garage transition
        if (whiteScreen != null) 
        {
            whiteScreen.alpha = 1f;
            whiteScreen.blocksRaycasts = true; // Block inputs during intro
        }
        
        if (bAndWVolume != null) 
        {
            bAndWVolume.weight = 1f;
        }

        // 2. Begin the intro
        StartCoroutine(StartupRoutine());
    }

    private IEnumerator StartupRoutine()
    {
        // Hold on white
        yield return new WaitForSeconds(initialWhiteHold);

        float timer = 0f;

        // 3. Close the white screen (Reveals the B&W game world)
        if (whiteScreen != null)
        {
            while (timer < whiteFadeOutDuration)
            {
                timer += Time.deltaTime;
                whiteScreen.alpha = Mathf.Lerp(1f, 0f, timer / whiteFadeOutDuration);
                yield return null;
            }
            whiteScreen.alpha = 0f;
            whiteScreen.blocksRaycasts = false; // Give UI control back if needed
        }

        // Hold on the B&W world for dramatic effect
        yield return new WaitForSeconds(bAndWHold);

        // 4. Remove the LUT (Colors bleed back in)
        timer = 0f;
        if (bAndWVolume != null)
        {
            while (timer < lutFadeOutDuration)
            {
                timer += Time.deltaTime;
                bAndWVolume.weight = Mathf.Lerp(1f, 0f, timer / lutFadeOutDuration);
                yield return null;
            }
            bAndWVolume.weight = 0f;
        }

        // 5. Fire the event to start the game!
        OnStartupComplete?.Invoke();
    }
}