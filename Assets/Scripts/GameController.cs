using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using System;
using System.Security.Cryptography;
using System.Text;

// Save data structure for JSON serialization
[System.Serializable]
public class SaveData
{
    public int score;
    public string saveDateTime;
    public string playerName;
    public int level;
    
    // Upgrade system data
    public int scorePerClickLevel;
    public int prestigeLevel;
    public int baseMultiplier;
    
    public SaveData()
    {
        score = 0;
        saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        playerName = "Player";
        level = 1;
        scorePerClickLevel = 0;
        prestigeLevel = 0;
        baseMultiplier = 1;
    }
    
    public SaveData(int currentScore, int clickLevel, int prestige, int baseMult)
    {
        score = currentScore;
        saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        playerName = "Player";
        level = 1;
        scorePerClickLevel = clickLevel;
        prestigeLevel = prestige;
        baseMultiplier = baseMult;
    }
    
    // Get data string for hash calculation (excludes hash itself)
    public string GetDataString()
    {
        return $"{score}|{saveDateTime}|{playerName}|{level}|{scorePerClickLevel}|{prestigeLevel}|{baseMultiplier}";
    }
}

// Secure save container with hash validation
[System.Serializable]
public class SecureSaveData
{
    public SaveData gameData;
    public string dataHash;
    public string salt;
    
    public SecureSaveData(SaveData data, string secretKey)
    {
        gameData = data;
        salt = GenerateRandomSalt();
        dataHash = GenerateHash(data.GetDataString(), salt, secretKey);
    }
    
    // Validate if the save data hasn't been tampered with
    public bool IsValid(string secretKey)
    {
        if (gameData == null || string.IsNullOrEmpty(dataHash) || string.IsNullOrEmpty(salt))
            return false;
            
        string expectedHash = GenerateHash(gameData.GetDataString(), salt, secretKey);
        return dataHash.Equals(expectedHash);
    }
    
    // Generate random salt for additional security
    private string GenerateRandomSalt()
    {
        byte[] saltBytes = new byte[16];
        using (var rng = new RNGCryptoServiceProvider())
        {
            rng.GetBytes(saltBytes);
        }
        return Convert.ToBase64String(saltBytes);
    }
    
    // Generate hash with salt and secret key
    private string GenerateHash(string data, string salt, string secretKey)
    {
        string combinedData = $"{data}|{salt}|{secretKey}";
        using (SHA256 sha256Hash = SHA256.Create())
        {
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(combinedData));
            return Convert.ToBase64String(bytes);
        }
    }
}

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
    
    // Security key for save validation (keep this secret!)
    private const string SAVE_SECRET_KEY = "MyGameSecretKey2024!@#$";
    
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
        
        // Ensure save directory exists
        CreateSaveDirectory();
        
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
        if (!isPaused)
        {
            int scoreGain = GetScorePerClick();
            currentScore += scoreGain;
            UpdateScoreDisplay();
            UpdateUpgradeUI();
            Debug.Log($"Score increased by {scoreGain}! Current score: {currentScore}");
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
    private void LoadGameData(SaveData data)
    {
        currentScore = data.score;
        scorePerClickLevel = data.scorePerClickLevel;
        prestigeLevel = data.prestigeLevel;
        baseMultiplier = data.baseMultiplier;
        
        // Ensure backward compatibility with old saves
        if (baseMultiplier <= 0) baseMultiplier = 1;
        
        UpdateScoreDisplay();
        UpdateUpgradeUI();
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
    
    // Create save directory if it doesn't exist
    private void CreateSaveDirectory()
    {
        string saveDir = GetSaveDirectory();
        if (!Directory.Exists(saveDir))
        {
            Directory.CreateDirectory(saveDir);
            Debug.Log($"Created save directory: {saveDir}");
        Debug.Log($"Full save path: {saveDir}");
        }
    }
    
    // Get save directory path
    private string GetSaveDirectory()
    {
        return Path.Combine(Application.persistentDataPath, "SaveFiles");
    }
    
    // Get save file path for a specific slot
    private string GetSaveFilePath(int slotNumber)
    {
        return Path.Combine(GetSaveDirectory(), $"SaveSlot{slotNumber}.json");
    }
    
    // Check if we should auto-load a save file (called from menu)
    private void CheckForAutoLoad()
    {
        if (PlayerPrefs.GetInt("ShouldLoadSave", 0) == 1)
        {
            int slotToLoad = PlayerPrefs.GetInt("LoadSlotNumber", 1);
            
            // Clear the load flags
            PlayerPrefs.DeleteKey("ShouldLoadSave");
            PlayerPrefs.DeleteKey("LoadSlotNumber");
            PlayerPrefs.Save();
            
            // Load the save
            LoadGameFromSlot(slotToLoad);
        }
    }
    
    // Save game data to a JSON file with hash validation
    public void SaveGameToSlot(int slotNumber)
    {
        try
        {
            // Create save data with all upgrade info
            SaveData saveData = new SaveData(currentScore, scorePerClickLevel, prestigeLevel, baseMultiplier);
            
            // Create secure save data with hash
            SecureSaveData secureData = new SecureSaveData(saveData, SAVE_SECRET_KEY);
            
            // Convert to JSON
            string jsonData = JsonUtility.ToJson(secureData, true);
            
            // Write to file
            string filePath = GetSaveFilePath(slotNumber);
            File.WriteAllText(filePath, jsonData);
            
            Debug.Log($"Game saved to slot {slotNumber}! Score: {currentScore}");
            Debug.Log($"Save file location: {filePath}");
            Debug.Log($"Save data is protected with hash validation");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game to slot {slotNumber}: {e.Message}");
        }
    }
    
    // Load game data from a JSON file with hash validation
    public bool LoadGameFromSlot(int slotNumber)
    {
        try
        {
            string filePath = GetSaveFilePath(slotNumber);
            
            if (File.Exists(filePath))
            {
                // Read JSON from file
                string jsonData = File.ReadAllText(filePath);
                
                // Try to load as secure save data first
                try
                {
                    SecureSaveData secureData = JsonUtility.FromJson<SecureSaveData>(jsonData);
                    
                    if (secureData != null && secureData.IsValid(SAVE_SECRET_KEY))
                    {
                        // Valid secure save - apply all data
                        LoadGameData(secureData.gameData);
                        Debug.Log($"Game loaded from slot {slotNumber}! Score: {secureData.gameData.score}");
                        Debug.Log($"Click Level: {secureData.gameData.scorePerClickLevel}, Prestige: {secureData.gameData.prestigeLevel}");
                        Debug.Log($"Save date: {secureData.gameData.saveDateTime}");
                        Debug.Log($"Save data integrity verified ✓");
                        return true;
                    }
                    else
                    {
                        Debug.LogWarning($"Save file for slot {slotNumber} has been tampered with! Hash validation failed.");
                        Debug.LogWarning($"The save file may have been modified externally.");
                        return false;
                    }
                }
                catch (Exception)
                {
                    // Try to load as legacy save data (old format without hash)
                    Debug.LogWarning($"Loading legacy save format for slot {slotNumber} (no hash protection)");
                    SaveData legacyData = JsonUtility.FromJson<SaveData>(jsonData);
                    LoadGameData(legacyData);
                    Debug.Log($"Legacy game loaded from slot {slotNumber}! Score: {legacyData.score}");
                    return true;
                }
            }
            else
            {
                Debug.Log($"No save file found for slot {slotNumber}");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game from slot {slotNumber}: {e.Message}");
            return false;
        }
    }
    
    // Check if a save slot has data
    public bool HasSaveData(int slotNumber)
    {
        string filePath = GetSaveFilePath(slotNumber);
        return File.Exists(filePath);
    }
    
    // Get save slot info for display
    public string GetSaveSlotInfo(int slotNumber)
    {
        try
        {
            if (HasSaveData(slotNumber))
            {
                string filePath = GetSaveFilePath(slotNumber);
                string jsonData = File.ReadAllText(filePath);
                
                // Try to read as secure save data first
                try
                {
                    SecureSaveData secureData = JsonUtility.FromJson<SecureSaveData>(jsonData);
                    
                    if (secureData != null && secureData.IsValid(SAVE_SECRET_KEY))
                    {
                        return $"Score: {secureData.gameData.score}\nClick Lv: {secureData.gameData.scorePerClickLevel} | Prestige: {secureData.gameData.prestigeLevel}\n{secureData.gameData.saveDateTime.Substring(0, 16)}";
                    }
                    else
                    {
                        return "Corrupted";
                    }
                }
                catch (Exception)
                {
                    // Try legacy format
                    SaveData legacyData = JsonUtility.FromJson<SaveData>(jsonData);
                    return $"Score: {legacyData.score}\nLegacy Save\n{legacyData.saveDateTime.Substring(0, 16)}";
                }
            }
            else
            {
                return "Empty Slot";
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to read save slot {slotNumber} info: {e.Message}");
            return "Error Reading Slot";
        }
    }
    
    // Get full save data for a slot (useful for advanced features)
    public SaveData GetSaveData(int slotNumber)
    {
        try
        {
            if (HasSaveData(slotNumber))
            {
                string filePath = GetSaveFilePath(slotNumber);
                string jsonData = File.ReadAllText(filePath);
                
                // Try secure format first
                try
                {
                    SecureSaveData secureData = JsonUtility.FromJson<SecureSaveData>(jsonData);
                    if (secureData != null && secureData.IsValid(SAVE_SECRET_KEY))
                    {
                        return secureData.gameData;
                    }
                }
                catch (Exception)
                {
                    // Try legacy format
                    return JsonUtility.FromJson<SaveData>(jsonData);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to get save data for slot {slotNumber}: {e.Message}");
        }
        
        return null;
    }
    
    // Delete a save slot
    public bool DeleteSaveSlot(int slotNumber)
    {
        try
        {
            string filePath = GetSaveFilePath(slotNumber);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
                Debug.Log($"Deleted save slot {slotNumber}");
                return true;
            }
            else
            {
                Debug.Log($"Save slot {slotNumber} doesn't exist");
                return false;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to delete save slot {slotNumber}: {e.Message}");
            return false;
        }
    }
    
    // Debug method to show save directory path
    [ContextMenu("Show Save Directory Path")]
    public void ShowSaveDirectoryPath()
    {
        string saveDir = GetSaveDirectory();
        Debug.Log($"Save directory: {saveDir}");
        
        // Try to open the directory (Windows only)
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN
        if (Directory.Exists(saveDir))
        {
            System.Diagnostics.Process.Start("explorer.exe", saveDir.Replace('/', '\\'));
        }
#endif
    }
}
