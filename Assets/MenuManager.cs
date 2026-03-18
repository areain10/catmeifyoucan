using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenu; 
    public GameObject pauseMenu;
    public GameObject characters;

    private bool isPaused = false;

    void Start()
    {
        if (mainMenu != null) mainMenu.SetActive(true);
        if (pauseMenu != null) pauseMenu.SetActive(false);

        Time.timeScale = 0f;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else if (!mainMenu.activeSelf) 
                PauseGame();
        }
    }

    public void StartGame()
    {
        mainMenu.SetActive(false);
        characters.SetActive(false);
        Time.timeScale = 1f;
        TagGameManager.Instance.BeginGame(); 
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game!");
        Application.Quit();
    }
        
    public void PauseGame()
    {
        pauseMenu.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;
    }

    public void ResumeGame()
    {
        pauseMenu.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
    }
}