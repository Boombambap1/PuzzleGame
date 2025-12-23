using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class OptionsMenu : MonoBehaviour
{
    public GameObject settingsMenuUI;
    public GameObject keybindsMenuUI;
    public GameObject mechanicsMenuUI;

    public void OpenSettingsMenu()
    {
        keybindsMenuUI.SetActive(false);
        mechanicsMenuUI.SetActive(false);
        settingsMenuUI.SetActive(true);
    }

    public void OpenKeybindsMenu()
    {
        settingsMenuUI.SetActive(false);
        mechanicsMenuUI.SetActive(false);
        keybindsMenuUI.SetActive(true);
    }

    public void OpenMechanicsMenu()
    {
        settingsMenuUI.SetActive(false);
        keybindsMenuUI.SetActive(false);
        mechanicsMenuUI.SetActive(true);
    }

    public void CloseMenu()
    {
        settingsMenuUI.SetActive(false);
        keybindsMenuUI.SetActive(false);
        mechanicsMenuUI.SetActive(false);
    }
}