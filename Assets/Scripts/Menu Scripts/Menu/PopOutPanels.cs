using UnityEngine;
using UnityEngine.UI;

public class PopOutPanels : MonoBehaviour
{
    [Header("Pop-out Panels")]
    public GameObject customizePanel;
    public GameObject shopPanel;
    public GameObject assemblyPanel;

    private void Start()
    {
        // Ensure panels are hidden when the scene starts
        if (customizePanel != null) customizePanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
    }

    // Call this from the Customize Button's OnClick event
    public void ToggleCustomizePanel()
    {
        if (customizePanel != null)
        {
            // Toggle the state of the customize panel
            bool isOpening = !customizePanel.activeSelf;
            customizePanel.SetActive(isOpening);

            // If we are opening the Customize panel, make sure the Shop panel is closed
            if (isOpening && shopPanel != null)
            {
                shopPanel.SetActive(false);
                assemblyPanel.SetActive(false);
            }
        }
    }

    // Call this from the Shop Button's OnClick event
    public void ToggleShopPanel()
    {
        if (shopPanel != null)
        {
            // Toggle the state of the shop panel
            bool isOpening = !shopPanel.activeSelf;
            shopPanel.SetActive(isOpening);

            // If we are opening the Shop panel, make sure the Customize panel is closed
            if (isOpening && customizePanel != null)
            {
                customizePanel.SetActive(false);
                assemblyPanel.SetActive(false);
            }
        }
    }

    public void ToggleAssemblyPanel()
    {
        if (shopPanel != null)
        {
            // Toggle the state of the shop panel
            bool isOpening = !shopPanel.activeSelf;
            assemblyPanel.SetActive(isOpening);

            if (isOpening && customizePanel != null)
            {
                customizePanel.SetActive(false);
                shopPanel.SetActive(false);
            }
        }
    }
}