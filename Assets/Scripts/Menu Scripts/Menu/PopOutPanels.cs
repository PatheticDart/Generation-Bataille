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

    [Header("Shop Integration")]
    public GameObject shopScreen; // --- NEW: Drag your main Shop panel here
    public ShopUI shopUI;         // --- NEW: Drag the object with the ShopUI script here

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

    // --- NEW: SHOP ROUTING ---
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
        assemblyScreen.SetActive(true);
    }

    public void OpenPaintScreen()
    {
        CloseAllPopOutsAndScreens();
        paintScreen.SetActive(true);
    }

    public void OpenArenaScreen()
    {
        CloseAllPopOutsAndScreens();
        if (arenaScreen != null) arenaScreen.SetActive(true);
    }

    // --- MASTER RESET ---
    public void ReturnToMainHub()
    {
        CloseAllPopOutsAndScreens();
        mainHubPanel.SetActive(true);
    }

    private void CloseAllPopOutsAndScreens()
    {
        mainHubPanel.SetActive(false);

        if (customizePopOut != null) customizePopOut.SetActive(false);
        if (shopPopOut != null) shopPopOut.SetActive(false);
        if (arenaPopOut != null) arenaPopOut.SetActive(false);

        if (assemblyScreen != null) assemblyScreen.SetActive(false);
        if (paintScreen != null) paintScreen.SetActive(false);
        if (arenaScreen != null) arenaScreen.SetActive(false);
        if (shopScreen != null) shopScreen.SetActive(false);
    }
}