using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SceneTracker : MonoBehaviour
{
    public static SceneTracker Instance;

    public string LastScene { get; private set; }
    private static List<string> sceneHistory = new List<string>();
    [SerializeField] string mainMenuSceneName = "MainMenu";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Initialize()
    {
        GameObject go = new GameObject("SceneTracker");
        go.AddComponent<SceneTracker>();
        DontDestroyOnLoad(go);
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LastScene = SceneManager.GetActiveScene().name;
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (sceneHistory.Count == 0 || sceneHistory[sceneHistory.Count - 1] != scene.name)
        {
            sceneHistory.Add(scene.name);
        }
    }

    public void GoToPreviousScene()
    {
        if (sceneHistory.Count >= 2)
        {
            string previousSceneName = sceneHistory[sceneHistory.Count - 2];
            sceneHistory.RemoveAt(sceneHistory.Count - 1);
            sceneHistory.RemoveAt(sceneHistory.Count - 1);
            SceneManager.LoadScene(previousSceneName);
        }
        else
        {
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
