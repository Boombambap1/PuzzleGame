using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject levelsMenuUI;
    private PostProcessVolume ppVolume;

    void Start()
    {
        levelsMenuUI.SetActive(false);

        ppVolume = Camera.main.gameObject.GetComponent<PostProcessVolume>();
        ppVolume.enabled = false;
    }

    public void OpenMenu()
    {
        levelsMenuUI.SetActive(true);
        ppVolume.enabled = true;
    }

    public void CloseMenu()
    {
        levelsMenuUI.SetActive(false);
        ppVolume.enabled = false;
    }

    public void LoadLevel(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }
}
