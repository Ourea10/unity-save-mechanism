using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using System.Security.Cryptography;
using System.Text;

// Save data structure for JSON serialization
[System.Serializable]
public class GameSaveData
{
    public int score;
    public string saveDateTime;
    public string playerName;
    public int level;
    
    // Upgrade system data
    public int scorePerClickLevel;
    public int prestigeLevel;
    public int baseMultiplier;
    
    public GameSaveData()
    {
        score = 0;
        saveDateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        playerName = "Player";
        level = 1;
        scorePerClickLevel = 0;
        prestigeLevel = 0;
        baseMultiplier = 1;
    }
    
    public GameSaveData(int currentScore, int clickLevel, int prestige, int baseMult)
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
public class SecureGameSaveData
{
    public GameSaveData gameData;
    public string dataHash;
    public string salt;
    
    public SecureGameSaveData(GameSaveData data, string secretKey)
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

public class SaveManager : MonoBehaviour
{
    // Security key for save validation (keep this secret!)
    private const string SAVE_SECRET_KEY = "MyGameSecretKey2024!@#$";
    
    // Singleton instance
    public static SaveManager Instance { get; private set; }
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            
            // Ensure save directory exists
            CreateSaveDirectory();
        }
        else
        {
            Destroy(gameObject);
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
    
    // Save game data to a JSON file with hash validation
    public bool SaveGameToSlot(int slotNumber, int score, int clickLevel, int prestigeLevel, int baseMultiplier)
    {
        try
        {
            // Create save data with all upgrade info
            GameSaveData saveData = new GameSaveData(score, clickLevel, prestigeLevel, baseMultiplier);
            
            // Create secure save data with hash
            SecureGameSaveData secureData = new SecureGameSaveData(saveData, SAVE_SECRET_KEY);
            
            // Convert to JSON
            string jsonData = JsonUtility.ToJson(secureData, true);
            
            // Write to file
            string filePath = GetSaveFilePath(slotNumber);
            File.WriteAllText(filePath, jsonData);
            
            Debug.Log($"Game saved to slot {slotNumber}! Score: {score}");
            Debug.Log($"Click Level: {clickLevel}, Prestige: {prestigeLevel}, Base Multiplier: {baseMultiplier}");
            Debug.Log($"Save file location: {filePath}");
            Debug.Log($"Save data is protected with hash validation");
            
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to save game to slot {slotNumber}: {e.Message}");
            return false;
        }
    }
    
    // Load game data from a JSON file with hash validation
    public GameSaveData LoadGameFromSlot(int slotNumber)
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
                    SecureGameSaveData secureData = JsonUtility.FromJson<SecureGameSaveData>(jsonData);
                    
                    if (secureData != null && secureData.IsValid(SAVE_SECRET_KEY))
                    {
                        Debug.Log($"Game loaded from slot {slotNumber}! Score: {secureData.gameData.score}");
                        Debug.Log($"Click Level: {secureData.gameData.scorePerClickLevel}, Prestige: {secureData.gameData.prestigeLevel}");
                        Debug.Log($"Save date: {secureData.gameData.saveDateTime}");
                        Debug.Log($"Save data integrity verified ✓");
                        return secureData.gameData;
                    }
                    else
                    {
                        Debug.LogWarning($"Save file for slot {slotNumber} has been tampered with! Hash validation failed.");
                        Debug.LogWarning($"The save file may have been modified externally.");
                        return null;
                    }
                }
                catch (Exception)
                {
                    // Try to load as legacy save data (old format without hash)
                    Debug.LogWarning($"Loading legacy save format for slot {slotNumber} (no hash protection)");
                    GameSaveData legacyData = JsonUtility.FromJson<GameSaveData>(jsonData);
                    
                    // Ensure backward compatibility with old saves
                    if (legacyData.baseMultiplier <= 0) legacyData.baseMultiplier = 1;
                    
                    Debug.Log($"Legacy game loaded from slot {slotNumber}! Score: {legacyData.score}");
                    return legacyData;
                }
            }
            else
            {
                Debug.Log($"No save file found for slot {slotNumber}");
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to load game from slot {slotNumber}: {e.Message}");
            return null;
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
                    SecureGameSaveData secureData = JsonUtility.FromJson<SecureGameSaveData>(jsonData);
                    
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
                    GameSaveData legacyData = JsonUtility.FromJson<GameSaveData>(jsonData);
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
    public GameSaveData GetSaveData(int slotNumber)
    {
        return LoadGameFromSlot(slotNumber);
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
    
    // Auto-load functionality
    public void SetAutoLoad(int slotNumber)
    {
        PlayerPrefs.SetInt("ShouldLoadSave", 1);
        PlayerPrefs.SetInt("LoadSlotNumber", slotNumber);
        PlayerPrefs.Save();
    }
    
    public bool ShouldAutoLoad(out int slotNumber)
    {
        bool shouldLoad = PlayerPrefs.GetInt("ShouldLoadSave", 0) == 1;
        slotNumber = PlayerPrefs.GetInt("LoadSlotNumber", 1);
        
        if (shouldLoad)
        {
            // Clear the load flags
            PlayerPrefs.DeleteKey("ShouldLoadSave");
            PlayerPrefs.DeleteKey("LoadSlotNumber");
            PlayerPrefs.Save();
        }
        
        return shouldLoad;
    }
}
