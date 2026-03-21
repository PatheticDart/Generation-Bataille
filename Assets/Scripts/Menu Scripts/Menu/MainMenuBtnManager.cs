using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuBtnManager : MonoBehaviour
{

    public int mainSceneIndex = 1; //main game index 
    public GameObject opponentSelectPanel;
    public GameObject mainMenuPanel, settingsPanel;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartGame()
    {
        // SceneManager.LoadScene(mainSceneIndex);
        mainMenuPanel.SetActive(false);
        opponentSelectPanel.SetActive(true);
    }

    public void BackToMainMenu()
    {
        opponentSelectPanel.SetActive(false);
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
