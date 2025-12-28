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

    [SerializeField] private Color unselectedButton = new Color(200, 200, 200);
    [SerializeField] private Color selectedButton = Color.white;


    void SetButtonState(Button button, bool selected)
    {
        Image image = button.image;
        Shadow shadow = button.GetComponent<Shadow>();

        image.color = selected ? selectedButton : unselectedButton;
        shadow.enabled = selected;

        button.GetComponent<ButtonVisualState>().SetSelected(selected);
    }

    void Awake()
    {
        CloseMenu();
        SetButtonState(settingsButton, false);
        SetButtonState(keybindsButton, false);
        SetButtonState(mechanicsButton, false);
    }

    public void OpenSettingsMenu()
    {
        keybindsMenuUI.SetActive(false);
        mechanicsMenuUI.SetActive(false);
        settingsMenuUI.SetActive(true);
        SetButtonState(keybindsButton, false);
        SetButtonState(mechanicsButton, false);
        SetButtonState(settingsButton, true);
    }

    public void OpenKeybindsMenu()
    {
        settingsMenuUI.SetActive(false);
        mechanicsMenuUI.SetActive(false);
        keybindsMenuUI.SetActive(true);
        SetButtonState(settingsButton, false);
        SetButtonState(mechanicsButton, false);
        SetButtonState(keybindsButton, true);
    }

    public void OpenMechanicsMenu()
    {
        settingsMenuUI.SetActive(false);
        keybindsMenuUI.SetActive(false);
        mechanicsMenuUI.SetActive(true);
        SetButtonState(settingsButton, false);
        SetButtonState(keybindsButton, false);
        SetButtonState(mechanicsButton, true);
    }

    public void CloseMenu()
    {
        settingsMenuUI.SetActive(false);
        keybindsMenuUI.SetActive(false);
        mechanicsMenuUI.SetActive(false);
    }
}