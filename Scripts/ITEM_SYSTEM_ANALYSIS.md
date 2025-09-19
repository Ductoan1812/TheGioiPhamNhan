# 🎮 Phân tích hệ thống quản lý Item

## 📋 Tổng quan hệ thống Item

Hệ thống Item trong dự án Clean Architecture của bạn được thiết kế theo **3-layer pattern**:

```
📁 Infrastructure.Data (ScriptableObjects) 
    ↓ Tạo runtime instances
📁 GameSystems.Inventory (Core Logic)
    ↓ Quản lý và xử lý
📁 Entities.Player (Player Integration)
```

---

## 🏗️ Kiến trúc hệ thống

### 1. 📦 **Infrastructure.Data Layer** - Data Definition
**Mục đích**: Định nghĩa và lưu trữ dữ liệu Item

#### **ItemData.cs** - Base ScriptableObject
```csharp
[CreateAssetMenu(fileName = "New Item", menuName = "Game Data/Items/Item")]
public class ItemData : ScriptableObject
{
    // Basic Info
    string itemId, displayName, description
    ItemType itemType, ItemRarity rarity
    Sprite icon, GameObject worldPrefab
    
    // Stacking
    int maxStackSize, bool isConsumable
    
    // Value
    int goldValue, int sellValue
    
    // Core Method
    public virtual Item CreateItem() // Tạo runtime instance
}
```

#### **Derived ItemData Classes**:
- **ConsumableItemData** → Tạo `ConsumableItem`
- **EquipmentItemData** → Tạo `EquipmentItem`  
- **MaterialItemData** → Tạo `MaterialItem`

---

### 2. 🎯 **GameSystems.Inventory Layer** - Core Logic
**Mục đích**: Logic xử lý Item, không phụ thuộc Unity

#### **Item.cs** - Base Item Class
```csharp
public abstract class Item
{
    // Core Properties
    string Id, DisplayName, Description
    ItemType Type, ItemRarity Rarity
    int MaxStackSize, bool IsConsumable
    int Value, Sprite Icon
    
    // Core Methods
    public virtual bool Use(object target = null)
    public virtual bool CanStackWith(Item other)
    public abstract Item Clone()
}
```

#### **Item Types**:
- **ConsumableItem**: Potions, food (healAmount, manaAmount)
- **EquipmentItem**: Weapons, armor (statModifiers, durability)
- **MaterialItem**: Crafting materials (materialCategory, tier)

#### **Inventory.cs** - Container Logic
```csharp
public class Inventory
{
    // Core Data
    ItemStack[] slots
    int capacity
    
    // Core Methods
    public bool AddItem(Item item, int quantity = 1)
    public bool RemoveItem(Item item, int quantity = 1)
    public Item GetItem(int slotIndex)
    public bool MoveItem(int fromSlot, int toSlot)
    public List<Item> GetAllItems()
    public List<Item> GetItemsByType(ItemType type)
}
```

#### **ItemStack.cs** - Stack Management
```csharp
public class ItemStack
{
    Item item
    int quantity
    
    // Stack Operations
    public int AddItems(int amount)
    public int RemoveItems(int amount)
    public bool CanAddItems(int amount)
    public ItemStack SplitStack(int amount)
}
```

---

### 3. 👤 **Entities.Player Layer** - Player Integration
**Mục đích**: Tích hợp Item system với Player

#### **PlayerInventory.cs** - Player Wrapper
```csharp
public class PlayerInventory : MonoBehaviour
{
    // Core Inventory
    private Inventory inventory
    
    // Unity Integration
    [SerializeField] int inventoryCapacity = 30
    [SerializeField] bool startWithItems = true
    
    // Events
    public Action<Item, int> OnItemAdded
    public Action<Item, int> OnItemRemoved
    public Action OnInventoryChanged
    
    // Methods
    public void Initialize()
    public bool AddItem(Item item, int quantity = 1)
    public bool RemoveItem(Item item, int quantity = 1)
    public void UseItem(Item item)
}
```

---

## 🔄 Luồng hoạt động của hệ thống

### **1. Tạo Item từ ScriptableObject:**
```csharp
// Designer tạo ItemData asset trong Unity Editor
ConsumableItemData healthPotionData = // Assign trong Inspector

// Runtime: Tạo Item instance
var healthPotion = healthPotionData.CreateItem() as ConsumableItem;
```

### **2. Thêm Item vào Inventory:**
```csharp
// PlayerInventory nhận Item
playerInventory.AddItem(healthPotion);

// PlayerInventory delegate cho Inventory core
inventory.AddItem(item, quantity);

// Inventory tạo ItemStack và quản lý slots
var stack = new ItemStack(item, quantity);
slots[slotIndex] = stack;
```

### **3. Sử dụng Item:**
```csharp
// Player sử dụng Item
playerInventory.UseItem(healthPotion);

// Item.Use() được gọi
bool success = healthPotion.Use(player);

// Nếu thành công, remove khỏi inventory
if (success) {
    inventory.RemoveItem(healthPotion, 1);
}
```

---

## 🎯 Các tính năng chính

### **1. Item Stacking:**
- Items có `MaxStackSize` khác nhau
- Consumable: 99, Equipment: 1, Material: 999
- `ItemStack` quản lý quantity trong mỗi slot

### **2. Item Types:**
- **ConsumableItem**: Có thể sử dụng (Use())
- **EquipmentItem**: Có stat modifiers và durability
- **MaterialItem**: Dùng cho crafting

### **3. Item Rarity:**
- Common, Uncommon, Rare, Epic, Legendary
- Ảnh hưởng đến value và drop rate

### **4. Event System:**
- `OnItemAdded`, `OnItemRemoved`, `OnInventoryChanged`
- UI có thể subscribe để update display

---

## 🔧 Cách sử dụng trong Code

### **Tạo Item:**
```csharp
// Từ ScriptableObject
var item = itemData.CreateItem();

// Tạo trực tiếp
var customItem = new ConsumableItem("id", "name", 50, 0);
```

### **Quản lý Inventory:**
```csharp
// Add item
bool added = playerInventory.AddItem(item);

// Remove item  
bool removed = playerInventory.RemoveItem(item, 5);

// Get all items
var allItems = playerInventory.GetAllItems();

// Filter by type
var consumables = playerInventory.GetItemsByType(ItemType.Consumable);
```

### **Sử dụng Item:**
```csharp
// Use item
playerInventory.UseItem(healthPotion);

// Check if can use
if (healthPotion.IsConsumable) {
    healthPotion.Use(player);
}
```

---

## 🎮 Ưu điểm của hệ thống

### **✅ Clean Architecture:**
- **Infrastructure.Data**: Pure data, không logic
- **GameSystems.Inventory**: Pure logic, testable
- **Entities.Player**: Unity integration

### **✅ Flexible:**
- Dễ thêm item types mới
- ScriptableObject cho designer
- Event-driven cho UI

### **✅ Scalable:**
- Inventory capacity configurable
- Item stacking system
- Stat modifiers cho equipment

### **✅ Maintainable:**
- Separation of concerns
- Clear responsibilities
- Easy to debug

---

## 🚀 Mở rộng hệ thống

### **Thêm Item Type mới:**
1. Tạo class kế thừa `Item`
2. Tạo `ItemData` tương ứng
3. Implement `CreateItem()` method
4. Add vào `ItemType` enum

### **Thêm tính năng mới:**
- **Item Enchanting**: Thêm enchantments
- **Item Trading**: Trade system
- **Item Crafting**: Crafting recipes
- **Item Durability**: Equipment wear

---

## 📊 Tóm tắt

Hệ thống Item của bạn được thiết kế rất tốt với:

- **3-layer architecture** rõ ràng
- **ScriptableObject** cho data management
- **Core logic** tách biệt khỏi Unity
- **Event system** cho communication
- **Flexible và scalable** design

Đây là một implementation rất solid của Clean Architecture pattern! 🎮✨
