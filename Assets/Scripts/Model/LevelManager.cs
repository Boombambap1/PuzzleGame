using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{
    [Header("Level Files")]
    [Tooltip("Drag all your JSON level files here in order")]
    public TextAsset[] levelFiles;
    
    [Header("References")]
    public LevelJsonLoader jsonLoader;
    public GamePhysics gamePhysics;
    
    [Header("Scene Names")]
    [SerializeField] string mainMenuSceneName = "MainMenu";
    
    [Header("Current State")]
    public int currentLevelIndex = 0;
    
    private bool levelComplete = false;
    
    void Start()
    {
        if (jsonLoader == null)
        {
            jsonLoader = FindObjectOfType<LevelJsonLoader>();
        }
        if (gamePhysics == null)
        {
            gamePhysics = FindObjectOfType<GamePhysics>();
        }
        
        // Check if main menu told us which level to start at
        if (PlayerPrefs.HasKey("StartLevel"))
        {
            currentLevelIndex = PlayerPrefs.GetInt("StartLevel");
            PlayerPrefs.DeleteKey("StartLevel"); // Clear after reading
        }
        
        LoadLevel(currentLevelIndex);
    }
    
    void Update()
    {
        // Keyboard shortcuts
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }
        if (Input.GetKeyDown(KeyCode.N) && levelComplete)
        {
            LoadNextLevel();
        }
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ReturnToMainMenu();
        }
    }
    
    public void LoadLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= levelFiles.Length)
        {
            Debug.LogError($"Level index {levelIndex} out of range! Total levels: {levelFiles.Length}");
            return;
        }
        
        currentLevelIndex = levelIndex;
        levelComplete = false;
        
        Debug.Log($"========================================");
        Debug.Log($"Loading Level {currentLevelIndex + 1} of {levelFiles.Length}");
        Debug.Log($"========================================");
        
        // Load JSON
        string json = levelFiles[levelIndex].text;
        jsonLoader.LoadLevelFromJson(json);
        
        // Apply initial gravity after a short delay
        Invoke("ApplyGravity", 0.1f);
        
        // Update UI
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.UpdateLevelDisplay();
        }
    }
    
    private void ApplyGravity()
    {
        if (gamePhysics != null)
        {
            gamePhysics.ApplyGravity();
        }
    }
    
    public void LoadNextLevel()
    {
        if (currentLevelIndex + 1 < levelFiles.Length)
        {
            LoadLevel(currentLevelIndex + 1);
        }
        else
        {
            Debug.Log("========================================");
            Debug.Log("ALL LEVELS COMPLETE! GAME FINISHED!");
            Debug.Log("========================================");
            OnGameComplete();
        }
    }
    
    public void RestartLevel()
    {
        Debug.Log("Restarting level...");
        LoadLevel(currentLevelIndex);
    }
    
    public void ReturnToMainMenu()
    {
        // Save progress before returning
        PlayerPrefs.SetInt("LastCompletedLevel", currentLevelIndex);
        PlayerPrefs.Save();
        
        SceneManager.LoadScene(mainMenuSceneName);
    }
    
    // Called by GamePhysics via SendMessage when win condition is met
    void OnWinConditionMet()
    {
        levelComplete = true;
        
        // Save progress (unlock next level)
        int lastCompleted = PlayerPrefs.GetInt("LastCompletedLevel", -1);
        if (currentLevelIndex > lastCompleted)
        {
            PlayerPrefs.SetInt("LastCompletedLevel", currentLevelIndex);
            PlayerPrefs.Save();
        }
        
        // Notify UI
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ShowWinScreen();
        }
    }
    
    private void OnGameComplete()
    {
        UIManager uiManager = FindObjectOfType<UIManager>();
        if (uiManager != null)
        {
            uiManager.ShowGameCompleteScreen();
        }
    }
    
    public int GetTotalLevels()
    {
        return levelFiles.Length;
    }
    
    public int GetCurrentLevel()
    {
        return currentLevelIndex + 1; // 1-indexed for display
    }
}
