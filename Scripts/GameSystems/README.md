# GameSystems Layer

## 📋 Mục đích
GameSystems layer chứa core mechanics và domain logic của game. Đây là "business logic" của game, hoàn toàn độc lập với Unity và có thể được test một cách riêng biệt.

## 🏗️ Cấu trúc thư mục

### 📁 Stats/
**Mục đích**: Hệ thống thống kê nhân vật và entities
- Base stat system (Health, Mana, Strength, etc.)
- Stat modifiers và buffs/debuffs
- Progression và level scaling

**Thành phần chính**:
- `Stat.cs` - Base stat class
- `StatModifier.cs` - Temporary/permanent modifiers
- `StatCollection.cs` - Container cho stats
- `StatCalculator.cs` - Calculation logic

### 📁 Inventory/
**Mục đích**: Hệ thống quản lý vật phẩm
- Item container logic
- Stack management
- Item filtering và sorting
- Drag & drop logic (non-UI)

**Thành phần chính**:
- `Inventory.cs` - Main inventory container
- `Item.cs` - Base item class
- `ItemStack.cs` - Stackable item logic
- `ItemFilter.cs` - Filtering utilities

### 📁 Combat/
**Mục đích**: Hệ thống chiến đấu
- Damage calculation
- Combat state management
- Turn-based hoặc real-time combat logic
- Skill và ability system

**Thành phần chính**:
- `CombatSystem.cs` - Main combat controller
- `DamageCalculator.cs` - Damage computation
- `CombatState.cs` - Combat state machine
- `Ability.cs` - Base ability class

### 📁 Progression/
**Mục đích**: Hệ thống tiến triển nhân vật
- Experience và leveling
- Skill trees
- Achievement tracking
- Character development

**Thành phần chính**:
- `ExperienceSystem.cs` - XP management
- `LevelingSystem.cs` - Level progression
- `SkillTree.cs` - Skill progression
- `Achievement.cs` - Achievement logic

## ⚡ Đặc điểm chính

### ✅ Pure Logic
- Không có MonoBehaviour dependencies
- Hoàn toàn testable
- Platform-independent

### ✅ Domain-Driven Design
- Rich domain models
- Business rules encapsulation
- Clear bounded contexts

### ✅ Event-Driven
- Sử dụng Foundation Events
- Loose coupling between systems
- Reactive programming patterns

## 📐 Quy tắc thiết kế

### ✅ Nên làm:
- Implement business rules trong domain objects
- Sử dụng value objects cho immutable data
- Raise events cho state changes
- Viết comprehensive unit tests

### ❌ Không nên:
- Reference Unity APIs trực tiếp
- Chứa UI logic
- Hard-code configuration values
- Tạo circular dependencies

## 🔗 Dependencies
- **Depends on**: Foundation (Events, Utils, Architecture)
- **Used by**: Entities, Features
- **Independent from**: Unity engine, UI, Audio, VFX

## 📊 Data Flow

```
Entities → GameSystems → Foundation
     ↓         ↓           ↓
Presentation ←← Events ←←← Events
```

## 📝 Ví dụ sử dụng

```csharp
// Stats System
var playerStats = new StatCollection();
playerStats.AddStat(StatType.Health, 100);
var modifier = new StatModifier(StatType.Health, 50, ModifierType.Flat);
playerStats.ApplyModifier(modifier);

// Inventory System
var inventory = new Inventory(capacity: 30);
var sword = new Weapon("Iron Sword", damage: 25);
inventory.AddItem(sword, quantity: 1);

// Combat System
var combatResult = CombatSystem.ResolveDamage(attacker, defender, ability);
EventBus.Publish(new DamageDealtEvent(combatResult));

// Progression System
experienceSystem.AddExperience(100);
if (experienceSystem.CanLevelUp()) {
    var newLevel = levelingSystem.LevelUp();
    EventBus.Publish(new PlayerLevelUpEvent(newLevel));
}
```

## 🧪 Testing Strategy
- **Unit Tests**: Tất cả business logic
- **Integration Tests**: Cross-system interactions
- **Property-Based Tests**: Stat calculations, inventory operations
- **Performance Tests**: Large-scale operations

## 🚀 Performance Considerations
- Object pooling cho frequent operations
- Lazy loading cho complex calculations
- Caching cho expensive computations
- Event batching để giảm overhead

