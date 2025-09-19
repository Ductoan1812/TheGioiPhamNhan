# 🎮 Examples - Hướng dẫn sử dụng hệ thống

Thư mục này chứa các ví dụ cụ thể về cách sử dụng Clean Architecture system.

## 📁 Files trong thư mục

### 1. 🚀 `SceneSetupExample.cs`
**Mục đích**: Hướng dẫn setup hoàn chỉnh một scene game

**Cách sử dụng**:
1. Tạo Empty GameObject tên `SceneSetup`
2. Add component `SceneSetupExample`
3. Tạo `SceneConfig` ScriptableObject
4. Assign config vào component
5. Click "Setup Scene" trong Inspector

**Tính năng**:
- ✅ Tự động tạo GameManager
- ✅ Tự động tạo các Manager (Input, Audio, UI)
- ✅ Tự động tạo Canvas và UI elements
- ✅ Tự động initialize tất cả systems
- ✅ Test scene setup

### 2. 👤 `PlayerSetupExample.cs`
**Mục đích**: Hướng dẫn setup Player với tất cả components

**Cách sử dụng**:
1. Tạo GameObject tên `Player`
2. Add component `PlayerSetupExample`
3. Tạo `PlayerConfig` ScriptableObject
4. Assign config vào component
5. Click "Setup Player" trong Inspector

**Tính năng**:
- ✅ Tự động add tất cả Player components
- ✅ Tự động setup Unity components (Rigidbody, Collider, SpriteRenderer)
- ✅ Tự động configure Player settings
- ✅ Tự động add starting items
- ✅ Methods để give items, heal, damage player

### 3. 📦 `ItemCreationExample.cs`
**Mục đích**: Hướng dẫn tạo và sử dụng items từ ScriptableObjects

**Cách sử dụng**:
1. Tạo GameObject tên `ItemCreator`
2. Add component `ItemCreationExample`
3. Assign các ItemData ScriptableObjects
4. Assign PlayerInventory reference
5. Click "Demonstrate Item Creation" trong Inspector

**Tính năng**:
- ✅ Tạo Consumable items (Health Potion, Mana Potion)
- ✅ Tạo Equipment items (Sword, Armor)
- ✅ Tạo Material items (Iron Ore, Wood)
- ✅ Demonstrate item usage
- ✅ Demonstrate item cloning
- ✅ Show inventory contents

## 🎯 Quick Start Guide

### Bước 1: Setup Scene
```csharp
// 1. Tạo SceneSetup GameObject
// 2. Add SceneSetupExample component
// 3. Tạo SceneConfig ScriptableObject
// 4. Assign config và click "Setup Scene"
```

### Bước 2: Setup Player
```csharp
// 1. Tạo Player GameObject
// 2. Add PlayerSetupExample component
// 3. Tạo PlayerConfig ScriptableObject
// 4. Assign config và click "Setup Player"
```

### Bước 3: Tạo Items
```csharp
// 1. Tạo ItemCreator GameObject
// 2. Add ItemCreationExample component
// 3. Assign ItemData ScriptableObjects
// 4. Click "Demonstrate Item Creation"
```

## 📋 ScriptableObjects cần tạo

### 1. SceneConfig
```
Right-click → Create → Game → Scene Config
- Game Speed: 1.0
- Enable Pause: true
- Enable Input: true
- Master Volume: 1.0
- UI Scale: 1.0
```

### 2. PlayerConfig
```
Right-click → Create → Game → Player Config
- Move Speed: 5.0
- Jump Force: 10.0
- Max Health: 100
- Inventory Size: 20
- Starting Items: [Assign item data assets]
```

### 3. ItemData Assets
```
Right-click → Create → Item Data → Consumable Item Data
- Item ID: "health_potion"
- Display Name: "Health Potion"
- Heal Amount: 50
- Rarity: Common
- Gold Value: 25
```

## 🔧 Context Menu Commands

Tất cả examples đều có **Context Menu** commands:

### SceneSetupExample:
- `Setup Scene` - Setup hoàn chỉnh scene
- `Test Scene` - Test tất cả systems

### PlayerSetupExample:
- `Setup Player` - Setup hoàn chỉnh player
- `Give Item` - Give item cho player
- `Heal Player` - Heal player
- `Damage Player` - Damage player

### ItemCreationExample:
- `Demonstrate Item Creation` - Tạo và demo items
- `Create Custom Item` - Tạo custom item
- `Demonstrate Item Cloning` - Demo item cloning
- `Show Inventory` - Show inventory contents

## 🎮 Cách sử dụng trong Game

### 1. Trong Start():
```csharp
private void Start()
{
    // Auto-setup khi start
    if (autoSetupOnStart)
    {
        SetupScene();
    }
}
```

### 2. Trong Inspector:
- Check `Auto Setup On Start` để tự động setup
- Assign các ScriptableObject references
- Sử dụng Context Menu để test

### 3. Trong Code:
```csharp
// Give item to player
playerSetupExample.GiveItem(healthPotionData, 5);

// Heal player
playerSetupExample.HealPlayer(50);

// Show inventory
itemCreationExample.ShowInventory();
```

## 🐛 Troubleshooting

### Lỗi thường gặp:
1. **Missing References**: Assign ScriptableObjects trong Inspector
2. **Null Reference**: Check component setup
3. **Not Found**: Sử dụng FindObjectOfType để auto-find

### Debug Tips:
- Sử dụng Debug.Log để track flow
- Check Console cho errors
- Verify ScriptableObject setup
- Test từng component riêng lẻ

## 🚀 Next Steps

Sau khi setup xong:
1. ✅ Test tất cả systems hoạt động
2. ✅ Tạo thêm ScriptableObject assets
3. ✅ Customize Player và Scene configs
4. ✅ Thêm custom items và features
5. ✅ Build và test game

---

**Lưu ý**: Tất cả examples đều có auto-setup và context menu commands để dễ dàng test và sử dụng!
