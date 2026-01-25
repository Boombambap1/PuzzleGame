using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject levelsMenuUI;
    public enum OptionsMenuTab
    {
        Settings,
        Keybinds,
        Mechanics
    }
    private PostProcessVolume ppVolume;
    private OptionsMenuTab tabToOpen;
    [SerializeField] string optionsMenuSceneName = "OptionsMenu";

    void Start()
    {
        levelsMenuUI.SetActive(false);

        ppVolume = Camera.main.gameObject.GetComponent<PostProcessVolume>();
        ppVolume.enabled = false;
    }

    public void OpenLevelsMenu()
    {
        levelsMenuUI.SetActive(true);
        ppVolume.enabled = true;
    }

    public void CloseLevelsMenu()
    {
        levelsMenuUI.SetActive(false);
        ppVolume.enabled = false;
    }

    public void LoadLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
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

    public void OpenKeybindsMenu()
    {
        tabToOpen = OptionsMenuTab.Keybinds;
        LoadOptionsMenu();
    }

    public void OpenMechanicsMenu()
    {
        tabToOpen = OptionsMenuTab.Mechanics;
        LoadOptionsMenu();
    }
}
