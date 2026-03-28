using UnityEngine;

public class PopOutPanels : MonoBehaviour
{
    [Header("Main Hub")]
    public GameObject mainHubPanel; // The main screen with Customize, Shop, Arena, etc.

    [Header("Pop-out Panels")]
    public GameObject customizePopOut;
    public GameObject shopPopOut;

    [Header("Full Screen Menus")]
    public GameObject assemblyScreen;
    public GameObject paintScreen; // --- NEW: Added Paint Screen Reference ---

    private void Start()
    {
        // Ensure we start in a clean state
        ReturnToMainHub();
    }

    // --- POP-OUT TOGGLES ---
    public void ToggleCustomizePanel()
    {
        if (customizePopOut != null)
        {
            bool isOpening = !customizePopOut.activeSelf;
            customizePopOut.SetActive(isOpening);

            if (isOpening && shopPopOut != null) shopPopOut.SetActive(false);
        }
    }

    public void ToggleShopPanel()
    {
        if (shopPopOut != null)
        {
            bool isOpening = !shopPopOut.activeSelf;
            shopPopOut.SetActive(isOpening);

            if (isOpening && customizePopOut != null) customizePopOut.SetActive(false);
        }
    }

    // --- SCREEN ROUTING ---

    // Link this to the "ASSEMBLY" button inside your Customize Pop-Out
    public void OpenAssemblyScreen()
    {
        mainHubPanel.SetActive(false);

        // Safely close the pop-out only if it is currently open
        if (customizePopOut != null && customizePopOut.activeSelf) ToggleCustomizePanel();

        if (paintScreen != null) paintScreen.SetActive(false); // Safety catch
        assemblyScreen.SetActive(true);
    }

    public void CloseAssemblyScreen()
    {
        assemblyScreen.SetActive(false);

        // Re-open the customize pop-out when backing out
        if (customizePopOut != null && !customizePopOut.activeSelf) ToggleCustomizePanel();

        mainHubPanel.SetActive(true);
    }

    // --- NEW: PAINT SCREEN ROUTING ---

    // Link this to the "PAINT" button inside your Customize Pop-Out
    public void OpenPaintScreen()
    {
        mainHubPanel.SetActive(false);

        // Safely close the pop-out only if it is currently open
        if (customizePopOut != null && customizePopOut.activeSelf) ToggleCustomizePanel();

        if (assemblyScreen != null) assemblyScreen.SetActive(false); // Safety catch
        paintScreen.SetActive(true);
    }

    public void ClosePaintScreen()
    {
        paintScreen.SetActive(false);

        // Re-open the customize pop-out when backing out
        if (customizePopOut != null && !customizePopOut.activeSelf) ToggleCustomizePanel();

        mainHubPanel.SetActive(true);
    }

    // --- MASTER RESET ---

    // Link this to a hard "BACK" button if you want to bypass the pop-out and just go straight to the hub
    public void ReturnToMainHub()
    {
        if (assemblyScreen != null) assemblyScreen.SetActive(false);
        if (paintScreen != null) paintScreen.SetActive(false); // --- NEW: Hide paint screen ---

        mainHubPanel.SetActive(true);

        // Hide pop-outs when returning
        if (customizePopOut != null) customizePopOut.SetActive(false);
        if (shopPopOut != null) shopPopOut.SetActive(false);
    }
}