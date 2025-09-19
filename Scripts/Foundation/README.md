# Foundation Layer

## 📋 Mục đích
Foundation layer chứa các core utilities và base classes có thể tái sử dụng, không phụ thuộc vào game cụ thể. Đây là layer thấp nhất trong kiến trúc, cung cấp các building blocks cho các layer khác.

## 🏗️ Cấu trúc thư mục

### 📁 Events/
**Mục đích**: Hệ thống event-driven architecture
- Base classes cho tất cả game events
- Event bus và messaging system
- Strongly-typed events thay thế Unity Actions

**Thành phần chính**:
- `GameEvent.cs` - Base event classes
- `EventBus.cs` - Central event dispatcher
- Interfaces cho event handling

### 📁 Serialization/
**Mục đích**: Xử lý serialize/deserialize data
- JSON serialization utilities
- XML processing (nếu cần)
- Custom serialization cho Unity objects

**Thành phần dự kiến**:
- `JsonSerializer.cs` - JSON handling
- `SerializationUtilities.cs` - Helper methods
- Custom converters cho Unity types

### 📁 Utils/
**Mục đích**: Tiện ích chung không phụ thuộc engine
- Extension methods
- Helper functions
- Mathematical utilities
- String processing

**Thành phần dự kiến**:
- `Extensions.cs` - Extension methods
- `MathUtils.cs` - Mathematical helpers
- `StringUtils.cs` - String processing
- `CollectionUtils.cs` - Collection utilities

### 📁 Architecture/
**Mục đích**: Design patterns và architectural components
- Dependency Injection
- Object Pooling
- Observer pattern implementations
- Factory patterns

**Thành phần dự kiến**:
- `ObjectPool.cs` - Generic object pooling
- `ServiceLocator.cs` - Service location pattern
- `Observer.cs` - Observer pattern implementation
- `Factory.cs` - Factory pattern utilities

## ⚡ Đặc điểm chính

### ✅ Engine-Agnostic
- Không phụ thuộc vào Unity MonoBehaviour
- Có thể sử dụng trong unit tests
- Portable sang engine khác

### ✅ Reusable
- Generic implementations
- Interface-based design
- Configurable components

### ✅ Performance-Focused
- Minimal allocations
- Efficient algorithms
- Memory-conscious design

## 📐 Quy tắc sử dụng

### ✅ Nên làm:
- Tạo generic, reusable components
- Sử dụng interfaces thay vì concrete classes
- Viết unit tests cho tất cả utilities
- Document API rõ ràng

### ❌ Không nên:
- Reference đến game-specific logic
- Sử dụng Unity-specific APIs (trừ khi cần thiết)
- Tạo dependencies lên layers khác
- Hard-code game values

## 🔗 Dependencies
- **Depends on**: .NET Standard libraries
- **Used by**: GameSystems, Entities, Features, Presentation, Infrastructure

## 📝 Ví dụ sử dụng

```csharp
// Event System
public class PlayerDeathEvent : GameEvent<PlayerData>
{
    public PlayerDeathEvent(PlayerData playerData) : base(playerData) { }
}

// Object Pool
var bulletPool = new ObjectPool<Bullet>(() => new Bullet(), 100);
var bullet = bulletPool.Get();

// Utilities
var processedText = inputText.RemoveSpecialCharacters().TruncateToLength(50);
```

## 🧪 Testing
Foundation components phải có unit tests coverage >= 90% vì chúng là nền tảng cho toàn bộ hệ thống.

