using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject loadPanel;     // Load Panel containing save slots
    public GameObject savePanel;     // Save Panel (if different from load panel)
    public Button playButton;        // Play Game button
    public Button loadButton;        // Load Save button
    public Button exitButton;       // Exit button
    public Button backButton;       // Back button in load panel
    
    [Header("Save Slot Buttons")]
    public Button slot1Button;      // Save Slot 1
    public Button slot2Button;      // Save Slot 2
    public Button slot3Button;      // Save Slot 3
    
    void Start()
    {
        // // Initialize UI
        // if (loadPanel != null)
        //     loadPanel.SetActive(false);
            
        // Setup button listeners
        SetupButtonListeners();
    }
    
    void SetupButtonListeners()
    {
        // Main menu buttons
        if (playButton != null)
            playButton.onClick.AddListener(PlayGame);
            
        if (loadButton != null)
            loadButton.onClick.AddListener(ShowLoadPanel);
            
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
            
        if (backButton != null)
            backButton.onClick.AddListener(HideLoadPanel);
            
        // Save slot buttons
        if (slot1Button != null)
            slot1Button.onClick.AddListener(() => LoadSaveSlot(1));
            
        if (slot2Button != null)
            slot2Button.onClick.AddListener(() => LoadSaveSlot(2));
            
        if (slot3Button != null)
            slot3Button.onClick.AddListener(() => LoadSaveSlot(3));
    }
    
    // Play Game - Load GameScene
    public void PlayGame()
    {
        Debug.Log("Starting new game...");
        // Option 1: Load by name (requires scene in Build Settings)
        SceneManager.LoadScene("GameScene");
        
        // Option 2: Alternative - Load by path (uncomment if Build Settings method doesn't work)
        // SceneManager.LoadScene("Assets/Scenes/GameScene.unity");
    }
    
    // Show Load Save Panel
    public void ShowLoadPanel()
    {
        Debug.Log("Showing load save panel...");
        if (loadPanel != null) {
            loadPanel.SetActive(false);
            savePanel.SetActive(true);
        }
    }
    
    // Hide Load Save Panel
    public void HideLoadPanel()
    {
        Debug.Log("Hiding load save panel...");
        if (savePanel != null){
            savePanel.SetActive(false);
            loadPanel.SetActive(true);
        }
    }
    
    // Load specific save slot
    public void LoadSaveSlot(int slotNumber)
    {
        Debug.Log($"Loading save slot {slotNumber}...");
        
        // Check if save slot has data
        if (HasSaveData(slotNumber))
        {
            // Store which slot to load (we'll load it in GameScene)
            PlayerPrefs.SetInt("LoadSlotNumber", slotNumber);
            PlayerPrefs.SetInt("ShouldLoadSave", 1);
            PlayerPrefs.Save();
            
            Debug.Log($"Loading save slot {slotNumber} and switching to GameScene");
            SceneManager.LoadScene("GameScene");
        }
        else
        {
            Debug.Log($"Save slot {slotNumber} is empty!");
            // You could show a popup here saying "Empty slot"
        }
    }
    
    // Check if a save slot has data (mirrors GameController logic)
    private bool HasSaveData(int slotNumber)
    {
        string saveDir = System.IO.Path.Combine(Application.persistentDataPath, "SaveFiles");
        string filePath = System.IO.Path.Combine(saveDir, $"SaveSlot{slotNumber}.json");
        return System.IO.File.Exists(filePath);
    }
    
    // Exit Game
    public void ExitGame()
    {
        Debug.Log("Exiting game...");
        
#if UNITY_EDITOR
        // If running in the Unity editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // If running in a built application
        Application.Quit();
#endif
    }
    
    // Optional: You can call these methods directly from UI buttons instead of using the listeners
    // Just assign these methods to the OnClick events in the Inspector
}
