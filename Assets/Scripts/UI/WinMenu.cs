using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class WinMenu : MonoBehaviour
{
    public GameObject winMenuPanel;
    private PostProcessVolume ppVolume;
    [SerializeField] private LevelManager levelManager;
    [SerializeField] string mainMenuSceneName = "MainMenu";

    public void LevelComplete()
    {
        winMenuPanel.SetActive(true);
        ppVolume.enabled = true;
        Time.timeScale = 0f;
    }

    public void RestartLevel()
    {
        OnExitComplete();
        levelManager.RestartLevel();
    }

    public void ToMainMenu()
    {
        OnExitComplete();
        levelManager.ReturnToMainMenu();
    }

    public void ToNextLevel()
    {
        OnExitComplete();
        levelManager.LoadNextLevel();
    }

    void OnExitComplete()
    {
        winMenuPanel.SetActive(false);
        ppVolume.enabled = false;
        Time.timeScale = 1f;
    }
}
