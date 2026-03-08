using UnityEngine;

public class PauseManager : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject pauseMenuRoot;
    public GameObject mainViewPanel;
    public GameObject optionsPanel;
    // Add other sub-menus here as you build them

    private bool isPaused = false;

    void Start()
    {
        // Ensure the menu is hidden on start
        if (pauseMenuRoot != null) pauseMenuRoot.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused)
            {
                PauseGame();
            }
            else
            {
                // If we are in the Main View, unpause the game
                if (mainViewPanel.activeSelf)
                {
                    ResumeGame();
                }
                // If we are in Options (or another sub-menu), go back to Main View
                else
                {
                    ReturnToMainView();
                }
            }
        }
    }

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Freezes gameplay and physics

        pauseMenuRoot.SetActive(true);
        ReturnToMainView(); // Always open to the main view

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Resumes gameplay

        pauseMenuRoot.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void ReturnToMainView()
    {
        mainViewPanel.SetActive(true);
        if (optionsPanel != null) optionsPanel.SetActive(false);
    }

    // You can link UI Buttons (like "Options") to this function in the Inspector!
    public void OpenOptionsPanel()
    {
        mainViewPanel.SetActive(false);
        optionsPanel.SetActive(true);
    }
}