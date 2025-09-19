# Infrastructure Layer

## 📋 Mục đích
Infrastructure layer chứa Unity-specific glue code và platform integrations. Đây là layer chịu trách nhiệm kết nối game logic với Unity engine và external systems, xử lý low-level concerns như data management, performance optimization, và platform-specific features.

## 🏗️ Cấu trúc thư mục

### 📁 Data/
**Mục đích**: Data management và persistence
- ScriptableObject definitions
- Addressable assets management
- Data loading/streaming
- Configuration management

**Thành phần chính**:
- `DataManager.cs` - Central data coordination
- `ScriptableObjectDatabase.cs` - SO management
- `AddressableLoader.cs` - Asset loading
- `ConfigurationManager.cs` - Config handling
- `DataStreamer.cs` - Streaming data management

### 📁 Performance/
**Mục đích**: Performance monitoring và optimization
- Frame rate monitoring
- Memory management
- Profiling integration
- Performance metrics

**Thành phần chính**:
- `PerformanceMonitor.cs` - Performance tracking
- `MemoryManager.cs` - Memory optimization
- `FPSCounter.cs` - Frame rate monitoring
- `ProfilerIntegration.cs` - Unity Profiler integration
- `OptimizationManager.cs` - Auto-optimization

### 📁 Input/
**Mục đích**: Input handling và device management
- Input system integration
- Device-specific handling
- Input remapping
- Accessibility input options

**Thành phần chính**:
- `InputManager.cs` - Central input coordination
- `InputMapper.cs` - Input mapping/remapping
- `DeviceManager.cs` - Device detection
- `TouchInputHandler.cs` - Touch-specific input
- `AccessibilityInput.cs` - Accessibility support

### 📁 Scene/
**Mục đích**: Scene management và transitions
- Scene loading/unloading
- Scene transitions
- Async scene operations
- Scene data persistence

**Thành phần chính**:
- `SceneManager.cs` - Scene coordination
- `SceneTransition.cs` - Transition handling
- `AsyncSceneLoader.cs` - Async operations
- `SceneDataManager.cs` - Cross-scene data
- `LoadingScreen.cs` - Loading UI integration

### 📁 Debug/
**Mục đích**: Development tools và debugging utilities
- Debug overlays
- Console commands
- Development cheats
- Logging systems

**Thành phần chính**:
- `DebugConsole.cs` - In-game debug console
- `DebugOverlay.cs` - Debug information display
- `LogManager.cs` - Centralized logging
- `CheatManager.cs` - Development cheats
- `DebugCommands.cs` - Console command definitions

## ⚡ Đặc điểm chính

### ✅ Platform Abstraction
- Cross-platform compatibility
- Platform-specific optimizations
- Device capability detection
- API abstraction layers

### ✅ Unity Integration
- Deep Unity engine integration
- Unity lifecycle management
- Editor tool integration
- Build pipeline support

### ✅ Performance Focus
- Low-level optimizations
- Resource management
- Memory pooling
- Async operations

## 📐 Quy tắc thiết kế

### ✅ Nên làm:
- Abstract platform-specific code
- Provide clean APIs cho higher layers
- Implement proper resource cleanup
- Use async/await cho blocking operations

### ❌ Không nên:
- Expose Unity-specific details
- Create dependencies on game logic
- Ignore platform differences
- Block main thread với heavy operations

## 🔗 Dependencies
- **Depends on**: Unity Engine, Platform APIs, Foundation
- **Provides services to**: All other layers
- **External integrations**: Analytics, Ads, Cloud services

## 📊 Service Architecture

```
Game Layers
     ↓
Infrastructure Services
     ↓
Unity Engine + Platform APIs
```

## 📝 Ví dụ sử dụng

```csharp
// Data Manager
public class DataManager : MonoBehaviour
{
    private static DataManager instance;
    public static DataManager Instance => instance;
    
    [SerializeField] private AddressableLoader addressableLoader;
    [SerializeField] private ConfigurationManager configManager;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            Initialize();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public async Task<T> LoadDataAsync<T>(string address) where T : ScriptableObject
    {
        return await addressableLoader.LoadAssetAsync<T>(address);
    }
    
    public void SaveUserPreferences(UserPreferences prefs)
    {
        configManager.SaveConfiguration("UserPrefs", prefs);
    }
}

// Performance Monitor
public class PerformanceMonitor : MonoBehaviour
{
    [SerializeField] private float targetFrameRate = 60f;
    [SerializeField] private int memoryThreshold = 512; // MB
    
    private float frameTime;
    private long lastMemoryUsage;
    
    private void Update()
    {
        MonitorFrameRate();
        MonitorMemoryUsage();
        
        if (ShouldOptimize())
        {
            TriggerOptimization();
        }
    }
    
    private void MonitorFrameRate()
    {
        frameTime = Time.unscaledDeltaTime;
        if (frameTime > 1f / targetFrameRate * 1.5f)
        {
            EventBus.Publish(new PerformanceWarningEvent("Low FPS detected"));
        }
    }
    
    private void TriggerOptimization()
    {
        EventBus.Publish(new OptimizationRequestedEvent());
    }
}

// Input Manager
public class InputManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset inputActions;
    
    private InputActionMap gameplayMap;
    private InputActionMap uiMap;
    
    private void Awake()
    {
        gameplayMap = inputActions.FindActionMap("Gameplay");
        uiMap = inputActions.FindActionMap("UI");
        
        SetupInputBindings();
    }
    
    private void SetupInputBindings()
    {
        gameplayMap.FindAction("Move").performed += OnMove;
        gameplayMap.FindAction("Jump").performed += OnJump;
        gameplayMap.FindAction("Attack").performed += OnAttack;
    }
    
    private void OnMove(InputAction.CallbackContext context)
    {
        var movement = context.ReadValue<Vector2>();
        EventBus.Publish(new InputMoveEvent(movement));
    }
    
    public void SwitchToUIMode()
    {
        gameplayMap.Disable();
        uiMap.Enable();
    }
}

// Scene Manager
public class SceneManager : MonoBehaviour
{
    [SerializeField] private SceneTransition transition;
    [SerializeField] private LoadingScreen loadingScreen;
    
    public async Task LoadSceneAsync(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
    {
        // Start transition
        await transition.FadeOut();
        
        // Show loading screen
        loadingScreen.Show();
        
        // Load scene asynchronously
        var asyncOperation = UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(sceneName, mode);
        
        while (!asyncOperation.isDone)
        {
            loadingScreen.UpdateProgress(asyncOperation.progress);
            await Task.Yield();
        }
        
        // Hide loading screen
        loadingScreen.Hide();
        
        // Complete transition
        await transition.FadeIn();
        
        EventBus.Publish(new SceneLoadedEvent(sceneName));
    }
}

// Debug Console
public class DebugConsole : MonoBehaviour
{
    [SerializeField] private bool enableInBuild = false;
    [SerializeField] private KeyCode toggleKey = KeyCode.BackQuote;
    
    private Dictionary<string, System.Action<string[]>> commands = new();
    private bool isVisible = false;
    
    private void Awake()
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (enableInBuild)
        {
            RegisterCommands();
        }
        #endif
    }
    
    private void RegisterCommands()
    {
        commands["god"] = GodModeCommand;
        commands["give"] = GiveItemCommand;
        commands["teleport"] = TeleportCommand;
        commands["fps"] = ShowFPSCommand;
    }
    
    private void GodModeCommand(string[] args)
    {
        var enable = args.Length > 0 && args[0].ToLower() == "on";
        EventBus.Publish(new DebugGodModeEvent(enable));
        Log($"God mode {(enable ? "enabled" : "disabled")}");
    }
}
```

## 🏗️ Service Locator Pattern

```csharp
public class ServiceLocator : MonoBehaviour
{
    private static Dictionary<Type, object> services = new();
    
    public static void RegisterService<T>(T service)
    {
        services[typeof(T)] = service;
    }
    
    public static T GetService<T>()
    {
        return services.TryGetValue(typeof(T), out var service) ? (T)service : default;
    }
    
    public static bool IsServiceRegistered<T>()
    {
        return services.ContainsKey(typeof(T));
    }
}

// Usage in other layers
public class GameManager : MonoBehaviour
{
    private void Start()
    {
        var dataManager = ServiceLocator.GetService<DataManager>();
        var inputManager = ServiceLocator.GetService<InputManager>();
    }
}
```

## 🔧 Configuration Management

```csharp
[CreateAssetMenu(fileName = "GameConfig", menuName = "Infrastructure/Game Configuration")]
public class GameConfiguration : ScriptableObject
{
    [Header("Performance")]
    public int targetFrameRate = 60;
    public bool enableVSync = false;
    public int textureQuality = 2;
    
    [Header("Audio")]
    public float masterVolume = 1f;
    public float musicVolume = 0.7f;
    public float sfxVolume = 0.8f;
    
    [Header("Input")]
    public float mouseSensitivity = 1f;
    public bool invertY = false;
}
```

## ⚡ Performance Monitoring

```csharp
public class PerformanceMetrics
{
    public float AverageFrameTime { get; set; }
    public long MemoryUsage { get; set; }
    public int DrawCalls { get; set; }
    public float CPUTime { get; set; }
    public float GPUTime { get; set; }
}

public class PerformanceProfiler : MonoBehaviour
{
    private PerformanceMetrics currentMetrics = new();
    
    private void Update()
    {
        CollectMetrics();
        
        if (MetricsExceedThresholds())
        {
            EventBus.Publish(new PerformanceIssueEvent(currentMetrics));
        }
    }
}
```

## 🧪 Testing Strategy
- **Integration Tests**: Service interactions
- **Performance Tests**: Memory và frame rate benchmarks
- **Platform Tests**: Cross-platform compatibility
- **Load Tests**: Asset loading performance
