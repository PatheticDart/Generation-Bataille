using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuBtnManager : MonoBehaviour
{

    public int mainSceneIndex = 1; //main game index 
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
        SceneManager.LoadScene(mainSceneIndex);
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
