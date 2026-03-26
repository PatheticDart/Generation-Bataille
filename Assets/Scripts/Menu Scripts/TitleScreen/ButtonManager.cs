using UnityEngine;

public class ButtonManager : MonoBehaviour
{
    [Header("SCENE INDEXES")]
    public int titleSceneIndex = 0;
    public int ColosseumSceneIndex = 1;
    public int BurriedCitySceneIndex = 2;

    [Header("PANELS")]
    public GameObject titleScreenContainer;
    //public GameObject newGamePanel;
    public GameObject mainMenuPanel;
    //public GameObject selectionPanel;
    //public GameObject settingsPanel;
    //public GameObject creditsPanel;
    public GameObject opponentViewPanel;
    

    public void opponentSelect()
    {
        opponentViewPanel.SetActive(true);
        titleScreenContainer.SetActive(false);
        mainMenuPanel.SetActive(false);
    }

    public void mainMenu()
    {
        mainMenuPanel.SetActive(true);
        titleScreenContainer.SetActive(false);
        opponentViewPanel.SetActive(false);
    }
}
