# GameSystems/Progression - Hệ Thống Progression Nâng Cao

## Tổng Quan

Hệ thống Progression được thiết kế để quản lý:

- **Cultivation Realms** (Cảnh giới tu luyện) với 19 realm từ Phàm Nhân → Chuẩn Tiên
- **Experience & Level System** với batched processing và realm-based scaling
- **Progression Milestones** với rewards tự động
- **Stats Integration** với Foundation architecture
- **Event-driven progression** cho UI và game logic

## Cấu Trúc Hệ Thống

```
GameSystems/Progression/
├── Core/
│   └── ProgressionDefinitions.cs    # Core definitions, enums, data structures
├── ExperienceManager.cs             # Experience & level management
├── ProgressionManager.cs            # Milestones & rewards system
└── README.md                        # Documentation này
```

## Core Components

### 1. ProgressionDefinitions (Core/)

**CultivationRealm Enum** với metadata:

```csharp
public enum CultivationRealm
{
    [RealmMeta("Phàm Nhân", 0, 1, 1.0f, "#FFFFFF")]
    PhamNhan = 0,
    
    [RealmMeta("Luyện Khí", 1, 10, 1.2f, "#87CEEB")]
    LuyenKhi = 1,
    // ... 19 realms total
    
    [RealmMeta("Chuẩn Tiên", 18, 100, 10.0f, "#FF1493")]
    ChuanTien = 18
}
```

**Experience Types:**
- `Combat` - từ chiến đấu (1.0x multiplier)
- `Quest` - từ nhiệm vụ (1.5x multiplier)
- `Cultivation` - từ tu luyện (0.8x multiplier)
- `Discovery` - từ khám phá (2.0x multiplier)
- `Event` - từ sự kiện (3.0x multiplier)

**Progression Data Structure:**
```csharp
public class ProgressionData
{
    public string EntityId { get; }
    public int Level { get; set; }
    public float Experience { get; set; }
    public CultivationRealm Realm { get; set; }
    public List<string> ClaimedMilestones { get; }
    public float TotalLifetimeExp { get; set; }
}
```

### 2. ExperienceManager

**Core Functions:**

```csharp
// Add experience with type-based multipliers
ExperienceManager.Instance.AddExperience("player", 100f, ExperienceType.Combat, "killed_enemy");

// Get progression info
int level = ExperienceManager.Instance.GetLevel("player");
float exp = ExperienceManager.Instance.GetExperience("player");
CultivationRealm realm = ExperienceManager.Instance.GetRealm("player");

// Get experience progress
float progress = ExperienceManager.Instance.GetExpProgress("player"); // 0-1
float required = ExperienceManager.Instance.GetExpRequiredForNextLevel("player");

// Check progression status
bool canLevelUp = ExperienceManager.Instance.CanLevelUp("player");
```

**Features:**
- ✅ **Batched Experience Processing** - Reduces performance impact
- ✅ **Auto Level Up** với level-up bonuses tự động
- ✅ **Realm Advancement** dựa trên level requirements
- ✅ **Stats Integration** - Sync với StatManager
- ✅ **Experience Scaling** theo realm multipliers
- ✅ **Event System** cho level up / realm advancement

**Experience Calculation:**
```csharp
// Base formula: baseExp * (growthRate ^ level) * realmMultiplier * typeMultiplier
// Example: Level 10 in Luyện Khí realm
float baseExp = 100f;
float growthRate = 1.2f;
float realmMultiplier = 1.2f; // Luyện Khí
float typeMultiplier = 1.5f; // Quest experience

float requiredExp = baseExp * Math.Pow(growthRate, 10) * realmMultiplier;
// = 100 * 6.19 * 1.2 = 742.8 exp required for level 11
```

### 3. ProgressionManager

**Milestone System:**

```csharp
// Check available milestones
var milestones = ProgressionManager.Instance.GetAvailableMilestones("player");
var unclaimed = ProgressionManager.Instance.GetUnclaimedMilestones("player");

// Claim milestone reward
bool claimed = ProgressionManager.Instance.ClaimMilestone("player", "level_50");

// Get progression summary
string summary = ProgressionManager.Instance.GetProgressionSummary("player");
```

**Reward Types:**
- `StatBonus` - Permanent stat increases
- `StatPoints` - Allocatable stat points
- `Item` - Item rewards (inventory integration)
- `Currency` - Money/resources
- `Skill` - Skill unlocks
- `Feature` - Feature unlocks
- `Title` - Title unlocks
- `Achievement` - Achievement unlocks

**Auto-Generated Milestones:**
```csharp
// Level milestones (every 5 levels)
"level_5", "level_10", "level_15", ..., "level_100"

// Realm milestones (每个 realm)
"realm_LuyenKhi", "realm_TrucCo", "realm_KimDan", ...
```

## Stats System Integration

### Automatic Stat Updates

**Level Up Bonuses (per level):**
```csharp
var levelBonuses = new[]
{
    (StatId.KhiHuyetMax, 20f), // +20 HP per level
    (StatId.LinhLucMax, 10f),  // +10 MP per level  
    (StatId.ThoNguyenMax, 5f), // +5 Stamina per level
    (StatId.CongVatLy, 2f),    // +2 Attack per level
    (StatId.PhongVatLy, 1f),   // +1 Defense per level
    (StatId.Points, 5f),       // +5 allocatable points per level
};
```

**Realm Advancement Bonuses:**
```csharp
var realmBonuses = new[]
{
    (StatId.KhiHuyetMax, 100f), // +100 HP per realm
    (StatId.LinhLucMax, 50f),   // +50 MP per realm
    (StatId.CongVatLy, 10f),    // +10 Attack per realm
    (StatId.PhongVatLy, 10f),   // +10 Defense per realm
    (StatId.TocDo, 0.1f),       // +0.1 Speed per realm
};
```

**Cultivation Stats Sync:**
```csharp
// Auto-updated by ExperienceManager
StatId.TuVi      -> Current Level
StatId.DaoHanh   -> Current Experience
StatId.TuViCan   -> Experience Required for Next Level
```

## Event System

**Experience Events:**
```csharp
// Subscribe to experience events
EventBus.Subscribe<ExperienceGainedEvent>(OnExpGained);
EventBus.Subscribe<LevelUpEvent>(OnLevelUp);
EventBus.Subscribe<RealmAdvancedEvent>(OnRealmAdvanced);

private void OnExpGained(ExperienceGainedEvent evt)
{
    Debug.Log($"{evt.EntityId} gained {evt.Amount} exp from {evt.ExpType}");
}

private void OnLevelUp(LevelUpEvent evt)
{
    Debug.Log($"{evt.EntityId} leveled up: {evt.OldLevel} -> {evt.NewLevel}");
    // Update UI, play effects, etc.
}

private void OnRealmAdvanced(RealmAdvancedEvent evt)
{
    string oldRealm = RealmMetaHelper.GetDisplayName(evt.OldRealm);
    string newRealm = RealmMetaHelper.GetDisplayName(evt.NewRealm);
    Debug.Log($"{evt.EntityId} advanced: {oldRealm} -> {newRealm}");
}
```

**Milestone Events:**
```csharp
EventBus.Subscribe<MilestoneUnlockedEvent>(OnMilestoneUnlocked);
EventBus.Subscribe<MilestoneClaimedEvent>(OnMilestoneClaimed);
EventBus.Subscribe<RewardAppliedEvent>(OnRewardApplied);
```

## Realm System Details

### Realm Metadata

```csharp
// Access realm information
string displayName = RealmMetaHelper.GetDisplayName(CultivationRealm.LuyenKhi);
Color realmColor = RealmMetaHelper.GetRealmColor(CultivationRealm.KimDan);
int minLevel = RealmMetaHelper.GetMinLevel(CultivationRealm.NguyenAnh);
float expMultiplier = RealmMetaHelper.GetExpMultiplier(CultivationRealm.HoaThan);

// Realm progression
CultivationRealm realmForLevel = RealmMetaHelper.GetRealmForLevel(25);
bool canAdvance = RealmMetaHelper.CanAdvanceRealm(currentLevel, currentRealm);
CultivationRealm? nextRealm = RealmMetaHelper.GetNextRealm(currentRealm);
```

### Realm Progression Requirements

| Realm | Min Level | Levels | Exp Multiplier | Color |
|-------|-----------|--------|----------------|-------|
| Phàm Nhân | 0 | 1 | 1.0x | White |
| Luyện Khí | 1-10 | 10 each | 1.2x | Light Blue |
| Trúc Cơ | 11 | 15 | 1.5x | Light Green |
| Kim Đan | 12 | 20 | 1.8x | Gold |
| Nguyên Anh | 13 | 25 | 2.2x | Tomato |
| Hóa Thần | 14 | 30 | 2.8x | Purple |
| Luyện Hư | 15 | 35 | 3.5x | Blue |
| Hợp Thể | 16 | 40 | 4.5x | Crimson |
| Đại Thừa | 17 | 50 | 6.0x | Dark Red |
| Chuẩn Tiên | 18 | 100 | 10.0x | Deep Pink |

## Usage Examples

### Basic Experience Gain

```csharp
// Combat experience
ExperienceManager.Instance.AddExperience("player", 50f, ExperienceType.Combat, "defeated_enemy");

// Quest completion
ExperienceManager.Instance.AddExperience("player", 200f, ExperienceType.Quest, "completed_main_quest");

// Item usage
ExperienceManager.Instance.AddExperience("player", 100f, ExperienceType.Item, "used_exp_pill");
```

### Manual Level Management

```csharp
// Force level up (admin/debug)
ExperienceManager.Instance.ForceLevelUp("player", levels: 5);

// Set experience directly
ExperienceManager.Instance.SetExperience("player", 1000f);

// Get detailed progression info
string summary = ExperienceManager.Instance.GetProgressionSummary("player");
Debug.Log(summary);
```

### Milestone Management

```csharp
// Create custom milestone
var milestone = new ProgressionMilestone(
    "custom_achievement",
    "Master Cultivator",
    50, // Required level
    CultivationRealm.KimDan // Required realm
);

// Add rewards
milestone.Rewards = new[]
{
    new ProgressionReward(ProgressionRewardType.StatBonus, "CongVatLy", 50f, "Master's Strength"),
    new ProgressionReward(ProgressionRewardType.Item, "legendary_sword", 1f, "Legendary Weapon"),
    new ProgressionReward(ProgressionRewardType.StatPoints, "", 20f, "Bonus Points")
};

// Check and claim milestones
ProgressionManager.Instance.CheckMilestones("player");
```

## Performance Optimization

### Batched Processing

**Experience Gains:**
- Experience gains are batched với configurable delay (default 0.1s)
- Reduces update frequency cho multiple rapid exp gains
- Prevents UI spam during combat

**Reward Processing:**
- Rewards queued và processed sequentially
- Prevents blocking during multiple milestone claims
- Configurable processing delay (default 0.5s)

### Caching & Efficiency

```csharp
// Cached realm metadata for fast lookups
private static readonly Dictionary<CultivationRealm, RealmMetaAttribute> _metaCache;

// Efficient milestone checking
private Dictionary<string, HashSet<string>> claimedMilestones;

// Progression data caching
private Dictionary<string, ProgressionData> entityProgression;
```

## Migration từ Hệ Thống Cũ

### PlayerData Integration

```csharp
// Old PlayerData fields: level, realm (Realm enum)
// New: Enhanced with ProgressionData

// Migration example
var oldPlayerData = PlayerManager.Instance.Data;
var progressionData = new ProgressionData("player")
{
    Level = oldPlayerData.level,
    Realm = ConvertRealm(oldPlayerData.realm), // Convert old Realm to CultivationRealm
    Experience = oldPlayerData.stats.GetFinal(StatId.DaoHanh),
};

ExperienceManager.Instance.LoadProgressionData("player", progressionData);
```

### Stats System Migration

```csharp
// Old: Manual level/exp management in PlayerStatsManager
// New: Auto-managed through ExperienceManager

// Old approach
playerData.level++;
playerData.stats.SetBase(StatId.TuVi, playerData.level);

// New approach
ExperienceManager.Instance.AddExperience("player", expAmount, ExperienceType.Combat);
// -> Auto level up, realm advancement, stat bonuses applied
```

## Configuration

### ExperienceConfig Settings

```csharp
[Header("Base Experience Settings")]
public float baseExpPerLevel = 100f;        // Base exp for level 2
public float expGrowthRate = 1.2f;          // Exponential growth rate
public float maxExpMultiplier = 10f;        // Cap on exp scaling

[Header("Experience Type Multipliers")]
public float combatMultiplier = 1.0f;       // Combat exp multiplier
public float questMultiplier = 1.5f;        // Quest exp multiplier
public float cultivationMultiplier = 0.8f;  // Meditation exp multiplier
// ... other multipliers
```

### Manager Settings

```csharp
[Header("Experience Configuration")]
public ExperienceConfig experienceConfig;
public bool autoLevelUp = true;             // Auto-process level ups
public bool debugMode = false;              // Debug logging

[Header("Integration Settings")]
public bool syncWithStatsSystem = true;     // Auto-sync with StatManager
public bool autoRealmAdvancement = true;    // Auto-advance realms
public float expGainDelay = 0.1f;          // Batch delay for exp gains
```

## Debug & Testing

### Debug Commands

```csharp
// Experience testing
ExperienceManager.Instance.AddExperience("player", 10000f, ExperienceType.Event, "debug_test");
ExperienceManager.Instance.ForceLevelUp("player", 10);
ExperienceManager.Instance.SetExperience("player", 5000f);

// Progression summaries
string expSummary = ExperienceManager.Instance.GetProgressionSummary("player");
string mileSummary = ProgressionManager.Instance.GetProgressionSummary("player");

// Force milestone claims
ProgressionManager.Instance.ForceClaimMilestone("player", "level_50");
```

### Validation

```csharp
// Check progression data integrity
var progression = ExperienceManager.Instance.GetProgressionData("player");
Debug.Log($"Level {progression.Level}, Realm {progression.Realm}");
Debug.Log($"Exp: {progression.Experience}/{ExperienceManager.Instance.GetExpRequiredForNextLevel("player")}");

// Validate milestone system
var available = ProgressionManager.Instance.GetAvailableMilestones("player");
var unclaimed = ProgressionManager.Instance.GetUnclaimedMilestones("player");
Debug.Log($"Available: {available.Length}, Unclaimed: {unclaimed.Length}");
```

## Integration Points

### With Inventory System

```csharp
// Exp items in inventory
InventoryManager.Instance.UseItem("player", "exp_pill", 1);
// -> Triggers ExperienceManager.AddExperience via item use effect

// Milestone item rewards
var itemReward = new ProgressionReward(ProgressionRewardType.Item, "legendary_armor", 1f);
// -> Auto-added to inventory through ProgressionManager
```

### With Combat System

```csharp
// Combat exp calculation
float baseExp = enemyLevel * 10f;
float bonusExp = CalculateBonusExp(playerLevel, enemyLevel);
ExperienceManager.Instance.AddExperience("player", baseExp + bonusExp, ExperienceType.Combat, $"defeated_{enemyName}");
```

### With UI System

```csharp
// UI updates through events
EventBus.Subscribe<LevelUpEvent>(evt => {
    ShowLevelUpEffect(evt.NewLevel);
    UpdateLevelDisplay(evt.NewLevel);
});

EventBus.Subscribe<ExperienceGainedEvent>(evt => {
    ShowExpGainFloatingText(evt.Amount);
    UpdateExpBar(ExperienceManager.Instance.GetExpProgress(evt.EntityId));
});
```

## Best Practices

1. **Experience Design:**
   - Balance exp multipliers carefully
   - Consider realm scaling impact
   - Use appropriate exp types for different sources

2. **Performance:**
   - Use batched experience gains for rapid sources
   - Cache progression data for frequent queries
   - Minimize milestone checks during combat

3. **Event Handling:**
   - Subscribe/unsubscribe properly
   - Use events for UI updates, not direct calls
   - Handle edge cases in event handlers

4. **Data Management:**
   - Save progression data regularly
   - Validate data integrity on load
   - Handle corrupted data gracefully

## Kết Luận

Hệ thống Progression mới cung cấp:

- ✅ **Enhanced Cultivation System** với 19 realms đầy đủ metadata
- ✅ **Smart Experience Management** với batching và scaling
- ✅ **Automated Milestone System** với configurable rewards
- ✅ **Deep Stats Integration** với automatic bonus application
- ✅ **Performance Optimized** cho real-time gameplay
- ✅ **Event-Driven Architecture** cho responsive UI
- ✅ **Flexible Configuration** cho game balancing
- ✅ **Debug Tools** cho development và testing

Hệ thống ready để integrate với UI, Combat, và các systems khác, cung cấp progression experience rich và engaging cho game cultivation.