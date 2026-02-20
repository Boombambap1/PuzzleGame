using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UIManager : MonoBehaviour
{
    [Header("UI Panels")]
    public GameObject winPanel;
    public GameObject gameCompletePanel;
    
    [Header("Win Panel Elements")]
    public TextMeshProUGUI winLevelText;
    public Button nextLevelButton;
    public Button restartButton;
    public Button menuButton;
    
    [Header("HUD Elements")]
    public TextMeshProUGUI levelNumberText;
    
    [Header("Game Complete Elements")]
    public TextMeshProUGUI totalLevelsText;
    public Button playAgainButton;
    
    private LevelManager levelManager;
    
    void Start()
    {
        levelManager = FindObjectOfType<LevelManager>();
        
        // Hide panels initially
        if (winPanel != null) winPanel.SetActive(false);
        if (gameCompletePanel != null) gameCompletePanel.SetActive(false);
        
        // Setup buttons
        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(OnNextLevel);
        if (restartButton != null)
            restartButton.onClick.AddListener(OnRestart);
        if (menuButton != null)
            menuButton.onClick.AddListener(OnReturnToMenu);
        if (playAgainButton != null)
            playAgainButton.onClick.AddListener(OnPlayAgain);
        
        UpdateLevelDisplay();
    }
    
    public void ShowWinScreen()
    {
        if (winPanel != null)
        {
            winPanel.SetActive(true);
            
            if (winLevelText != null)
            {
                winLevelText.text = $"Level {levelManager.GetCurrentLevel()} Complete!";
            }
            
            // Hide next button if last level
            if (nextLevelButton != null)
            {
                bool isLastLevel = levelManager.GetCurrentLevel() >= levelManager.GetTotalLevels();
                nextLevelButton.gameObject.SetActive(!isLastLevel);
            }
        }
    }
    
    public void ShowGameCompleteScreen()
    {
        if (gameCompletePanel != null)
        {
            gameCompletePanel.SetActive(true);
            
            if (totalLevelsText != null)
            {
                totalLevelsText.text = $"Congratulations!\nYou completed all {levelManager.GetTotalLevels()} levels!";
            }
        }
    }
    
    public void HideAllPanels()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (gameCompletePanel != null) gameCompletePanel.SetActive(false);
    }
    
    public void OnNextLevel()
    {
        HideAllPanels();
        levelManager.LoadNextLevel();
    }
    
    public void OnRestart()
    {
        HideAllPanels();
        levelManager.RestartLevel();
    }
    
    public void OnReturnToMenu()
    {
        levelManager.ReturnToMainMenu();
    }
    
    public void OnPlayAgain()
    {
        HideAllPanels();
        levelManager.LoadLevel(0); // Start from beginning
    }
    
    public void UpdateLevelDisplay()
    {
        if (levelNumberText != null)
        {
            levelNumberText.text = $"Level {levelManager.GetCurrentLevel()} / {levelManager.GetTotalLevels()}";
        }
    }
}
