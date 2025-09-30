using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject pauseButton;
    private PostProcessVolume ppVolume;

    void Start()
    {
        pauseMenuUI.SetActive(false);

        ppVolume = Camera.main.gameObject.GetComponent<PostProcessVolume>();
        ppVolume.enabled = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        ppVolume.enabled = true;
        pauseButton.SetActive(false);
        Time.timeScale = 0f; // stops the game
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        ppVolume.enabled = false;
        pauseButton.SetActive(true);
        Time.timeScale = 1f;
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
}