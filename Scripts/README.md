# Game Project - Clean Architecture

## 🎯 Tổng quan dự án

Dự án game này được xây dựng theo **Clean Architecture** với **6 layers** rõ ràng, đảm bảo code dễ maintain, test và scale. Mỗi layer có trách nhiệm riêng biệt và dependencies flow theo một hướng nhất định.

## 🏗️ Kiến trúc tổng thể

```
┌─────────────────────────────────────────────────────────┐
│                    Presentation                         │
│                (UI, Audio, VFX, Animation)             │
├─────────────────────────────────────────────────────────┤
│                     Entities                            │
│           (MonoBehaviours, Unity Components)            │
├─────────────────────────────────────────────────────────┤
│                     Features                            │
│          (Optional Modules: Quest, Shop, etc.)         │
├─────────────────────────────────────────────────────────┤
│                   GameSystems                           │
│             (Business Logic: Stats, Inventory)          │
├─────────────────────────────────────────────────────────┤
│                   Foundation                            │
│              (Core Utilities, Events, Utils)           │
├─────────────────────────────────────────────────────────┤
│                 Infrastructure                          │
│           (Unity Integration, Data, Performance)       │
└─────────────────────────────────────────────────────────┘
```

## 📁 Cấu trúc thư mục

```
Scripts/
├── Foundation/              # Core utilities (engine-agnostic)
│   ├── Events/             # Event system
│   ├── Serialization/      # JSON/XML handling
│   ├── Utils/              # Extension methods, helpers
│   └── Architecture/       # Design patterns (ObjectPool, etc.)
│
├── GameSystems/            # Business logic
│   ├── Stats/              # Character stats system
│   ├── Inventory/          # Item management
│   ├── Combat/             # Combat mechanics
│   └── Progression/        # Level/XP system
│
├── Entities/               # Unity MonoBehaviours
│   ├── Player/             # Player components
│   ├── Enemies/            # AI entities
│   ├── Items/              # World items
│   └── Environment/        # Interactive objects
│
├── Features/               # Optional modules
│   ├── QuestSystem/        # Quest management
│   ├── ShopSystem/         # Commerce
│   ├── CraftingSystem/     # Item crafting
│   └── SaveSystem/         # Game persistence
│
├── Presentation/           # Feedback layers
│   ├── UI/                 # User interface
│   ├── Audio/              # Sound management
│   ├── VFX/                # Visual effects
│   └── Animation/          # Animation control
│
└── Infrastructure/         # Unity-specific
    ├── Data/               # ScriptableObjects
    ├── Performance/        # Optimization
    ├── Input/              # Input handling
    ├── Scene/              # Scene management
    └── Debug/              # Debug utilities
```

## 🚀 Bắt đầu nhanh

### 1. Setup cơ bản

1. **Tạo GameManager**: Kéo `GameManager.cs` vào scene và setup
2. **Tạo Player**: Setup PlayerController với các components
3. **Cấu hình UI**: Setup UIManager và các UI panels
4. **Audio Setup**: Cấu hình AudioManager với Audio Mixer

### 2. Tạo Player cơ bản

```csharp
// Tạo GameObject với các components:
// - PlayerController
// - PlayerMovement
// - PlayerHealth  
// - PlayerInventory
// - PlayerInput
// - CharacterController
```

### 3. Setup UI

```csharp
// Tạo Canvas với UIManager
// Setup các panels: GameHUD, Inventory, PauseMenu
// Kết nối HealthBarUI và InventoryUI
```

## 📋 Hướng dẫn sử dụng từng layer

### 🔧 Foundation Layer
- **Chứa**: Core utilities, Event system, Object pooling
- **Sử dụng**: Import namespace `Foundation.Events`, `Foundation.Utils`
- **Ví dụ**: 
```csharp
EventBus.Publish(new PlayerHealthChangedEvent(currentHealth, maxHealth));
var pool = new ObjectPool<Bullet>(() => new Bullet(), 100);
```

### 🎮 GameSystems Layer  
- **Chứa**: Business logic, Stats, Inventory
- **Sử dụng**: Import `GameSystems.Stats`, `GameSystems.Inventory`
- **Ví dụ**:
```csharp
var playerStats = new StatCollection();
playerStats.AddStat(StatType.Health, 100f);
var inventory = new Inventory(30);
```

### 🎭 Entities Layer
- **Chứa**: MonoBehaviour components
- **Sử dụng**: Attach components to GameObjects
- **Ví dụ**: PlayerController orchestrates player behavior

### 🎯 Features Layer
- **Chứa**: Optional gameplay modules
- **Sử dụng**: Enable/disable features as needed
- **Ví dụ**: QuestSystem, ShopSystem

### 🎨 Presentation Layer
- **Chứa**: UI, Audio, VFX
- **Sử dụng**: React to events from GameSystems
- **Ví dụ**: HealthBarUI updates when health changes

### ⚙️ Infrastructure Layer
- **Chứa**: Unity integration, ScriptableObjects
- **Sử dụng**: Data configuration, Performance utilities
- **Ví dụ**: ItemData ScriptableObjects

## 🔄 Event Flow

```
User Input → Entities → GameSystems → Foundation Events → Presentation
```

1. **Input**: PlayerInput detects user input
2. **Entity**: PlayerController processes input
3. **System**: Updates game state in GameSystems
4. **Event**: Publishes events via EventBus
5. **Presentation**: UI/Audio responds to events

## 📝 Ví dụ flow hoàn chỉnh

### Player takes damage:
1. `EnemyController` → deals damage
2. `PlayerHealth` → processes damage, updates stats
3. `StatCollection` → publishes `PlayerHealthChangedEvent`
4. `HealthBarUI` → updates health bar display
5. `AudioManager` → plays hurt sound effect

## 🧪 Testing Strategy

- **Foundation**: Unit tests cho utilities
- **GameSystems**: Unit tests cho business logic  
- **Entities**: Integration tests với Unity
- **Features**: Feature-specific tests
- **Presentation**: UI automation tests

## 📊 Performance Guidelines

- Sử dụng **Object Pooling** cho frequent spawning
- **Event batching** để reduce overhead
- **Component caching** trong MonoBehaviours
- **ScriptableObjects** cho configuration data

## 🔧 Configuration

### GameSettings
Tạo `GameSettings` ScriptableObject trong thư mục Resources để cấu hình:
- Player settings (speed, health, etc.)
- Audio settings (volume levels)
- Graphics settings (quality, resolution)
- Gameplay settings (difficulty, XP rates)

### ItemData
Tạo các ItemData ScriptableObjects để define items:
- ConsumableItemData (potions, food)
- EquipmentItemData (weapons, armor)  
- MaterialItemData (crafting materials)

## 🚨 Quy tắc quan trọng

### ✅ Nên làm:
- Sử dụng EventBus cho communication
- Delegate business logic to GameSystems
- Keep MonoBehaviours thin
- Use interfaces for flexibility
- Write comprehensive tests

### ❌ Không nên:
- Direct coupling between layers
- Business logic trong MonoBehaviours
- Circular dependencies
- Hardcode configuration values
- Skip event-driven patterns

## 📚 Tài liệu tham khảo

- [Foundation Layer](Foundation/README.md)
- [GameSystems Layer](GameSystems/README.md)  
- [Entities Layer](Entities/README.md)
- [Features Layer](Features/README.md)
- [Presentation Layer](Presentation/README.md)
- [Infrastructure Layer](Infrastructure/README.md)

## 🤝 Đóng góp

1. Follow Clean Architecture principles
2. Maintain layer separation
3. Write tests for new features
4. Document public APIs
5. Use consistent naming conventions

---

**Happy Coding! 🎮**
