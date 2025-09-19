using UnityEngine;
using Foundation.Events;
using Foundation.Utils;

namespace Foundation.Architecture
{
    /// <summary>
    /// Base Game Manager - điều phối tất cả managers khác.
    /// Entry point cho toàn bộ game systems.
    /// </summary>
    public class GameManager : PersistentSingleton<GameManager>
    {
        [Header("Game State")]
        [SerializeField] private GameState currentState = GameState.Loading;
        [SerializeField] private bool pauseOnFocusLoss = true;
        
        [Header("Debug")]
        [SerializeField] private bool enableDebugMode = false;
        [SerializeField] private bool showPerformanceStats = false;
        
        // Events
        public static System.Action<GameState> OnGameStateChanged;
        public static System.Action OnGamePaused;
        public static System.Action OnGameResumed;
        
        // Properties
        public GameState CurrentState => currentState;
        public bool IsPaused => currentState == GameState.Paused;
        public bool IsPlaying => currentState == GameState.Playing;
        
        #region Unity Lifecycle
        
        protected override void OnAwakeInstance()
        {
            base.OnAwakeInstance();
            
            // Initialize core systems
            InitializeCoreystems();
            
            DebugUtils.Log("[GameManager] Game Manager initialized");
        }
        
        private void Start()
        {
            // Start game initialization sequence
            StartCoroutine(InitializeGameSystems());
        }
        
        private void Update()
        {
            // Handle input
            HandleGlobalInput();
            
            // Debug display
            if (enableDebugMode && showPerformanceStats)
            {
                DebugUtils.DrawFPS();
            }
        }
        
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && pauseOnFocusLoss && IsPlaying)
            {
                PauseGame();
            }
        }
        
        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && pauseOnFocusLoss && IsPlaying)
            {
                PauseGame();
            }
        }
        
        #endregion
        
        #region Initialization
        
        private void InitializeCoreystems()
        {
            // Initialize Service Locator
            ServiceLocator.Initialize();
            
            // Set debug mode
            DebugUtils.SetDebugEnabled(enableDebugMode);
            
            // Initialize event bus (it's a singleton so just ensure it exists)
            var eventBus = EventBus.Instance;
            
            DebugUtils.Log("[GameManager] Core systems initialized");
        }
        
        private System.Collections.IEnumerator InitializeGameSystems()
        {
            ChangeState(GameState.Loading);
            
            // Wait a frame
            yield return null;
            
            // Initialize managers in order
            yield return StartCoroutine(InitializeManagers());
            
            // Load initial data
            yield return StartCoroutine(LoadInitialData());
            
            // Verify systems
            if (VerifySystemsReady())
            {
                ChangeState(GameState.MainMenu);
                DebugUtils.Log("[GameManager] Game systems initialized successfully");
            }
            else
            {
                ChangeState(GameState.Error);
                DebugUtils.LogError("[GameManager] Failed to initialize game systems");
            }
        }
        
        private System.Collections.IEnumerator InitializeManagers()
        {
            DebugUtils.Log("[GameManager] Initializing managers...");
            
            // Initialize managers that extend our architecture
            // These would be implemented in GameSystems layer
            
            yield return null; // Placeholder - actual managers will be added later
            
            DebugUtils.Log("[GameManager] Managers initialized");
        }
        
        private System.Collections.IEnumerator LoadInitialData()
        {
            DebugUtils.Log("[GameManager] Loading initial data...");
            
            // Load game data, settings, etc.
            // This would integrate with Data layer
            
            yield return null; // Placeholder
            
            DebugUtils.Log("[GameManager] Initial data loaded");
        }
        
        private bool VerifySystemsReady()
        {
            // Check if all critical systems are ready
            bool eventBusReady = EventBus.Instance != null;
            bool serviceLocatorReady = ServiceLocator.IsRegistered<GameManager>();
            
            return eventBusReady && serviceLocatorReady;
        }
        
        #endregion
        
        #region Game State Management
        
        public void ChangeState(GameState newState)
        {
            if (currentState == newState) return;
            
            var oldState = currentState;
            currentState = newState;
            
            DebugUtils.Log($"[GameManager] State changed: {oldState} -> {newState}");
            
            // Handle state transitions
            OnStateExit(oldState);
            OnStateEnter(newState);
            
            // Notify listeners
            OnGameStateChanged?.Invoke(newState);
            EventBus.Dispatch(new GameStateChangedEvent(oldState, newState));
        }
        
        private void OnStateExit(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    // Cleanup playing state
                    break;
                case GameState.Paused:
                    // Resume systems if needed
                    break;
            }
        }
        
        private void OnStateEnter(GameState state)
        {
            switch (state)
            {
                case GameState.Loading:
                    // Show loading UI
                    Time.timeScale = 1f;
                    break;
                case GameState.MainMenu:
                    // Show main menu
                    Time.timeScale = 1f;
                    break;
                case GameState.Playing:
                    // Start gameplay
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    // Pause game
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                    // Handle game over
                    Time.timeScale = 0f;
                    break;
                case GameState.Error:
                    // Handle error state
                    Time.timeScale = 0f;
                    break;
            }
        }
        
        #endregion
        
        #region Game Control
        
        public void StartGame()
        {
            if (currentState == GameState.MainMenu || currentState == GameState.GameOver)
            {
                ChangeState(GameState.Playing);
                DebugUtils.Log("[GameManager] Game started");
            }
        }
        
        public void PauseGame()
        {
            if (currentState == GameState.Playing)
            {
                ChangeState(GameState.Paused);
                OnGamePaused?.Invoke();
                EventBus.Dispatch(new GamePausedEvent());
                DebugUtils.Log("[GameManager] Game paused");
            }
        }
        
        public void ResumeGame()
        {
            if (currentState == GameState.Paused)
            {
                ChangeState(GameState.Playing);
                OnGameResumed?.Invoke();
                EventBus.Dispatch(new GameResumedEvent());
                DebugUtils.Log("[GameManager] Game resumed");
            }
        }
        
        public void EndGame()
        {
            if (currentState == GameState.Playing)
            {
                ChangeState(GameState.GameOver);
                EventBus.Dispatch(new GameOverEvent());
                DebugUtils.Log("[GameManager] Game ended");
            }
        }
        
        public void RestartGame()
        {
            ChangeState(GameState.Loading);
            StartCoroutine(RestartGameCoroutine());
        }
        
        private System.Collections.IEnumerator RestartGameCoroutine()
        {
            // Cleanup current game state
            EventBus.Dispatch(new GameRestartingEvent());
            
            yield return new WaitForSecondsRealtime(0.5f);
            
            // Restart game
            StartGame();
        }
        
        public void QuitGame()
        {
            DebugUtils.Log("[GameManager] Quitting game");
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }
        
        #endregion
        
        #region Input Handling
        
        private void HandleGlobalInput()
        {
            // ESC key handling
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                HandleEscapeKey();
            }
            
            // Debug keys
            if (enableDebugMode)
            {
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    showPerformanceStats = !showPerformanceStats;
                }
                
                if (Input.GetKeyDown(KeyCode.F2))
                {
                    ServiceLocator.LogRegisteredServices();
                }
            }
        }
        
        private void HandleEscapeKey()
        {
            switch (currentState)
            {
                case GameState.Playing:
                    PauseGame();
                    break;
                case GameState.Paused:
                    ResumeGame();
                    break;
                case GameState.MainMenu:
                    QuitGame();
                    break;
            }
        }
        
        #endregion
        
        #region Debug
        
        [ContextMenu("Log System Status")]
        private void LogSystemStatus()
        {
            DebugUtils.Log($"[GameManager] Current State: {currentState}");
            DebugUtils.Log($"[GameManager] Time Scale: {Time.timeScale}");
            DebugUtils.Log($"[GameManager] Debug Mode: {enableDebugMode}");
            
            ServiceLocator.LogRegisteredServices();
        }
        
        public void SetDebugMode(bool enabled)
        {
            enableDebugMode = enabled;
            DebugUtils.SetDebugEnabled(enabled);
        }
        
        #endregion
    }
    
    /// <summary>
    /// Game states
    /// </summary>
    public enum GameState
    {
        Loading,
        MainMenu,
        Playing,
        Paused,
        GameOver,
        Error
    }
    
    #region Game State Events
    
    public class GameStateChangedEvent : GameEvent
    {
        public GameState PreviousState { get; }
        public GameState NewState { get; }
        
        public GameStateChangedEvent(GameState previousState, GameState newState) : base("GameManager")
        {
            PreviousState = previousState;
            NewState = newState;
        }
    }
    
    public class GamePausedEvent : GameEvent
    {
        public GamePausedEvent() : base("GameManager") { }
    }
    
    public class GameResumedEvent : GameEvent
    {
        public GameResumedEvent() : base("GameManager") { }
    }
    
    public class GameOverEvent : GameEvent
    {
        public GameOverEvent() : base("GameManager") { }
    }
    
    public class GameRestartingEvent : GameEvent
    {
        public GameRestartingEvent() : base("GameManager") { }
    }
    
    #endregion
}