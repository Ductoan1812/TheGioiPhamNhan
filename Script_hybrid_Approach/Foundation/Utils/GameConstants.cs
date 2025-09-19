namespace Foundation.Utils
{
    /// <summary>
    /// Game constants - tập trung tất cả magic numbers từ code hiện tại.
    /// Thay vì hard-code values trong từng file, ta centralize ở đây.
    /// </summary>
    public static class GameConstants
    {
        #region Player Constants
        
        public static class Player
        {
            public const float DEFAULT_MOVE_SPEED = 6f;
            public const float DEFAULT_ATTACK_RADIUS = 1.2f;
            public const float DEFAULT_ATTACK_COOLDOWN = 0.5f;
            public const float FACING_DOT_THRESHOLD = 0f;  // 180° range
            
            // Animation blending
            public const float ANIMATION_BLEND_DEFAULT = 0.5f;
            
            // Scale flipping
            public const float SCALE_NORMAL = 1f;
            public const float SCALE_FLIPPED = -1f;
        }
        
        #endregion
        
        #region Enemy Constants
        
        public static class Enemy
        {
            // Movement speeds
            public const float DEFAULT_WALK_SPEED = 1.2f;
            public const float DEFAULT_RUN_SPEED = 3f;
            
            // AI behavior
            public const float DEFAULT_PATROL_RADIUS = 5f;
            public const float DEFAULT_CHASE_RADIUS = 3.5f;
            public const float DEFAULT_KEEP_DISTANCE = 0.6f;
            
            // Direction change timing
            public const float DIR_CHANGE_MIN = 1f;
            public const float DIR_CHANGE_MAX = 3f;
            
            // Movement phases
            public const float MOVE_PHASE_MIN = 2f;
            public const float MOVE_PHASE_MAX = 4f;
            public const float IDLE_PHASE_MIN = 2f;
            public const float IDLE_PHASE_MAX = 4f;
            
            // Boundary return
            public const float BOUNDARY_RETURN_FACTOR = 0.85f;
            
            // Attack
            public const float DEFAULT_ATTACK_RADIUS = 1.2f;
            public const float DEFAULT_ATTACK_COOLDOWN = 2f;
            
            // Health regeneration
            public const float HEAL_DELAY = 3f;
            public const float HEAL_RATE = 1f; // per second
        }
        
        #endregion
        
        #region Combat Constants
        
        public static class Combat
        {
            // Damage calculation
            public const float MIN_DAMAGE = 1f;
            public const float CRIT_BASE_MULTIPLIER = 2f;
            
            // Status effects
            public const float INVULNERABILITY_DURATION = 0.1f;
            
            // Visual feedback
            public const float DAMAGE_TEXT_DURATION = 1f;
            public const float DAMAGE_TEXT_RANDOM_X = 0.3f;
            public const float DAMAGE_TEXT_RANDOM_Y = 0.2f;
        }
        
        #endregion
        
        #region UI Constants
        
        public static class UI
        {
            // Animation durations
            public const float PANEL_FADE_DURATION = 0.3f;
            public const float TOOLTIP_DELAY = 0.5f;
            
            // Layout
            public const float SLOT_SIZE = 64f;
            public const float SLOT_SPACING = 4f;
            
            // Health bar
            public const float HEALTH_BAR_HEIGHT = 8f;
            public const float HEALTH_BAR_WIDTH = 64f;
            public const float HEALTH_BAR_OFFSET_Y = 80f;
        }
        
        #endregion
        
        #region Item Constants
        
        public static class Items
        {
            // Drop behavior
            public const float DROP_FORCE_MIN = 2f;
            public const float DROP_FORCE_MAX = 5f;
            public const float DROP_UP_FORCE = 0.5f;
            
            // Pickup
            public const float PICKUP_RADIUS = 1f;
            public const float PICKUP_MAGNETIC_RADIUS = 3f;
            
            // Stack limits
            public const int DEFAULT_MAX_STACK = 99;
            public const int EQUIPMENT_MAX_STACK = 1;
        }
        
        #endregion
        
        #region System Constants
        
        public static class System
        {
            // Performance
            public const int OBJECT_POOL_INITIAL_SIZE = 10;
            public const int OBJECT_POOL_MAX_SIZE = 100;
            
            // Save/Load
            public const string PLAYER_DATA_FILE = "player_data.json";
            public const string SETTINGS_FILE = "game_settings.json";
            
            // Layers (using string names)
            public const string PLAYER_LAYER = "Player";
            public const string ENEMY_LAYER = "Enemy";
            public const string GROUND_LAYER = "Ground";
            public const string ITEM_LAYER = "Item";
            
            // Tags
            public const string PLAYER_TAG = "Player";
            public const string ENEMY_TAG = "Enemy";
            public const string ITEM_TAG = "Item";
        }
        
        #endregion
        
        #region Math Constants
        
        public static class Math
        {
            public const float EPSILON = 0.001f;
            public const float DEG_TO_RAD = UnityEngine.Mathf.PI / 180f;
            public const float RAD_TO_DEG = 180f / UnityEngine.Mathf.PI;
            
            // Common angles
            public const float ANGLE_90 = 90f;
            public const float ANGLE_180 = 180f;
            public const float ANGLE_270 = 270f;
            public const float ANGLE_360 = 360f;
        }
        
        #endregion
        
        #region Audio Constants (Future)
        
        public static class Audio
        {
            public const float DEFAULT_SFX_VOLUME = 0.8f;
            public const float DEFAULT_MUSIC_VOLUME = 0.6f;
            public const float FADE_DURATION = 1f;
            
            // 3D Audio
            public const float MAX_AUDIO_DISTANCE = 20f;
            public const float MIN_AUDIO_DISTANCE = 1f;
        }
        
        #endregion
        
        #region Colors (for debugging/UI)
        
        public static class Colors
        {
            // Debug colors
            public static readonly UnityEngine.Color DEBUG_GIZMO = new UnityEngine.Color(0f, 1f, 0.6f, 0.35f);
            public static readonly UnityEngine.Color DEBUG_WIRE = new UnityEngine.Color(1f, 1f, 0f, 0.8f);
            
            // Rarity colors
            public static readonly UnityEngine.Color RARITY_COMMON = UnityEngine.Color.white;
            public static readonly UnityEngine.Color RARITY_UNCOMMON = UnityEngine.Color.green;
            public static readonly UnityEngine.Color RARITY_RARE = UnityEngine.Color.blue;
            public static readonly UnityEngine.Color RARITY_EPIC = UnityEngine.Color.magenta;
            public static readonly UnityEngine.Color RARITY_LEGENDARY = new UnityEngine.Color(1f, 0.5f, 0f); // Orange
            public static readonly UnityEngine.Color RARITY_ARTIFACT = new UnityEngine.Color(1f, 0.84f, 0f); // Gold
            
            // UI colors
            public static readonly UnityEngine.Color UI_POSITIVE = UnityEngine.Color.green;
            public static readonly UnityEngine.Color UI_NEGATIVE = UnityEngine.Color.red;
            public static readonly UnityEngine.Color UI_NEUTRAL = UnityEngine.Color.white;
            public static readonly UnityEngine.Color UI_WARNING = UnityEngine.Color.yellow;
        }
        
        #endregion
    }
}