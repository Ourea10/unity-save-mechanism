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
    
    public SaveData()
    {
        score = 0;
        saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        playerName = "Player";
        level = 1;
    }
    
    public SaveData(int currentScore)
    {
        score = currentScore;
        saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        playerName = "Player";
        level = 1;
    }
    
    // Get data string for hash calculation (excludes hash itself)
    public string GetDataString()
    {
        return $"{score}|{saveDateTime}|{playerName}|{level}";
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
    
    [Header("Game Stats")]
    public int currentScore = 0;        // Current player score
    
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
    }
    
    // Called when player clicks the main game button
    public void OnClickMeButtonPressed()
    {
        if (!isPaused)
        {
            currentScore++;
            UpdateScoreDisplay();
            Debug.Log($"Score increased! Current score: {currentScore}");
        }
    }
    
    // Update the score display
    void UpdateScoreDisplay()
    {
        if (scoreText != null)
            scoreText.text = "Score: " + currentScore.ToString();
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
            // Create save data
            SaveData saveData = new SaveData(currentScore);
            
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
                        // Valid secure save - apply data
                        SetScore(secureData.gameData.score);
                        Debug.Log($"Game loaded from slot {slotNumber}! Score: {secureData.gameData.score}");
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
                    SetScore(legacyData.score);
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
                        return $"Score: {secureData.gameData.score}\n{secureData.gameData.saveDateTime.Substring(0, 16)}";
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
                    return $"Score: {legacyData.score}\n{legacyData.saveDateTime.Substring(0, 16)}";
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
