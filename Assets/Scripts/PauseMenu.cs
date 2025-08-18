using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public CameraBlurEffect blurEffect;
    public GameObject pauseButton;

    private bool isPaused = false;

    void Start()
    {
        pauseMenuUI.SetActive(false);
        blurEffect.enabled = false;
    }

    public void TogglePause()
    {
        if (isPaused)
            Resume();
        else
            Pause();
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        blurEffect.enabled = false;
        pauseButton.SetActive(true);
        Time.timeScale = 1f;
        isPaused = false;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        Debug.Log("quit game");
        Application.Quit();
    }

    private void Pause()
    {
        pauseMenuUI.SetActive(true);
        blurEffect.enabled = true;
        pauseButton.SetActive(false);
        Time.timeScale = 0f; // stops the game
        isPaused = true;
    }
}