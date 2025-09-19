# Foundation Layer - README

## 📋 Tổng quan

Foundation Layer là tầng nền tảng cho hệ thống game 2D top-down, cung cấp các công cụ và patterns cơ bản cho toàn bộ dự án.

## 🏗️ Cấu trúc

```
Foundation/
├── Events/             # Event system
├── Data/              # Data management & serialization  
├── Utils/             # Utilities & extensions
└── Architecture/      # Core patterns (Singleton, Service Locator, etc.)
```

## 🎯 Cải tiến từ hệ thống hiện tại

### **1. Events System**
**Trước:**
```csharp
// Mỗi class có events riêng
PlayerManager.Instance.OnPlayerDataLoaded += callback;
InventoryService.Instance.OnInventoryChanged += callback;
```

**Sau:**
```csharp
// Central event bus
EventBus.Subscribe<PlayerDataLoadedEvent>(OnPlayerDataLoaded);
EventBus.Dispatch(new PlayerDataLoadedEvent(playerData));
```

**Lợi ích:**
- ✅ Decoupling components
- ✅ Type-safe events
- ✅ Event history & debugging
- ✅ Automatic cleanup

### **2. Singleton Pattern**
**Trước:**
```csharp
// Mỗi class tự implement singleton
public class PlayerManager : MonoBehaviour 
{
    public static PlayerManager Instance { get; private set; }
    private void Awake() { /* singleton logic */ }
}
```

**Sau:**
```csharp
// Base class với built-in features
public class PlayerManager : PersistentSingleton<PlayerManager>
{
    // Chỉ cần override logic business
}
```

**Lợi ích:**
- ✅ Consistent implementation
- ✅ Thread-safe
- ✅ Auto DontDestroyOnLoad
- ✅ Proper cleanup

### **3. Constants & Magic Numbers**
**Trước:**
```csharp
// Magic numbers khắp nơi
moveSpeed = 6f;
attackRadius = 1.2f;
```

**Sau:**
```csharp
// Centralized constants
moveSpeed = GameConstants.Player.DEFAULT_MOVE_SPEED;
attackRadius = GameConstants.Player.DEFAULT_ATTACK_RADIUS;
```

## 🚀 Cách sử dụng

### **Events System**

```csharp
// 1. Subscribe to events
EventBus.Subscribe<PlayerDataLoadedEvent>(OnPlayerDataLoaded);

// 2. Dispatch events
var playerData = GetPlayerData();
EventBus.Dispatch(new PlayerDataLoadedEvent(playerData));

// 3. Handle events
private void OnPlayerDataLoaded(PlayerDataLoadedEvent evt)
{
    Debug.Log($"Player {evt.Data.playerId} loaded");
}

// 4. Cleanup (automatic in OnDestroy)
EventBus.Unsubscribe<PlayerDataLoadedEvent>(OnPlayerDataLoaded);
```

### **Service Locator**

```csharp
// 1. Register services at startup
ServiceLocator.Register<IInventoryService>(inventoryService);

// 2. Register with factory (lazy loading)
ServiceLocator.RegisterFactory<IDataService>(() => new DataService());

// 3. Resolve services
var inventory = ServiceLocator.Resolve<IInventoryService>();

// 4. Optional resolution
var audio = ServiceLocator.TryResolve<IAudioService>();
```

### **Object Pooling**

```csharp
// 1. Make your class poolable
public class FloatingText : MonoBehaviour, IPoolable
{
    public void OnSpawn() { /* Reset state */ }
    public void OnDespawn() { /* Cleanup */ }
}

// 2. Create pool
var pool = PoolManager.Instance.GetOrCreatePool(floatingTextPrefab, 10);

// 3. Use pool
var text = pool.Get();
// ... use object
pool.Return(text);
```

### **Data Serialization**

```csharp
// 1. Save with backup
var playerData = GetPlayerData();
SerializationHelper.SaveToJson(playerData, "player.json", createBackup: true);

// 2. Load with fallback
if (SerializationHelper.LoadFromJson<PlayerData>("player.json", out var data))
{
    // Use loaded data
}

// 3. Versioned data
var versionedData = new VersionedData<PlayerData>(playerData, version: 2);
```

### **Extensions & Utils**

```csharp
// Transform extensions
transform.SetPositionX(10f);
transform.DestroyAllChildren();
var child = transform.FindChildRecursive("DeepChild");

// Vector extensions  
var pos = transform.position.WithY(0f);
var randomPoint = center.RandomPointInCircle(5f);

// String extensions
var coloredText = "Damage".WithColor(Color.red).Bold();
var truncated = longText.Truncate(50);

// Collection extensions
var randomItem = itemList.GetRandom();
itemList.Shuffle();
```

## 🔧 Migration từ hệ thống cũ

### **Phase 1: Setup Foundation**
1. Copy Foundation folder vào project
2. Tạo GameManager prefab trong scene
3. Initialize ServiceLocator

### **Phase 2: Migrate Events**
```csharp
// Thay vì:
PlayerManager.Instance.OnPlayerDataLoaded += callback;

// Dùng:
EventBus.Subscribe<PlayerDataLoadedEvent>(callback);
```

### **Phase 3: Migrate Singletons**
```csharp
// Thay vì implement singleton manually
public class MyManager : MonoBehaviour 
{
    public static MyManager Instance;
    // ...
}

// Kế thừa từ base class
public class MyManager : PersistentSingleton<MyManager>
{
    // Business logic only
}
```

### **Phase 4: Replace Magic Numbers**
```csharp
// Thay vì:
public float moveSpeed = 6f;

// Dùng:
public float moveSpeed = GameConstants.Player.DEFAULT_MOVE_SPEED;
```

## 🎯 Lợi ích

### **Immediate Benefits**
- ✅ **Code cleaner** - Ít boilerplate code
- ✅ **Type safety** - Compile-time error checking  
- ✅ **Consistent patterns** - Same approach everywhere
- ✅ **Better debugging** - Built-in logging & monitoring

### **Long-term Benefits**
- ✅ **Easier testing** - Mockable services
- ✅ **Better performance** - Object pooling ready
- ✅ **Maintainable** - Clear separation of concerns
- ✅ **Scalable** - Easy to add new features

### **Team Benefits**
- ✅ **Faster onboarding** - Standard patterns
- ✅ **Less bugs** - Proven implementations
- ✅ **Better collaboration** - Clear interfaces

## ⚠️ Migration Notes

1. **Không cần migrate tất cả cùng lúc** - Làm từng phần
2. **Test thoroughly** - Đảm bảo không break existing features
3. **Keep old code** - Có thể rollback nếu cần
4. **Update documentation** - Để team hiểu changes

## 🔍 Debug & Monitoring

```csharp
// Enable debug mode
GameManager.Instance.SetDebugMode(true);

// Log event history
var history = EventBus.GetEventHistory();

// Log registered services
ServiceLocator.LogRegisteredServices();

// Monitor object pools
PoolManager.Instance.LogPoolStatistics();
```

## 📚 Next Steps

Sau khi Foundation layer stable, có thể move to:
1. **GameSystems layer** - Stats, Inventory, Combat systems
2. **Entities layer** - Player, Enemy components  
3. **Features layer** - Quest, Shop, Crafting systems
4. **Presentation layer** - UI, Audio, VFX

---

**💡 Tips:**
- Bắt đầu với Events system - impact lớn nhất
- Dùng DebugUtils.Log thay vì Debug.Log
- Luôn cleanup events trong OnDestroy
- Use constants thay vì magic numbers