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
    // You can add PaintScreen, ShopScreen, etc. here later!

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
        mainHubPanel.SetActive(false); // Hides the hub and the pop-outs
        ToggleCustomizePanel(); // Closes the customize pop-out if it's open
        assemblyScreen.SetActive(true);
    }

    public void CloseAssemblyScreen()
    {
        assemblyScreen.SetActive(false);
        ToggleCustomizePanel();
        mainHubPanel.SetActive(true);
    }

    // Link this to a "BACK" button on the Assembly screen
    public void ReturnToMainHub()
    {
        assemblyScreen.SetActive(false);
        mainHubPanel.SetActive(true);

        // Hide pop-outs when returning
        if (customizePopOut != null) customizePopOut.SetActive(false);
        if (shopPopOut != null) shopPopOut.SetActive(false);
    }
}