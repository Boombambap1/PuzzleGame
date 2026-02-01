using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject pauseButton;
    public enum OptionsMenuTab
    {
        Settings,
        Keybinds,
        Mechanics
    }
    private PostProcessVolume ppVolume;
    private OptionsMenuTab tabToOpen;
    [SerializeField] string optionsMenuSceneName = "OptionsMenu";
    [SerializeField] string mainMenuSceneName = "MainMenu";
    bool paused = false;

    void Start()
    {
        pauseMenuUI.SetActive(false);
        paused = false;

        ppVolume = Camera.main.gameObject.GetComponent<PostProcessVolume>();
        ppVolume.enabled = false;
    }

    public void Pause()
    {
        pauseMenuUI.SetActive(true);
        ppVolume.enabled = true;
        pauseButton.SetActive(false);
        Time.timeScale = 0f;
        paused = true;
    }

    public void Resume()
    {
        pauseMenuUI.SetActive(false);
        ppVolume.enabled = false;
        pauseButton.SetActive(true);
        Time.timeScale = 1f;
        paused = false;
    }

    public void TogglePause()
    {
        if (paused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void LoadOptionsMenu()
    {
        SceneManager.sceneLoaded += OnOptionsMenuLoaded;
        SceneManager.LoadScene(optionsMenuSceneName);
    }

    void OnOptionsMenuLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != optionsMenuSceneName)
        {
            return;
        }
        SceneManager.sceneLoaded -= OnOptionsMenuLoaded;
        OptionsMenu optionsMenu = FindObjectOfType<OptionsMenu>();
        switch (tabToOpen)
        {
            case OptionsMenuTab.Settings:
                optionsMenu.OpenSettingsMenu();
                break;
            case OptionsMenuTab.Keybinds:
                optionsMenu.OpenKeybindsMenu();
                break;
            case OptionsMenuTab.Mechanics:
                optionsMenu.OpenMechanicsMenu();
                break;
        }
    }

    public void OpenSettingsMenu()
    {
        tabToOpen = OptionsMenuTab.Settings;
        LoadOptionsMenu();
    }

    public void OpenMechanicsMenu()
    {
        tabToOpen = OptionsMenuTab.Mechanics;
        LoadOptionsMenu();
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }
}