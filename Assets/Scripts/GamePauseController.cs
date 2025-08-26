using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class GamePauseController : MonoBehaviour
{
    [Header("Pause Panel UI")]
    public Button resumeButton;         // Resume game button
    public Button saveButton;           // Save game button
    public Button menuButton;           // Go to menu button
    public Button exitButton;           // Exit game button
    
    [Header("Save Slot Panel UI")]
    public GameObject saveSlotPanel;    // Save slot selection panel
    public Button backToPauseButton;    // Back to pause menu button
    public GameObject pauseMainPanel;   // Main pause panel (Resume, Save, Menu, Exit buttons)
    
    [Header("Save Slot Buttons")]
    public Button saveSlot1Button;      // Save to slot 1
    public Button saveSlot2Button;      // Save to slot 2
    public Button saveSlot3Button;      // Save to slot 3
    
    [Header("Save Slot Info (Optional)")]
    public TextMeshProUGUI slot1InfoText;  // Info text for slot 1
    public TextMeshProUGUI slot2InfoText;  // Info text for slot 2
    public TextMeshProUGUI slot3InfoText;  // Info text for slot 3
    
    // Reference to game controller
    private GameController gameController;
    
    void Start()
    {
        // Get game controller reference
        gameController = FindObjectOfType<GameController>();
        
        // Initialize UI
        if (saveSlotPanel != null)
            saveSlotPanel.SetActive(false);
        if (pauseMainPanel != null)
            pauseMainPanel.SetActive(true);
            
        // Setup button listeners
        SetupButtonListeners();
        
        // Update save slot info
        UpdateSaveSlotInfo();
    }
    
    void SetupButtonListeners()
    {
        // Pause menu buttons
        if (resumeButton != null)
            resumeButton.onClick.AddListener(ResumeGame);
            
        if (saveButton != null)
            saveButton.onClick.AddListener(ShowSaveSlotPanel);
            
        if (menuButton != null)
            menuButton.onClick.AddListener(GoToMenu);
            
        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);
            
        // Save slot panel buttons
        if (backToPauseButton != null)
            backToPauseButton.onClick.AddListener(HideSaveSlotPanel);
            
        // Save slot buttons
        if (saveSlot1Button != null)
            saveSlot1Button.onClick.AddListener(() => SaveToSlot(1));
            
        if (saveSlot2Button != null)
            saveSlot2Button.onClick.AddListener(() => SaveToSlot(2));
            
        if (saveSlot3Button != null)
            saveSlot3Button.onClick.AddListener(() => SaveToSlot(3));
    }
    
    // Resume game
    public void ResumeGame()
    {
        Debug.Log("Resuming game...");
        if (gameController != null)
            gameController.ResumeGame();
    }
    
    // Show save slot selection panel
    public void ShowSaveSlotPanel()
    {
        Debug.Log("Showing save slot panel...");
        
        // Hide pause main panel and show save slot panel
        if (pauseMainPanel != null)
            pauseMainPanel.SetActive(false);
        if (saveSlotPanel != null)
        {
            saveSlotPanel.SetActive(true);
            UpdateSaveSlotInfo();
        }
    }
    
    // Hide save slot selection panel
    public void HideSaveSlotPanel()
    {
        Debug.Log("Hiding save slot panel...");
        
        // Hide save slot panel and show pause main panel
        if (saveSlotPanel != null)
            saveSlotPanel.SetActive(false);
        if (pauseMainPanel != null)
            pauseMainPanel.SetActive(true);
    }
    
    // Save game to specific slot
    public void SaveToSlot(int slotNumber)
    {
        Debug.Log($"Saving game to slot {slotNumber}...");
        if (gameController != null)
        {
            gameController.SaveGameToSlot(slotNumber);
            UpdateSaveSlotInfo();
            
            // Hide save panel after saving and return to pause menu
            HideSaveSlotPanel();
            
            // Show confirmation (you could add a popup here)
            Debug.Log($"Game successfully saved to slot {slotNumber}!");
        }
    }
    
    // Go back to main menu
    public void GoToMenu()
    {
        Debug.Log("Going to main menu...");
        
        // Reset time scale before changing scenes
        Time.timeScale = 1f;
        
        SceneManager.LoadScene("MenuScene");
    }
    
    // Exit game
    public void ExitGame()
    {
        Debug.Log("Exiting game...");
        
        // Reset time scale before exiting
        Time.timeScale = 1f;
        
#if UNITY_EDITOR
        // If running in the Unity editor
        UnityEditor.EditorApplication.isPlaying = false;
#else
        // If running in a built application
        Application.Quit();
#endif
    }
    
    // Update save slot information display
    void UpdateSaveSlotInfo()
    {
        if (gameController != null)
        {
            // Update slot 1 info
            if (slot1InfoText != null)
                slot1InfoText.text = gameController.GetSaveSlotInfo(1);
                
            // Update slot 2 info
            if (slot2InfoText != null)
                slot2InfoText.text = gameController.GetSaveSlotInfo(2);
                
            // Update slot 3 info
            if (slot3InfoText != null)
                slot3InfoText.text = gameController.GetSaveSlotInfo(3);
        }
    }
    
    // Optional: Update UI when enabled
    void OnEnable()
    {
        UpdateSaveSlotInfo();
    }
}
