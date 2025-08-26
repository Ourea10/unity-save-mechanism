# Unity Save Mechanism - Click & Upgrade Game

A Unity-based incremental clicker game featuring a secure save system, upgrade mechanics, and prestige functionality. This project demonstrates modern Unity development practices with modular architecture and comprehensive data persistence.

## 🎮 Game Features

### Core Gameplay
- **Click-to-Score**: Click the main button to increase your score
- **Dynamic Click Power**: Each click's value increases with upgrades
- **Real-time UI Updates**: Instant feedback on all player actions

### Upgrade System
- **Click Power Upgrades**: Increase score per click
  - Cost: 10, 20, 30, 40... (10 + level × 10)
  - Effect: +1 score per click per level
- **Prestige System**: Reset progress for permanent bonuses
  - Cost: 1000, 2000, 3000... (1000 + prestige × 1000)
  - Effect: Increases base multiplier, making future upgrades more powerful

### Save System
- **Secure JSON Saves**: SHA256 hash validation prevents save tampering
- **Multiple Save Slots**: Support for multiple game saves
- **Backward Compatibility**: Handles legacy save formats
- **Auto-save/Auto-load**: Seamless game state persistence
- **Cross-session Persistence**: Progress saved between game sessions

## 🏗️ Architecture

### Modular Design
The project uses a clean, modular architecture separating concerns:

```
Assets/Scripts/
├── GameController.cs      # Main game logic and UI management
├── SaveManager.cs         # Dedicated save/load system
├── GamePauseController.cs # Pause menu functionality
└── MenuController.cs      # Main menu navigation
```

### Key Components

#### GameController
- Handles all game logic (clicking, upgrades, prestige)
- Manages UI updates and user interactions
- Delegates save operations to SaveManager
- ~390 lines of focused game logic

#### SaveManager
- Singleton pattern for global save access
- Secure save data with cryptographic validation
- Complete save/load/delete functionality
- Scene-persistent with DontDestroyOnLoad
- ~360 lines of dedicated save logic

#### Save Data Structure
```csharp
public class GameSaveData
{
    public int score;                // Current player score
    public int scorePerClickLevel;   // Click upgrade level
    public int prestigeLevel;        // Prestige count
    public int baseMultiplier;       // Prestige bonus multiplier
    public string saveDateTime;      // Save timestamp
    public string playerName;        // Player identifier
    public int level;               // Game level (future use)
}
```

## 📁 Project Structure

```
Unity Save Mechanism/
├── Assets/
│   ├── Scenes/
│   │   ├── GameScene.unity     # Main gameplay scene
│   │   └── MenuScene.unity     # Main menu scene
│   ├── Scripts/
│   │   ├── GameController.cs
│   │   ├── SaveManager.cs
│   │   ├── GamePauseController.cs
│   │   └── MenuController.cs
│   └── TextMesh Pro/           # UI text rendering
├── .gitignore                  # Unity-specific gitignore
└── README.md                   # This file
```

## 🚀 Getting Started

### Prerequisites
- Unity 2020.3 LTS or newer
- TextMeshPro package (usually included)

### Setup Instructions

1. **Clone the Repository**
   ```bash
   git clone <repository-url>
   cd "save mechanism"
   ```

2. **Open in Unity**
   - Launch Unity Hub
   - Click "Open" and select the project folder
   - Unity will import and setup the project

3. **Setup SaveManager**
   - Create an empty GameObject in your scene
   - Name it "SaveManager"
   - Attach the `SaveManager.cs` script
   - The SaveManager will auto-initialize as a singleton

4. **Configure GameController**
   In the Unity Inspector, assign the following UI elements:
   
   **UI References:**
   - Click Me Button
   - Score Text
   - Pause Button
   - Pause Panel
   - Game Panel
   
   **Upgrade UI References:**
   - Upgrade Click Button
   - Prestige Button
   - Upgrade Click Text
   - Prestige Text
   - Click Power Text

5. **Build and Run**
   - Press Play in Unity Editor, or
   - Build for your target platform

## 🎯 How to Play

### Basic Gameplay
1. **Click the main button** to earn points
2. **Buy click upgrades** when you have enough points
3. **Save your progress** using the pause menu
4. **Prestige** when you want to reset for permanent bonuses

### Upgrade Strategy
- **Early Game**: Focus on click upgrades (cheap and effective)
- **Mid Game**: Save up for your first prestige at 1000 points
- **Late Game**: Balance between click upgrades and prestige

### Example Progression
- Start: 1 point per click
- Level 1 upgrade (10 points): 2 points per click
- Level 2 upgrade (20 points): 3 points per click
- First prestige (1000 points): Reset to 0, but base multiplier = 2
- Level 1 after prestige (10 points): 4 points per click (2 × 2)

## 🔒 Save System Details

### Security Features
- **SHA256 Hashing**: Prevents save file tampering
- **Salt Generation**: Random salt for each save enhances security
- **Integrity Validation**: Automatic detection of corrupted saves

### Save File Location
- **Windows**: `%userprofile%/AppData/LocalLow/[CompanyName]/[ProductName]/SaveFiles/`
- **macOS**: `~/Library/Application Support/[CompanyName]/[ProductName]/SaveFiles/`
- **Linux**: `~/.config/unity3d/[CompanyName]/[ProductName]/SaveFiles/`

### Save File Format
```json
{
  "gameData": {
    "score": 1500,
    "scorePerClickLevel": 5,
    "prestigeLevel": 1,
    "baseMultiplier": 2,
    "saveDateTime": "2024-01-15 14:30:22",
    "playerName": "Player",
    "level": 1
  },
  "dataHash": "abc123...",
  "salt": "xyz789..."
}
```

## 🛠️ Technical Implementation

### Upgrade Formulas
```csharp
// Score per click
int scorePerClick = (1 + scorePerClickLevel) * baseMultiplier;

// Click upgrade cost
int upgradeCost = 10 + (scorePerClickLevel * 10);

// Prestige cost  
int prestigeCost = 1000 + (prestigeLevel * 1000);
```

### Singleton SaveManager
```csharp
public static SaveManager Instance { get; private set; }

void Awake()
{
    if (Instance == null)
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    else
    {
        Destroy(gameObject);
    }
}
```

### Secure Save Validation
```csharp
public bool IsValid(string secretKey)
{
    string expectedHash = GenerateHash(gameData.GetDataString(), salt, secretKey);
    return dataHash.Equals(expectedHash);
}
```

## 🔧 Development Features

### Debug Tools
- **Context Menu**: Right-click GameController → "Show Save Directory Path"
- **Console Logging**: Detailed logs for save/load operations
- **Upgrade Tracking**: Real-time upgrade cost and effect logging

### Extensibility
The modular design makes it easy to add:
- New upgrade types
- Additional save data fields
- Different game modes
- Multiplayer features
- Achievement systems

## 🐛 Troubleshooting

### Common Issues

**Buttons not working after loading:**
- Fixed automatically by resetting Time.timeScale and game state

**Save files not found:**
- Check if SaveManager GameObject exists in scene
- Verify SaveManager script is attached

**UI elements not updating:**
- Ensure all UI references are assigned in GameController Inspector
- Check Console for NullReferenceException errors

**Save file corruption:**
- The system automatically detects and reports corrupted saves
- Try loading a different save slot

### Performance Considerations
- SaveManager persists across scenes (DontDestroyOnLoad)
- UI updates only when necessary (score changes, upgrades)
- Minimal file I/O operations (save/load only when requested)

## 📝 Git Features

### Included .gitignore
The project includes a comprehensive Unity .gitignore that excludes:
- Library/, Temp/, Obj/ (Unity generated files)
- Build outputs and logs
- User settings and cache files
- OS-specific files (.DS_Store, Thumbs.db)

### Version Control Ready
- Only source files are tracked
- No platform-specific builds in repository
- Clean commit history focused on code changes

## 🚧 Future Enhancements

### Planned Features
- [ ] Multiple save profiles
- [ ] Save file encryption
- [ ] Cloud save integration
- [ ] Auto-save intervals
- [ ] Save file compression
- [ ] Achievement system
- [ ] Statistics tracking

### Code Improvements
- [ ] Unit tests for save system
- [ ] Save data migration system
- [ ] Custom Unity Inspector tools
- [ ] Performance profiling
- [ ] Memory optimization

## 📄 License

This project is provided as-is for educational and demonstration purposes. Feel free to use, modify, and distribute according to your needs.

## 🤝 Contributing

This is a demonstration project, but suggestions and improvements are welcome! The modular architecture makes it easy to extend functionality.

---

**Created with Unity 2020.3+ | Secure Save System | Modular Architecture**
