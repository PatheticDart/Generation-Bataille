using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;

public class MatchManager : MonoBehaviour
{
    [Header("Combatants - Player (Team 1)")]
    public PlayerBrain playerBrain;
    public MechStats playerStats;
    public MechController playerController;

    [Header("Combatants - AI (Team 2)")]
    public PrototypeAIBrain aiBrain;
    public MechStats aiStats;
    public MechController aiController;

    [Header("UI References")]
    [Tooltip("Text element used to display countdowns and Win/Lose messages. Script will auto-find an object named 'MatchManagerText' if left blank.")]
    public TextMeshProUGUI matchText;

    [Header("Match Settings")]
    public float startCountdownTime = 3f;
    public float matchEndWaitTime = 5f;
    public int menuSceneIndex = 0;

    // Internal State Tracker
    private bool isMatchActive = false;
    private bool matchHasEnded = false;

    private void Awake()
    {
        // --- AUTO-FIND PLAYER ---
        if (playerBrain == null)
            playerBrain = FindObjectOfType<PlayerBrain>();
            
        if (playerBrain != null)
        {
            if (playerStats == null) playerStats = playerBrain.GetComponent<MechStats>();
            if (playerController == null) playerController = playerBrain.GetComponent<MechController>();
        }

        if (playerBrain == null || playerStats == null || playerController == null)
            Debug.LogError("MatchManager: Could not find PlayerBrain, MechStats, or MechController in the scene!");

        // --- AUTO-FIND AI ---
        if (aiBrain == null)
            aiBrain = FindObjectOfType<PrototypeAIBrain>();
            
        if (aiBrain != null)
        {
            if (aiStats == null) aiStats = aiBrain.GetComponent<MechStats>();
            if (aiController == null) aiController = aiBrain.GetComponent<MechController>();
        }

        if (aiBrain == null || aiStats == null || aiController == null)
            Debug.LogError("MatchManager: Could not find PrototypeAIBrain, MechStats, or MechController in the scene!");

        // --- AUTO-FIND UI TEXT ---
        if (matchText == null)
        {
            GameObject textObj = GameObject.Find("MatchManagerText");
            if (textObj != null)
            {
                matchText = textObj.GetComponent<TextMeshProUGUI>();
            }
            else
            {
                Debug.LogWarning("MatchManager: Could not find a GameObject named 'MatchManagerText' in the scene!");
            }
        }
    }

    private void Start()
    {
        // 1. Hide and lock the cursor as soon as the match scene loads
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        // 2. Immediately disable inputs upon scene load
        SetControlsActive(false);

        // 3. Begin the startup sequence
        StartCoroutine(MatchStartupRoutine());
    }

    private void Update()
    {
        // Monitor health only if the match is actively running
        if (isMatchActive && !matchHasEnded)
        {
            // Safety check in case references are missing
            if (aiStats == null || playerStats == null) return;

            if (aiStats.currentArmorPoints <= 0)
            {
                EndMatch(true); // Player Wins
            }
            else if (playerStats.currentArmorPoints <= 0)
            {
                EndMatch(false); // Player Loses
            }
        }
    }

    private IEnumerator MatchStartupRoutine()
    {
        float timer = startCountdownTime;

        // Display the countdown
        while (timer > 0)
        {
            if (matchText != null) 
            {
                matchText.text = Mathf.CeilToInt(timer).ToString();
            }
            
            timer -= Time.deltaTime;
            yield return null;
        }

        // Match Starts!
        if (matchText != null)
        {
            matchText.text = "START!";
        }

        SetControlsActive(true);
        isMatchActive = true;

        // Clear the "START!" text after 1 second
        yield return new WaitForSeconds(1f);
        if (matchText != null && matchText.text == "START!")
        {
            matchText.text = "";
        }
    }

    private void EndMatch(bool playerWon)
    {
        matchHasEnded = true;
        isMatchActive = false;

        // 1. Disable all controls immediately and halt movement
        SetControlsActive(false);

        // 2. Display the appropriate message and color
        if (matchText != null)
        {
            matchText.text = playerWon ? "YOU WIN" : "YOU LOSE";
            matchText.color = playerWon ? Color.white : Color.red;
        }

        // 3. Process Rewards if the player won
        if (playerWon)
        {
            RewardPlayerCredits();
        }

        // 4. Start the exit sequence
        StartCoroutine(MatchEndRoutine());
    }

    private void RewardPlayerCredits()
    {
        // TODO: Implement your credit rewarding logic here!
        // Example: 
        // int rewardAmount = 5000;
        // GameManager.Instance.AddCredits(rewardAmount);
        // Debug.Log($"Player won! Rewarded {rewardAmount} credits.");
    }

    private IEnumerator MatchEndRoutine()
    {
        // Wait for the specified time while the player reads the Win/Lose message
        yield return new WaitForSeconds(matchEndWaitTime);

        // Bring the cursor back right before we load into the menu so the player can click buttons again
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        // Transport the player back to the main menu (SceneIndex 0)
        SceneManager.LoadScene(menuSceneIndex);
    }

    // --- Helper function to easily toggle both brains and halt momentum ---
    private void SetControlsActive(bool isActive)
    {
        // Toggle the input brains
        if (playerBrain != null) playerBrain.enabled = isActive;
        if (aiBrain != null) aiBrain.enabled = isActive;

        // If we are disabling controls, explicitly wipe the movement intent on the physics controllers
        // so they don't get stuck doing the last action they were told to do.
        if (!isActive)
        {
            if (playerController != null)
            {
                playerController.moveInput = Vector3.zero;
                playerController.isBoosting = false;
                playerController.isJumping = false;
            }

            if (aiController != null)
            {
                aiController.moveInput = Vector3.zero;
                aiController.isBoosting = false;
                aiController.isJumping = false;
            }
        }
    }
}