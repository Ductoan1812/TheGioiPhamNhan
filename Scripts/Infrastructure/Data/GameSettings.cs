using UnityEngine;

namespace Infrastructure.Data
{
    /// <summary>
    /// Global game settings ScriptableObject
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Game Data/Settings/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        [Header("Player Settings")]
        [SerializeField] private PlayerSettings playerSettings;
        
        [Header("Gameplay Settings")]
        [SerializeField] private GameplaySettings gameplaySettings;
        
        [Header("Audio Settings")]
        [SerializeField] private AudioSettings audioSettings;
        
        [Header("Graphics Settings")]
        [SerializeField] private GraphicsSettings graphicsSettings;

        // Properties
        public PlayerSettings Player => playerSettings;
        public GameplaySettings Gameplay => gameplaySettings;
        public AudioSettings Audio => audioSettings;
        public GraphicsSettings Graphics => graphicsSettings;

        // Singleton instance
        private static GameSettings instance;
        public static GameSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Resources.Load<GameSettings>("GameSettings");
                    if (instance == null)
                    {
                        Debug.LogWarning("GameSettings not found in Resources folder. Creating default settings.");
                        instance = CreateInstance<GameSettings>();
                        instance.InitializeDefaults();
                    }
                }
                return instance;
            }
        }

        private void InitializeDefaults()
        {
            playerSettings = new PlayerSettings();
            gameplaySettings = new GameplaySettings();
            audioSettings = new AudioSettings();
            graphicsSettings = new GraphicsSettings();
        }

        private void OnValidate()
        {
            if (playerSettings == null) playerSettings = new PlayerSettings();
            if (gameplaySettings == null) gameplaySettings = new GameplaySettings();
            if (audioSettings == null) audioSettings = new AudioSettings();
            if (graphicsSettings == null) graphicsSettings = new GraphicsSettings();
        }
    }

    [System.Serializable]
    public class PlayerSettings
    {
        [Header("Movement")]
        public float walkSpeed = 5f;
        public float runSpeed = 8f;
        public float jumpHeight = 2f;
        
        [Header("Health")]
        public float maxHealth = 100f;
        public float healthRegenRate = 1f;
        public float invulnerabilityTime = 1f;
        
        [Header("Inventory")]
        public int inventorySize = 30;
        public bool autoPickupItems = true;
    }

    [System.Serializable]
    public class GameplaySettings
    {
        [Header("Difficulty")]
        public float difficultyMultiplier = 1f;
        public bool enablePvP = false;
        
        [Header("Economy")]
        public float shopPriceMultiplier = 1f;
        public float sellPriceMultiplier = 0.5f;
        
        [Header("Experience")]
        public float expGainMultiplier = 1f;
        public int maxLevel = 100;
    }

    [System.Serializable]
    public class AudioSettings
    {
        [Header("Volume")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float musicVolume = 0.8f;
        [Range(0f, 1f)] public float sfxVolume = 1f;
        [Range(0f, 1f)] public float voiceVolume = 1f;
        
        [Header("Quality")]
        public bool enableSpatialAudio = true;
        public int audioQuality = 2; // 0=Low, 1=Medium, 2=High
    }

    [System.Serializable]
    public class GraphicsSettings
    {
        [Header("Quality")]
        public int qualityLevel = 2; // Unity Quality Settings index
        public bool useVSync = true;
        public int targetFrameRate = 60;
        
        [Header("Resolution")]
        public bool fullscreen = true;
        public Vector2Int resolution = new Vector2Int(1920, 1080);
        
        [Header("Effects")]
        public bool enablePostProcessing = true;
        public bool enableParticles = true;
        [Range(0.5f, 2f)] public float renderScale = 1f;
    }
}
