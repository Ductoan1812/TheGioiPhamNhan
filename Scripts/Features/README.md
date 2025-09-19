# Features Layer

## 📋 Mục đích
Features layer chứa các optional gameplay modules có thể được bật/tắt độc lập. Đây là các tính năng mở rộng không thuộc core mechanics, giúp game có thể scale và customize dễ dàng.

## 🏗️ Cấu trúc thư mục

### 📁 QuestSystem/
**Mục đích**: Hệ thống nhiệm vụ và objective tracking
- Quest definition và management
- Objective tracking
- Reward distribution
- Quest chains và dependencies

**Thành phần chính**:
- `Quest.cs` - Base quest class
- `QuestManager.cs` - Quest lifecycle management
- `QuestObjective.cs` - Individual objectives
- `QuestReward.cs` - Reward handling
- `QuestDatabase.cs` - Quest storage

### 📁 ShopSystem/
**Mục đích**: Commerce và trading mechanics
- Item buying/selling
- Price calculation
- Merchant interactions
- Currency management

**Thành phần chính**:
- `Shop.cs` - Main shop logic
- `ShopItem.cs` - Shop item representation
- `Merchant.cs` - NPC merchant behavior
- `PriceCalculator.cs` - Dynamic pricing
- `Currency.cs` - Currency handling

### 📁 CraftingSystem/
**Mục đích**: Item creation và modification
- Recipe management
- Resource combination
- Crafting stations
- Item upgrading

**Thành phần chính**:
- `CraftingRecipe.cs` - Recipe definitions
- `CraftingStation.cs` - Crafting logic
- `CraftingManager.cs` - System coordination
- `ResourceRequirement.cs` - Material needs
- `CraftingResult.cs` - Output handling

### 📁 SaveSystem/
**Mục đích**: Game state persistence
- Save/load game data
- Checkpoint management
- Cross-session persistence
- Data versioning

**Thành phần chính**:
- `SaveManager.cs` - Main save coordination
- `SaveData.cs` - Serializable game state
- `SaveSlot.cs` - Save slot management
- `AutoSave.cs` - Automatic saving
- `DataMigration.cs` - Version compatibility

## ⚡ Đặc điểm chính

### ✅ Modular Design
- Có thể enable/disable từng feature
- Minimal dependencies between features
- Runtime configuration

### ✅ Plugin Architecture
- Feature discovery system
- Dependency injection support
- Event-driven integration

### ✅ Configuration-Driven
- ScriptableObject configurations
- Runtime parameter adjustment
- A/B testing support

## 📐 Quy tắc thiết kế

### ✅ Nên làm:
- Design features as independent modules
- Provide clear feature toggles
- Use dependency injection
- Document feature dependencies

### ❌ Không nên:
- Create tight coupling between features
- Assume other features are always available
- Modify core systems directly
- Ignore feature disabled states

## 🔗 Dependencies
- **Depends on**: GameSystems, Foundation
- **Optional dependencies**: Other Features (clearly documented)
- **Used by**: Entities, Presentation

## 🔧 Feature Lifecycle

```
1. Feature Discovery → 2. Configuration → 3. Initialization → 4. Runtime
                                                              ↓
                     5. Cleanup ← 4. Deactivation ← 4. Active State
```

## 📊 Integration Pattern

```
Features
   ↓
GameSystems (core logic)
   ↓
Foundation (events & utilities)
```

## 📝 Ví dụ sử dụng

```csharp
// Quest System
public class QuestManager : MonoBehaviour, IFeature
{
    public bool IsEnabled { get; set; } = true;
    
    public void Initialize()
    {
        if (!IsEnabled) return;
        
        EventBus.Subscribe<PlayerActionEvent>(OnPlayerAction);
        LoadActiveQuests();
    }
    
    private void OnPlayerAction(PlayerActionEvent evt)
    {
        foreach (var quest in activeQuests)
        {
            quest.CheckObjectives(evt);
        }
    }
}

// Shop System
public class Shop : MonoBehaviour
{
    [SerializeField] private ShopConfiguration config;
    
    public bool TryPurchaseItem(ShopItem item, Currency playerCurrency)
    {
        if (!ShopSystem.IsEnabled) return false;
        
        var price = priceCalculator.CalculatePrice(item);
        if (playerCurrency.CanAfford(price))
        {
            playerCurrency.Spend(price);
            EventBus.Publish(new ItemPurchasedEvent(item));
            return true;
        }
        return false;
    }
}

// Crafting System
public class CraftingStation : MonoBehaviour
{
    public CraftingResult TryCraft(CraftingRecipe recipe, Inventory playerInventory)
    {
        if (!CraftingSystem.IsEnabled)
            return CraftingResult.SystemDisabled();
        
        if (!recipe.CanCraft(playerInventory))
            return CraftingResult.InsufficientResources();
        
        recipe.ConsumeResources(playerInventory);
        var craftedItem = recipe.CreateItem();
        
        EventBus.Publish(new ItemCraftedEvent(craftedItem));
        return CraftingResult.Success(craftedItem);
    }
}

// Save System
public class SaveManager : MonoBehaviour
{
    public void SaveGame(SaveSlot slot)
    {
        if (!SaveSystem.IsEnabled) return;
        
        var saveData = new SaveData();
        
        // Collect data from all systems
        EventBus.Publish(new CollectSaveDataEvent(saveData));
        
        // Serialize and save
        var serializedData = JsonUtility.ToJson(saveData);
        File.WriteAllText(slot.FilePath, serializedData);
        
        EventBus.Publish(new GameSavedEvent(slot));
    }
}
```

## 🎛️ Feature Configuration

```csharp
[CreateAssetMenu(fileName = "QuestSystemConfig", menuName = "Features/Quest System")]
public class QuestSystemConfiguration : ScriptableObject
{
    [Header("System Settings")]
    public bool enableQuestSystem = true;
    public int maxActiveQuests = 10;
    public bool allowQuestSharing = false;
    
    [Header("UI Settings")]
    public bool showQuestTracker = true;
    public QuestTrackerPosition trackerPosition = QuestTrackerPosition.TopRight;
}
```

## 🧩 Feature Discovery

```csharp
public class FeatureManager : MonoBehaviour
{
    private List<IFeature> discoveredFeatures = new();
    
    private void Awake()
    {
        // Auto-discover features
        discoveredFeatures = FindObjectsOfType<MonoBehaviour>()
            .OfType<IFeature>()
            .ToList();
        
        foreach (var feature in discoveredFeatures)
        {
            if (feature.IsEnabled)
            {
                feature.Initialize();
            }
        }
    }
}
```

## ⚡ Performance Considerations
- Lazy loading cho disabled features
- Feature-specific object pools
- Conditional compilation cho build optimization
- Memory cleanup khi features disabled

## 🧪 Testing Strategy
- **Feature Tests**: Individual feature functionality
- **Integration Tests**: Feature interactions với core systems
- **Configuration Tests**: Various feature combinations
- **Performance Tests**: Feature enable/disable impact

