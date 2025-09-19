# Stats System - Migration Guide

## 📋 **Tổng quan**

Hệ thống Stats mới được thiết kế để thay thế `StatSystem.cs` hiện tại với những cải tiến đáng kể:
- **Kiến trúc modularity**: Tách biệt responsibilities rõ ràng
- **Event-driven**: Tích hợp với Foundation EventBus
- **Performance**: Cache và lazy calculation 
- **Extensibility**: Dễ dàng thêm stats và bonus types mới
- **Multi-entity**: Quản lý stats cho nhiều entities

## 🏗️ **Kiến trúc mới**

```
GameSystems/Stats/
├── Core/
│   ├── StatDefinition.cs      # Definition và metadata cho stats
│   ├── StatBonus.cs          # Bonus system với nhiều types
│   └── StatEntry.cs          # Entry cho 1 stat với bonuses
├── StatCollection.cs         # Collection quản lý all stats của 1 entity  
├── StatManager.cs           # Manager chính cho toàn bộ hệ thống
└── Demo/
    └── StatsMigrationDemo.cs # Demo migration và testing
```

## 🔄 **Migration Process**

### **Bước 1: Setup StatManager**

```csharp
// Tạo GameObject với StatManager
GameObject statManagerGO = new GameObject("StatManager");
StatManager statManager = statManagerGO.AddComponent<StatManager>();

// Hoặc access singleton
StatManager.Instance.RegisterEntity("Player_001");
```

### **Bước 2: Migrate từ PlayerStatsManager cũ**

**Trước (StatSystem.cs cũ):**
```csharp
public class PlayerStatsManager : MonoBehaviour 
{
    private StatCollection stats;
    
    void Start() 
    {
        stats = new StatCollection();
        stats.SetBaseStat(StatId.KhiHuyetMax, 1000f);
        stats.OnFinalChanged += OnStatChanged;
    }
    
    public float GetStat(StatId statId) 
    {
        return stats.GetStat(statId);
    }
}
```

**Sau (StatManager mới):**
```csharp
public class PlayerStatsManager : MonoBehaviour 
{
    private string playerId = "Player_001";
    private StatCollection playerStats;
    
    void Start() 
    {
        // Option 1: Migrate từ legacy data
        var legacyStats = GetLegacyStatData(); // Dictionary<StatId, float>
        playerStats = StatManager.Instance.RegisterEntityFromLegacy(playerId, legacyStats);
        
        // Option 2: Register mới
        playerStats = StatManager.Instance.RegisterEntity(playerId);
        
        // Subscribe events
        playerStats.OnStatChanged += OnStatChanged;
    }
    
    public float GetStat(StatId statId) 
    {
        return StatManager.Instance.GetStat(playerId, statId);
        // Hoặc: return playerStats.GetStat(statId);
    }
}
```

### **Bước 3: Integrate Bonus System**

**Equipment System:**
```csharp
public class EquipmentManager : MonoBehaviour 
{
    public void EquipItem(string playerId, ItemData item) 
    {
        var statManager = StatManager.Instance;
        
        // Add equipment bonuses
        foreach (var bonus in item.StatBonuses) 
        {
            statManager.AddBonus(playerId, bonus);
        }
        
        // Event notification tự động được dispatch
    }
    
    public void UnequipItem(string playerId, ItemData item) 
    {
        var playerStats = StatManager.Instance.GetEntityStats(playerId);
        
        // Remove all bonuses from this item
        playerStats.RemoveBonusesFromSource(item.ItemId);
    }
}
```

**Buff System:**
```csharp
public class BuffManager : MonoBehaviour 
{
    public void ApplyBuff(string entityId, BuffData buff) 
    {
        var statBonus = new StatBonus(
            statId: buff.TargetStat,
            type: buff.BonusType,
            value: buff.Value,
            sourceId: buff.BuffId,
            description: buff.Description,
            duration: buff.Duration
        );
        
        StatManager.Instance.AddBonus(entityId, statBonus);
        
        // Setup expiration
        StartCoroutine(RemoveBuffAfterDuration(entityId, statBonus, buff.Duration));
    }
}
```

## 🎯 **Sử dụng trong Combat System**

**DamageCalculator cũ:**
```csharp
public float CalculateDamage(GameObject attacker, GameObject target) 
{
    var attackerStats = attacker.GetComponent<PlayerStatsManager>();
    var targetStats = target.GetComponent<EnemyStatsManager>();
    
    float attack = attackerStats.GetStat(StatId.CongVatLy);
    float defense = targetStats.GetStat(StatId.PhongVatLy);
    
    return Mathf.Max(1f, attack - defense);
}
```

**DamageCalculator mới:**
```csharp
public float CalculateDamage(string attackerId, string targetId) 
{
    var statManager = StatManager.Instance;
    
    float attack = statManager.GetStat(attackerId, StatId.CongVatLy);
    float defense = statManager.GetStat(targetId, StatId.PhongVatLy);
    
    // Crit calculation
    float critRate = statManager.GetStat(attackerId, StatId.TiLeBaoKich);
    float critDamage = statManager.GetStat(attackerId, StatId.SatThuongBaoKich);
    
    float baseDamage = Mathf.Max(1f, attack - defense);
    
    // Apply crit if proc
    if (Random.Range(0f, 100f) < critRate) 
    {
        baseDamage *= (critDamage / 100f);
    }
    
    return baseDamage;
}

public void ApplyDamage(string targetId, float damage) 
{
    // Built-in damage application với events
    float actualDamage = StatManager.Instance.DamageEntity(targetId, damage);
    
    // Death event tự động được dispatch nếu HP <= 0
}
```

## 📊 **Event System Integration**

**Subscribe to stat changes:**
```csharp
public class UIHealthBar : MonoBehaviour 
{
    void Start() 
    {
        // Subscribe to global stat changes
        EventBus.Subscribe<StatChangedEvent>(OnStatChanged);
    }
    
    private void OnStatChanged(StatChangedEvent evt) 
    {
        if (evt.EntityId == "Player_001" && evt.StatId == StatId.KhiHuyet) 
        {
            UpdateHealthBar(evt.NewValue, StatManager.Instance.GetStat("Player_001", StatId.KhiHuyetMax));
        }
    }
}
```

**Entity death handling:**
```csharp
public class DeathManager : MonoBehaviour 
{
    void Start() 
    {
        EventBus.Subscribe<EntityDeathEvent>(OnEntityDeath);
    }
    
    private void OnEntityDeath(EntityDeathEvent evt) 
    {
        Debug.Log($"Entity {evt.EntityId} died at {evt.DeathTime}");
        
        // Handle death logic
        if (evt.EntityId.StartsWith("Enemy_")) 
        {
            HandleEnemyDeath(evt.EntityId);
        }
        else if (evt.EntityId.StartsWith("Player_")) 
        {
            HandlePlayerDeath(evt.EntityId);
        }
    }
}
```

## 🎮 **Advanced Features**

### **Resource Management**
```csharp
// Restore resources
StatManager.Instance.RestoreEntity("Player_001"); // Full restore

// Partial restore
var playerStats = StatManager.Instance.GetEntityStats("Player_001");
playerStats.RestoreResource(StatId.KhiHuyet, StatId.KhiHuyetMax, 0.5f); // 50% HP

// Get resource percentage
float hpPercent = playerStats.GetResourcePercentage(StatId.KhiHuyet, StatId.KhiHuyetMax);
```

### **Complex Bonus Stacking**
```csharp
// Multiple bonus types for same stat
var baseBonus = new StatBonus(StatId.CongVatLy, BonusType.Flat, 20f, "Weapon", "Base weapon damage");
var enchantBonus = new StatBonus(StatId.CongVatLy, BonusType.Percentage, 15f, "Enchant", "15% damage enchant");
var buffBonus = new StatBonus(StatId.CongVatLy, BonusType.Multiplicative, 0.25f, "Buff", "Strength buff");

playerStats.AddBonus(baseBonus);    // +20 flat
playerStats.AddBonus(enchantBonus); // +15% percentage  
playerStats.AddBonus(buffBonus);    // x1.25 multiplicative

// Calculation: (base + flat) * (1 + percentage) * (1 + multiplicative)
// If base = 100: (100 + 20) * (1 + 0.15) * (1 + 0.25) = 120 * 1.15 * 1.25 = 172.5
```

### **Validation & Debugging**
```csharp
// Validate stats
bool isValid = StatManager.Instance.ValidateAllStats();

// Get detailed breakdown
var playerStats = StatManager.Instance.GetEntityStats("Player_001");
var attackEntry = playerStats.GetStatEntry(StatId.CongVatLy);
Debug.Log(attackEntry.GetBonusBreakdown()); // "CongVatLy: 100 (base) + 20 (flat) × 1.15 (%) = 138"

// Log all stats
playerStats.LogAllStats();
```

## 🔧 **Customization**

### **Thêm Stat mới**
```csharp
// 1. Add to StatId enum
public enum StatId 
{
    // Existing stats...
    
    // New stats
    DamNegap,      // Damage reflection
    HoiPhuc,       // Regeneration rate
    MaPhong        // Magic resistance penetration
}

// 2. Add to GameConstants if needed
public static class GameConstants 
{
    public static class Stats 
    {
        public const float DEFAULT_DAM_NEGAP = 0f;
        public const float DEFAULT_HOI_PHUC = 1f;
    }
}

// 3. Update StatCollection initialization
private void InitializeDefaultStats()
{
    // Existing stats...
    SetBaseStat(StatId.DamNegap, GameConstants.Stats.DEFAULT_DAM_NEGAP);
    SetBaseStat(StatId.HoiPhuc, GameConstants.Stats.DEFAULT_HOI_PHUC);
}
```

### **Thêm Bonus Type mới**
```csharp
// 1. Add to BonusType enum
public enum BonusType 
{
    // Existing types...
    
    // New types
    Exponential,   // Exponential scaling
    Conditional    // Conditional bonuses
}

// 2. Update calculation in StatEntry.cs
private float CalculateFinalValue()
{
    // Existing calculations...
    
    // Step X: Apply exponential bonuses
    var expBonuses = bonuses.Where(b => b.Type == BonusType.Exponential);
    foreach (var bonus in expBonuses) 
    {
        result = Mathf.Pow(result, 1f + bonus.Value);
    }
    
    return result;
}
```

## 🚀 **Performance Tips**

1. **Cache entity references**: Lưu `StatCollection` reference thay vì lookup liên tục
2. **Batch operations**: Sử dụng `AddBonuses()` cho multiple bonuses
3. **Event optimization**: Unsubscribe events khi không cần
4. **Validation**: Chỉ gọi `ValidateAllStats()` khi debug hoặc testing

## 📝 **Best Practices**

1. **Entity naming**: Sử dụng consistent naming pattern (`Player_001`, `Enemy_Goblin_001`)
2. **Source IDs**: Sử dụng unique IDs cho bonus sources (`Item_Sword_001`, `Buff_Strength`)
3. **Error handling**: Always check `null` khi get entity stats
4. **Event cleanup**: Unsubscribe events trong `OnDestroy()`
5. **Documentation**: Document custom stats và bonus types

## 🔍 **Troubleshooting**

**Problem**: Stats không update
- **Solution**: Check nếu entity đã được registered với StatManager

**Problem**: Bonuses không apply
- **Solution**: Verify StatId matching và bonus type calculation

**Problem**: Events không fire
- **Solution**: Ensure EventBus initialization và proper subscription

**Problem**: Performance issues
- **Solution**: Check số lượng bonuses, optimize calculation frequency

## 🎯 **Next Steps**

1. ✅ **Complete Stats System** - DONE
2. 🔄 **Test Migration Demo** - Ready to test
3. 📋 **Validate with existing code** - Upcoming
4. 🚀 **Migrate Combat System** - Next phase
5. 🎮 **Migrate UI System** - Next phase

---

Sử dụng `StatsMigrationDemo.cs` để test toàn bộ system và xem các examples hoạt động!