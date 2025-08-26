using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GameController : MonoBehaviour
{
    [Header("UI References")]
    public Button clickMeButton;        // The main game button to click
    public TextMeshProUGUI scoreText;   // Score display text
    public Button pauseButton;          // Pause button
    public GameObject pausePanel;       // Pause panel to show/hide
    public GameObject gamePanel;        // Game panel to show/hide
    
    [Header("Upgrade UI References")]
    public Button upgradeClickButton;   // Button to upgrade click power
    public Button prestigeButton;       // Button to prestige/reset
    public TextMeshProUGUI upgradeClickText; // Text showing upgrade cost and level
    public TextMeshProUGUI prestigeText;     // Text showing prestige cost and level
    public TextMeshProUGUI clickPowerText;   // Text showing current click power
    
    [Header("Game Stats")]
    public int currentScore = 0;        // Current player score
    
    [Header("Upgrade System")]
    public int scorePerClickLevel = 0;  // Level of score per click upgrade
    public int prestigeLevel = 0;       // Prestige level (resets)
    public int baseMultiplier = 1;      // Base multiplier from prestige
    
    // Upgrade costs
    private int baseUpgradeCost = 10;   // Base cost for click upgrade
    private int basePrestigeCost = 1000; // Base cost for prestige
    
    [Header("Game State")]
    public bool isPaused = false;       // Game pause state
    
    // Reference to pause controller
    private GamePauseController pauseController;
    
    void Start()
    {
        // Initialize game
        currentScore = 0;
        isPaused = false;
        
        // Setup UI
        UpdateScoreDisplay();
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (gamePanel != null)
            gamePanel.SetActive(true);
            
        // Setup button listeners
        SetupButtonListeners();
        
        // Update upgrade UI (only if UI elements are assigned)
        UpdateUpgradeUI();
        
        // Get pause controller reference
        pauseController = FindObjectOfType<GamePauseController>();
        
        // Check if we should load a save file
        CheckForAutoLoad();
    }
    
    void SetupButtonListeners()
    {
        // Main click button
        if (clickMeButton != null)
            clickMeButton.onClick.AddListener(OnClickMeButtonPressed);
            
        // Pause button
        if (pauseButton != null)
            pauseButton.onClick.AddListener(PauseGame);
            
        // Upgrade buttons
        if (upgradeClickButton != null)
        {
            upgradeClickButton.onClick.RemoveAllListeners();
            upgradeClickButton.onClick.AddListener(BuyClickUpgrade);
        }
            
        if (prestigeButton != null)
        {
            prestigeButton.onClick.RemoveAllListeners();
            prestigeButton.onClick.AddListener(BuyPrestige);
        }
    }
    
    // Called when player clicks the main game button
    public void OnClickMeButtonPressed()
    {
        Debug.Log($"OnClickMeButtonPressed called! isPaused: {isPaused}, Time.timeScale: {Time.timeScale}");
        
        if (!isPaused)
        {
            int scoreGain = GetScorePerClick();
            currentScore += scoreGain;
            UpdateScoreDisplay();
            UpdateUpgradeUI();
            Debug.Log($"Score increased by {scoreGain}! Current score: {currentScore}");
        }
        else
        {
            Debug.Log("Click ignored - game is paused");
        }
    }
    
    // Update the score display
    void UpdateScoreDisplay()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + currentScore.ToString();
    }
    
    // Update upgrade UI elements
    void UpdateUpgradeUI()
    {
        try
        {
            // Update click power display
            if (clickPowerText != null)
                clickPowerText.text = $"Click Power: {GetScorePerClick()}";
                
            // Update click upgrade button
            if (upgradeClickText != null)
            {
                int upgradeCost = GetClickUpgradeCost();
                upgradeClickText.text = $"Upgrade Click\nLevel {scorePerClickLevel}\nCost: {upgradeCost}";
            }
            
            // Update prestige button
            if (prestigeText != null)
            {
                int prestigeCost = GetPrestigeCost();
                prestigeText.text = $"Prestige\nLevel {prestigeLevel}\nCost: {prestigeCost}\nBase x{baseMultiplier}";
            }
            
            // Enable/disable buttons based on affordability
            if (upgradeClickButton != null)
                upgradeClickButton.interactable = currentScore >= GetClickUpgradeCost();
                
            if (prestigeButton != null)
                prestigeButton.interactable = currentScore >= GetPrestigeCost();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error updating upgrade UI: {e.Message}");
        }
    }
    
    // Pause the game
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Pause game time
        
        // Hide game panel and show pause panel
        if (gamePanel != null)
            gamePanel.SetActive(false);
        if (pausePanel != null)
            pausePanel.SetActive(true);
            
        Debug.Log("Game paused");
    }
    
    // Resume the game (called by PauseController)
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Resume game time
        
        // Hide pause panel and show game panel
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (gamePanel != null)
            gamePanel.SetActive(true);
            
        Debug.Log("Game resumed");
    }
    
    // Get current score (for saving)
    public int GetCurrentScore()
    {
        return currentScore;
    }
    
    // Set score (for loading)
    public void SetScore(int newScore)
    {
        currentScore = newScore;
        UpdateScoreDisplay();
        UpdateUpgradeUI();
    }
    
    // Load all game data from save file
    private void LoadGameData(GameSaveData data)
    {
        currentScore = data.score;
        scorePerClickLevel = data.scorePerClickLevel;
        prestigeLevel = data.prestigeLevel;
        baseMultiplier = data.baseMultiplier;
        
        // Ensure backward compatibility with old saves
        if (baseMultiplier <= 0) baseMultiplier = 1;
        
        // Ensure game is not paused after loading
        isPaused = false;
        Time.timeScale = 1f;
        
        // Make sure correct panels are visible
        if (pausePanel != null)
            pausePanel.SetActive(false);
        if (gamePanel != null)
            gamePanel.SetActive(true);
        
        UpdateScoreDisplay();
        UpdateUpgradeUI();
        
        // Re-setup button listeners to ensure they work after loading
        SetupButtonListeners();
        
        Debug.Log($"Game loaded successfully! Score: {currentScore}, Click Level: {scorePerClickLevel}, Prestige: {prestigeLevel}");
    }
    
    // Calculate current score per click
    public int GetScorePerClick()
    {
        return (1 + scorePerClickLevel) * baseMultiplier;
    }
    
    // Calculate cost for next click upgrade
    public int GetClickUpgradeCost()
    {
        return baseUpgradeCost + (scorePerClickLevel * 10);
    }
    
    // Calculate cost for next prestige
    public int GetPrestigeCost()
    {
        return basePrestigeCost + (prestigeLevel * 1000);
    }
    
    // Buy click upgrade
    public void BuyClickUpgrade()
    {
        int cost = GetClickUpgradeCost();
        if (currentScore >= cost && !isPaused)
        {
            currentScore -= cost;
            scorePerClickLevel++;
            
            UpdateScoreDisplay();
            UpdateUpgradeUI();
            
            Debug.Log($"Bought click upgrade! Level: {scorePerClickLevel}, New click power: {GetScorePerClick()}");
        }
        else
        {
            Debug.Log($"Cannot afford click upgrade. Need {cost}, have {currentScore}");
        }
    }
    
    // Buy prestige (reset but increase base multiplier)
    public void BuyPrestige()
    {
        int cost = GetPrestigeCost();
        if (currentScore >= cost && !isPaused)
        {
            // Increase prestige level and base multiplier
            prestigeLevel++;
            baseMultiplier++;
            
            // Reset progress but keep prestige bonuses
            currentScore = 0;
            scorePerClickLevel = 0;
            
            UpdateScoreDisplay();
            UpdateUpgradeUI();
            
            Debug.Log($"Prestiged! Level: {prestigeLevel}, New base multiplier: {baseMultiplier}");
            Debug.Log($"Progress reset but now each click upgrade is {baseMultiplier}x more powerful!");
        }
        else
        {
            Debug.Log($"Cannot afford prestige. Need {cost}, have {currentScore}");
        }
    }
    

    
    // Check if we should auto-load a save file (called from menu)
    private void CheckForAutoLoad()
    {
        if (SaveManager.Instance != null && SaveManager.Instance.ShouldAutoLoad(out int slotToLoad))
        {
            LoadGameFromSlot(slotToLoad);
        }
    }
    
    // Save game data using SaveManager
    public void SaveGameToSlot(int slotNumber)
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.SaveGameToSlot(slotNumber, currentScore, scorePerClickLevel, prestigeLevel, baseMultiplier);
        }
        else
        {
            Debug.LogError("SaveManager instance not found!");
        }
    }
    
    // Load game data using SaveManager
    public bool LoadGameFromSlot(int slotNumber)
    {
        if (SaveManager.Instance != null)
        {
            GameSaveData saveData = SaveManager.Instance.LoadGameFromSlot(slotNumber);
            if (saveData != null)
            {
                LoadGameData(saveData);
                return true;
            }
            return false;
        }
        else
        {
            Debug.LogError("SaveManager instance not found!");
            return false;
        }
    }
    
    // Check if a save slot has data
    public bool HasSaveData(int slotNumber)
    {
        if (SaveManager.Instance != null)
        {
            return SaveManager.Instance.HasSaveData(slotNumber);
        }
        return false;
    }
    
    // Get save slot info for display
    public string GetSaveSlotInfo(int slotNumber)
    {
        if (SaveManager.Instance != null)
        {
            return SaveManager.Instance.GetSaveSlotInfo(slotNumber);
        }
        return "SaveManager not found";
    }
    
    // Get full save data for a slot (useful for advanced features)
    public GameSaveData GetSaveData(int slotNumber)
    {
        if (SaveManager.Instance != null)
        {
            return SaveManager.Instance.GetSaveData(slotNumber);
        }
        return null;
    }
    
    // Delete a save slot
    public bool DeleteSaveSlot(int slotNumber)
    {
        if (SaveManager.Instance != null)
        {
            return SaveManager.Instance.DeleteSaveSlot(slotNumber);
        }
        return false;
    }
    
    // Debug method to show save directory path
    [ContextMenu("Show Save Directory Path")]
    public void ShowSaveDirectoryPath()
    {
        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ShowSaveDirectoryPath();
        }
        else
        {
            Debug.LogError("SaveManager instance not found!");
        }
    }
}
