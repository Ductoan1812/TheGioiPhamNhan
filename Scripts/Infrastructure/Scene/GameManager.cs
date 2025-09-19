using UnityEngine;
using Foundation.Events;
using Infrastructure.Data;
using Infrastructure.Input;
using Entities.Player;
using Presentation.Audio;
using Presentation.UI;

namespace Infrastructure.Scene
{
    /// <summary>
    /// Main game manager - coordinates all systems
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private GameSettings gameSettings;
        
        [Header("Managers")]
        [SerializeField] private Presentation.Audio.AudioManager audioManager;
        [SerializeField] private Infrastructure.Input.InputManager inputManager;
        [SerializeField] private Presentation.UI.UIManager uiManager;

        // Game state
        private GameState currentState = GameState.Loading;
        private bool isPaused = false;

        // Properties
        public GameState CurrentState => currentState;
        public bool IsPaused => isPaused;
        public GameSettings Settings => gameSettings ?? GameSettings.Instance;

        // Events
        public System.Action<GameState> OnGameStateChanged;
        public System.Action<bool> OnPauseStateChanged;

        // Singleton
        private static GameManager instance;
        public static GameManager Instance => instance;

        private void Awake()
        {
            // Singleton setup
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGame();
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
        }

        private void Start()
        {
            // Subscribe to events
            EventBus.Subscribe<PlayerDeathEvent>(OnPlayerDeath);
            EventBus.Subscribe<SceneLoadedEvent>(OnSceneLoaded);
            
            // Start the game
            ChangeGameState(GameState.MainMenu);
        }

        private void OnDestroy()
        {
            // Unsubscribe from events
            EventBus.Unsubscribe<PlayerDeathEvent>(OnPlayerDeath);
            EventBus.Unsubscribe<SceneLoadedEvent>(OnSceneLoaded);
        }

        private void Update()
        {
            // Handle pause input
            if (UnityEngine.Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }

            // Process queued events
            EventBus.ProcessQueuedEvents();
        }

        private void InitializeGame()
        {
            // Initialize core systems
            InitializeSettings();
            InitializeManagers();
            
            Debug.Log("Game Manager initialized");
        }

        private void InitializeSettings()
        {
            if (gameSettings == null)
            {
                gameSettings = GameSettings.Instance;
            }

            // Apply initial settings
            Application.targetFrameRate = gameSettings.Graphics.targetFrameRate;
            QualitySettings.vSyncCount = gameSettings.Graphics.useVSync ? 1 : 0;
        }

        private void InitializeManagers()
        {
            // Find managers if not assigned
            if (audioManager == null) audioManager = FindFirstObjectByType<AudioManager>();
            if (inputManager == null) inputManager = FindFirstObjectByType<InputManager>();
            if (uiManager == null) uiManager = FindFirstObjectByType<UIManager>();

            // Initialize managers
            audioManager?.Initialize();
            inputManager?.Initialize();
            uiManager?.Initialize();
        }

        /// <summary>
        /// Change game state
        /// </summary>
        public void ChangeGameState(GameState newState)
        {
            if (currentState == newState) return;

            var previousState = currentState;
            currentState = newState;

            Debug.Log($"Game state changed: {previousState} -> {newState}");

            // Handle state transitions
            OnGameStateExit(previousState);
            OnGameStateEnter(newState);

            // Notify listeners
            OnGameStateChanged?.Invoke(newState);
            EventBus.Publish(new GameStateChangedEvent(newState, previousState));
        }

        private void OnGameStateExit(GameState state)
        {
            switch (state)
            {
                case GameState.Playing:
                    // Cleanup gameplay
                    break;
                case GameState.Paused:
                    // Resume systems
                    Time.timeScale = 1f;
                    break;
            }
        }

        private void OnGameStateEnter(GameState state)
        {
            switch (state)
            {
                case GameState.Loading:
                    // Show loading screen
                    break;
                case GameState.MainMenu:
                    // Show main menu
                    break;
                case GameState.Playing:
                    // Start gameplay
                    Time.timeScale = 1f;
                    break;
                case GameState.Paused:
                    // Pause systems
                    Time.timeScale = 0f;
                    break;
                case GameState.GameOver:
                    // Show game over screen
                    break;
            }
        }

        /// <summary>
        /// Toggle pause state
        /// </summary>
        public void TogglePause()
        {
            SetPause(!isPaused);
        }

        /// <summary>
        /// Set pause state
        /// </summary>
        public void SetPause(bool pause)
        {
            if (isPaused == pause) return;

            isPaused = pause;

            if (isPaused)
            {
                ChangeGameState(GameState.Paused);
            }
            else
            {
                ChangeGameState(GameState.Playing);
            }

            OnPauseStateChanged?.Invoke(isPaused);
            EventBus.Publish(new GamePausedEvent(isPaused));
        }

        /// <summary>
        /// Start new game
        /// </summary>
        public void StartNewGame()
        {
            ChangeGameState(GameState.Loading);
            
            // Load first level
            LoadScene("Game");
        }

        /// <summary>
        /// Load scene
        /// </summary>
        public void LoadScene(string sceneName)
        {
            ChangeGameState(GameState.Loading);
            
            // Use Unity's SceneManager
            UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
        }

        /// <summary>
        /// Quit game
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("Quitting game...");
            
            #if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;
            #else
                Application.Quit();
            #endif
        }

        /// <summary>
        /// Restart current scene
        /// </summary>
        public void RestartScene()
        {
            var currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            LoadScene(currentScene.name);
        }

        private void OnPlayerDeath(PlayerDeathEvent deathEvent)
        {
            // Handle player death
            ChangeGameState(GameState.GameOver);
        }

        private void OnSceneLoaded(SceneLoadedEvent sceneEvent)
        {
            // Scene loaded, transition to playing
            ChangeGameState(GameState.Playing);
        }

        #region Debug Methods
        
        [ContextMenu("Pause Game")]
        private void DebugPauseGame()
        {
            SetPause(true);
        }

        [ContextMenu("Resume Game")]
        private void DebugResumeGame()
        {
            SetPause(false);
        }

        [ContextMenu("Restart Scene")]
        private void DebugRestartScene()
        {
            RestartScene();
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
        Settings
    }

    /// <summary>
    /// Game state events
    /// </summary>
    public class GameStateChangedEvent : GameEvent<GameStateData>
    {
        public GameState NewState => Data.NewState;
        public GameState PreviousState => Data.PreviousState;

        public GameStateChangedEvent(GameState newState, GameState previousState) 
            : base(new GameStateData(newState, previousState))
        {
        }
    }

    public class GamePausedEvent : GameEvent<bool>
    {
        public bool IsPaused => Data;

        public GamePausedEvent(bool isPaused) : base(isPaused)
        {
        }
    }

    public class SceneLoadedEvent : GameEvent<string>
    {
        public string SceneName => Data;

        public SceneLoadedEvent(string sceneName) : base(sceneName)
        {
        }
    }

    [System.Serializable]
    public class GameStateData
    {
        public GameState NewState { get; }
        public GameState PreviousState { get; }

        public GameStateData(GameState newState, GameState previousState)
        {
            NewState = newState;
            PreviousState = previousState;
        }
    }
}
