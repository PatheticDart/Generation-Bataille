using UnityEngine;

public class PopOutPanels : MonoBehaviour
{
    [Header("Main Hub")]
    public GameObject mainHubPanel;

    [Header("Pop-out Panels")]
    public GameObject customizePopOut;
    public GameObject shopPopOut;
    public GameObject arenaPopOut;

    [Header("Full Screen Menus")]
    public GameObject assemblyScreen;
    public GameObject paintScreen;
    public GameObject arenaScreen;
    public GameObject optionsScreen; // --- NEW: Drag your Options panel here

    [Header("Shop Integration")]
    public GameObject shopScreen;
    public ShopUI shopUI;

    private void Start()
    {
        ReturnToMainHub();
    }

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

    public void ToggleArenaPanel()
    {
        if (arenaPopOut != null)
        {
            bool isOpening = !arenaPopOut.activeSelf;
            arenaPopOut.SetActive(isOpening);
        }
    }

    // --- SHOP ROUTING ---
    public void OpenShopBuyMode()
    {
        CloseAllPopOutsAndScreens();
        shopScreen.SetActive(true);
        if (shopUI != null) shopUI.OpenBuyMenu();
    }

    public void OpenShopSellMode()
    {
        CloseAllPopOutsAndScreens();
        shopScreen.SetActive(true);
        if (shopUI != null) shopUI.OpenSellMenu();
    }

    // --- SCREEN ROUTING ---
    public void OpenAssemblyScreen()
    {
        CloseAllPopOutsAndScreens();
        if (assemblyScreen != null) assemblyScreen.SetActive(true);
    }

    public void OpenPaintScreen()
    {
        CloseAllPopOutsAndScreens();
        if (paintScreen != null) paintScreen.SetActive(true);
    }

    public void OpenArenaScreen()
    {
        CloseAllPopOutsAndScreens();
        if (arenaScreen != null) arenaScreen.SetActive(true);
    }

    // --- NEW: OPTIONS ROUTING ---
    public void OpenOptionsScreen()
    {
        CloseAllPopOutsAndScreens();
        if (optionsScreen != null) optionsScreen.SetActive(true);
    }

    // --- MASTER RESET ---
    public void ReturnToMainHub()
    {
        CloseAllPopOutsAndScreens();
        if (mainHubPanel != null) mainHubPanel.SetActive(true);
    }

    private void CloseAllPopOutsAndScreens()
    {
        if (mainHubPanel != null) mainHubPanel.SetActive(false);

        // Hide Pop-outs
        if (customizePopOut != null) customizePopOut.SetActive(false);
        if (shopPopOut != null) shopPopOut.SetActive(false);
        if (arenaPopOut != null) arenaPopOut.SetActive(false);

        // Hide Full Screens
        if (assemblyScreen != null) assemblyScreen.SetActive(false);
        if (paintScreen != null) paintScreen.SetActive(false);
        if (arenaScreen != null) arenaScreen.SetActive(false);
        if (shopScreen != null) shopScreen.SetActive(false);
        if (optionsScreen != null) optionsScreen.SetActive(false); // --- NEW: Ensures Options hides when switching menus
    }
}