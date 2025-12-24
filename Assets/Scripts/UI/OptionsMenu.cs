using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class OptionsMenu : MonoBehaviour
{
    public GameObject settingsMenuUI;
    public GameObject keybindsMenuUI;
    public GameObject mechanicsMenuUI;
    public Button settingsButton;
    public Button keybindsButton;
    public Button mechanicsButton;

    public Color unselectedButton = Color.white;
    public Color selectedButton = new Color(200, 200, 200);

    public void OpenSettingsMenu()
    {
        keybindsMenuUI.SetActive(false);
        mechanicsMenuUI.SetActive(false);
        settingsMenuUI.SetActive(true);
        keybindsButton.image.color = unselectedButton;
        mechanicsButton.image.color = unselectedButton;
        settingsButton.image.color = selectedButton;
    }

    public void OpenKeybindsMenu()
    {
        settingsMenuUI.SetActive(false);
        mechanicsMenuUI.SetActive(false);
        keybindsMenuUI.SetActive(true);
        settingsButton.image.color = unselectedButton;
        mechanicsButton.image.color = unselectedButton;
        keybindsButton.image.color = selectedButton;
    }

    public void OpenMechanicsMenu()
    {
        settingsMenuUI.SetActive(false);
        keybindsMenuUI.SetActive(false);
        mechanicsMenuUI.SetActive(true);
        settingsButton.image.color = unselectedButton;
        keybindsButton.image.color = unselectedButton;
        mechanicsButton.image.color = selectedButton;
    }

    public void CloseMenu()
    {
        settingsMenuUI.SetActive(false);
        keybindsMenuUI.SetActive(false);
        mechanicsMenuUI.SetActive(false);
    }
}