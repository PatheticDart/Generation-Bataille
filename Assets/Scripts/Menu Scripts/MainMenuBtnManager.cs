using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuBtnManager : MonoBehaviour
{

    public int mainSceneIndex = 1; //main game index 
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

    public void QuitGame()
    {
        Application.Quit();
    }
}
