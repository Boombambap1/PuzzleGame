using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject canvasOverlay;
    
    public enum OptionsMenuTab
    {
        Settings,
        Keybinds,
        Mechanics
    }
    
    private PostProcessVolume ppVolume;
    private OptionsMenuTab tabToOpen;
    
    [SerializeField] string optionsMenuSceneName = "OptionsMenu";
    [SerializeField] string gameSceneName = "Level1";
    
    void Start()
    {
        canvasOverlay.SetActive(false);
        ppVolume = Camera.main.gameObject.GetComponent<PostProcessVolume>();
        ppVolume.enabled = false;
    }
    
    public void OpenLevelsMenu()
    {
        canvasOverlay.SetActive(true);
        ppVolume.enabled = true;
    }
    
    public void CloseLevelsMenu()
    {
        canvasOverlay.SetActive(false);
        ppVolume.enabled = false;
    }
    
    public void StartGame()
    {
        LoadSpecificLevel(0); 
    }
    

    public void ContinueGame()
    {
        int lastLevel = PlayerPrefs.GetInt("LastCompletedLevel", 0);
        LoadSpecificLevel(lastLevel);
    }
    
    public void LoadSpecificLevel(int levelIndex)
    {
        PlayerPrefs.SetInt("StartLevel", levelIndex);
        PlayerPrefs.Save();
        SceneManager.LoadScene(gameSceneName);
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
    
    public void QuitGame()
    {
        Application.Quit();
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}

