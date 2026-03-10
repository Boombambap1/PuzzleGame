using UnityEngine;
using UnityEngine.SceneManagement;
using NewArch;

namespace NewArch
{

/// <summary>
/// Manages level loading, restarting, and progression.
/// Drop-in replacement for LevelManager — old LevelManager is untouched.
/// </summary>
public class LevelManagerV2 : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Level Files")]
    [Tooltip("Drag all JSON level files here in order.")]
    public TextAsset[] levelFiles;

    [Header("References")]
    public LevelJsonLoaderV2 jsonLoader;
    public GamePhysicsV2     gamePhysics;
    [SerializeField] private PauseMenu pauseMenu;

    [Header("Scene Names")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Header("Current State")]
    public int currentLevelIndex = 0;

    // ── Private State ─────────────────────────────────────────────────────────

    private bool levelComplete = false;

    // ── Unity Lifecycle ───────────────────────────────────────────────────────

    void Start()
    {
        if (jsonLoader  == null) jsonLoader  = FindFirstObjectByType<LevelJsonLoaderV2>();
        if (gamePhysics == null) gamePhysics = FindFirstObjectByType<GamePhysicsV2>();

        // Check if the main menu specified a starting level.
        if (PlayerPrefs.HasKey("StartLevel"))
        {
            currentLevelIndex = PlayerPrefs.GetInt("StartLevel");
            PlayerPrefs.DeleteKey("StartLevel");
        }

        LoadLevel(currentLevelIndex);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))      RestartLevel();
        if (Input.GetKeyDown(KeyCode.N) && levelComplete) LoadNextLevel();
        if (Input.GetKeyDown(KeyCode.Escape)) ReturnToMainMenu();
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelFiles.Length)
        {
            Debug.LogError($"[LevelManagerV2] Level index {levelIndex} out of range. Total levels: {levelFiles.Length}");
            return;
        }

        CancelInvoke();
        gamePhysics?.CancelInvoke();

        currentLevelIndex = levelIndex;
        levelComplete     = false;

        Debug.Log($"[LevelManagerV2] Loading level {currentLevelIndex + 1} of {levelFiles.Length}");

        jsonLoader.LoadLevelFromJson(levelFiles[levelIndex].text);

        FindFirstObjectByType<UIManager>()?.UpdateLevelDisplay();
    }

    public void LoadNextLevel()
    {
        if (currentLevelIndex + 1 < levelFiles.Length)
        {
            LoadLevel(currentLevelIndex + 1);
        }
        else
        {
            Debug.Log("[LevelManagerV2] All levels complete!");
            FindFirstObjectByType<UIManager>()?.ShowGameCompleteScreen();
        }
    }

    public void RestartLevel()
    {
        Debug.Log("[LevelManagerV2] Restarting level.");
        LoadLevel(currentLevelIndex);
        pauseMenu?.Resume();
    }

    public void ReturnToMainMenu()
    {
        PlayerPrefs.SetInt("LastCompletedLevel", currentLevelIndex);
        PlayerPrefs.Save();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public int GetTotalLevels()   => levelFiles.Length;
    public int GetCurrentLevel()  => currentLevelIndex + 1; // 1-indexed for display

    // ── Win Condition ─────────────────────────────────────────────────────────

    /// <summary>
    /// Called by GamePhysicsV2 via SendMessage when the win condition is met.
    /// </summary>
    void OnWinConditionMet()
    {
        levelComplete = true;

        // Unlock next level if this is the furthest the player has reached.
        int lastCompleted = PlayerPrefs.GetInt("LastCompletedLevel", -1);
        if (currentLevelIndex > lastCompleted)
        {
            PlayerPrefs.SetInt("LastCompletedLevel", currentLevelIndex);
            PlayerPrefs.Save();
        }

        FindFirstObjectByType<UIManager>()?.ShowWinScreen();
    }
}

} // namespace NewArch
