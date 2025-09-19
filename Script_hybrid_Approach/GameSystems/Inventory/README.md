# GameSystems/Inventory - Hệ Thống Inventory Nâng Cao

## Tổng Quan

Hệ thống Inventory được cải tiến từ `ItemData.cs`, `InventoryService.cs`, và `PlayerInventory.cs` hiện tại, sử dụng Foundation architecture patterns để cung cấp:

- **Quản lý item definitions** với metadata đầy đủ
- **Inventory collections** cho từng entity với stacking, validation
- **Equipment system** với stat bonuses tự động
- **Drop/pickup mechanics** trong world
- **Event-driven architecture** cho UI và game logic
- **Serialization** cho save/load system

## Cấu Trúc Hệ Thống

```
GameSystems/Inventory/
├── Core/
│   ├── ItemDefinition.cs      # Item definitions (cải tiến từ ItemData)
│   └── InventoryItem.cs       # Item instances trong inventory
├── ItemManager.cs             # Manager cho item database
├── InventoryCollection.cs     # Collection quản lý slots cho 1 entity
├── EquipmentManager.cs        # Hệ thống equipment & stat bonuses
├── InventoryManager.cs        # Manager tổng hợp (cải tiến từ InventoryService)
└── README.md                  # Documentation này
```

## Core Components

### 1. ItemDefinition (Core/ItemDefinition.cs)
**Cải tiến từ:** `ItemData.cs`

```csharp
// Item definition với metadata đầy đủ
var swordDef = new ItemDefinition
{
    Id = "iron_sword",
    Name = "Kiếm Sắt",
    Category = ItemCategory.Equipment,
    Rarity = ItemRarity.Common,
    Element = ItemElement.Metal,
    MaxStackSize = 1,
    ItemStats = new ItemStats
    {
        attack = 25f,
        criticalRate = 0.05f
    },
    EquipmentSlots = new[] { EquipmentSlot.Weapon }
};
```

**Tính năng chính:**
- Enums đầy đủ: `ItemCategory`, `ItemRarity`, `ItemElement`, `Realm`
- `ItemStats` với cultivation stats (atk, def, hp, qi, moveSpd, etc.)
- `ItemResistances` cho elemental resistance
- `ItemAffix` cho random/magic properties
- `ItemUseEffect` cho consumables
- Equipment slot validation
- Power rating calculation
- Realm requirements

### 2. InventoryItem (Core/InventoryItem.cs)

```csharp
// Item instance trong inventory
var item = new InventoryItem(itemDefinition, quantity: 5);
item.AddStatOverride("attack", 30f); // Custom stat override
item.AddAffix(new ItemAffix("Sharp", BonusType.Percentage, 10f));

// Stacking operations
var splitItem = item.SplitStack(2);
bool canMerge = item.CanStackWith(otherItem);
item.TryMergeWith(otherItem);
```

**Tính năng chính:**
- Quantity management với validation
- Stat overrides cho unique items
- Affix system cho random properties
- Stack operations (split, merge)
- Cloning cho item duplication
- Serialization support

### 3. ItemManager
**Singleton manager** cho item database:

```csharp
// Load và query items
ItemManager.Instance.LoadItemDatabase("items_database.json");
var swordDef = ItemManager.Instance.GetItemDefinition("iron_sword");
var allWeapons = ItemManager.Instance.GetItemsByCategory(ItemCategory.Equipment);

// Create items
var item = ItemManager.Instance.CreateItem("iron_sword", quantity: 1);
var customItem = ItemManager.Instance.CreateItem("potion", 5, new Dictionary<string, object>
{
    ["healing"] = 50f
});
```

### 4. InventoryCollection
**Quản lý inventory** cho 1 entity (thay thế `PlayerInventory.cs`):

```csharp
// Tạo inventory cho player
var inventory = new InventoryCollection("player", maxSlots: 30);

// Item operations
var (success, remaining) = inventory.TryAddItem(item);
bool removed = inventory.TryRemoveItem("iron_sword", 1);
bool moved = inventory.TryMoveItem(fromSlot: 0, toSlot: 5);

// Queries
bool hasItem = inventory.HasItem("health_potion", minQuantity: 3);
int quantity = inventory.GetItemQuantity("iron_sword");
var items = inventory.FindItemsByCategory(ItemCategory.Consumable);

// Validation & cleanup
inventory.ValidateInventory();
inventory.CleanupInventory();
inventory.CompactInventory();
```

### 5. EquipmentManager
**Hệ thống equipment** với stat integration:

```csharp
// Equipment operations
bool equipped = EquipmentManager.Instance.TryEquipItem("player", weapon, EquipmentSlot.Weapon);
var unequipped = EquipmentManager.Instance.TryUnequipItem("player", EquipmentSlot.Weapon);

// Queries
var equippedWeapon = EquipmentManager.Instance.GetEquippedItem("player", EquipmentSlot.Weapon);
var allEquipped = EquipmentManager.Instance.GetAllEquippedItems("player");
float powerRating = EquipmentManager.Instance.GetEquipmentPowerRating("player");

// Auto stat application
bool canEquip = EquipmentManager.Instance.CanEquipItemToSlot(item, slot);
```

**Equipment Slots:**
- `Weapon`, `Helmet`, `Armor`, `Boots`, `Gloves`
- `Ring1`, `Ring2`, `Amulet`, `Belt`, `Cloak`

### 6. InventoryManager
**Manager tổng hợp** (cải tiến từ `InventoryService.cs`):

```csharp
// Inventory operations
var inventory = InventoryManager.Instance.GetInventory("player");
var (success, remaining) = InventoryManager.Instance.AddItemToInventory("player", "sword", 1);
bool removed = InventoryManager.Instance.RemoveItemFromInventory("player", "sword", 1);

// Transfer between entities
bool transferred = InventoryManager.Instance.TransferItem("player", "storage", "gold", 100);

// Equipment integration
bool equipped = InventoryManager.Instance.EquipItemFromInventory("player", slot: 0, EquipmentSlot.Weapon);
bool unequipped = InventoryManager.Instance.UnequipItemToInventory("player", EquipmentSlot.Weapon);

// Item usage
bool used = InventoryManager.Instance.UseItem("player", "health_potion", 1);

// World drops
bool dropped = InventoryManager.Instance.DropItem("player", "sword", 1, position);
```

## Event System

Hệ thống sử dụng **Foundation EventBus** cho communication:

```csharp
// Item events
EventBus.Subscribe<ItemAddedToInventoryEvent>(OnItemAdded);
EventBus.Subscribe<ItemRemovedFromInventoryEvent>(OnItemRemoved);
EventBus.Subscribe<ItemUsedEvent>(OnItemUsed);

// Equipment events  
EventBus.Subscribe<ItemEquippedEvent>(OnItemEquipped);
EventBus.Subscribe<ItemUnequippedEvent>(OnItemUnequipped);

// World events
EventBus.Subscribe<ItemDroppedInWorldEvent>(OnItemDropped);
EventBus.Subscribe<ItemPickedUpEvent>(OnItemPickedUp);
```

## Stats Integration

Tự động tích hợp với **GameSystems/Stats**:

```csharp
// Equipment tự động apply stat bonuses
var sword = new InventoryItem(swordDefinition);
EquipmentManager.Instance.TryEquipItem("player", sword, EquipmentSlot.Weapon);
// -> Tự động add StatBonus cho attack, critRate, etc.

// Item use effects
InventoryManager.Instance.UseItem("player", "health_potion", 1);
// -> Apply health restore thông qua StatBonus system
```

## Serialization

**Complete save/load support:**

```csharp
// Save inventory data
var inventoryData = inventory.GetSerializableData();
string json = JsonUtility.ToJson(inventoryData);

// Load inventory data
var loadedData = JsonUtility.FromJson<SerializableInventoryData>(json);
inventory.LoadFromSerializableData(loadedData);

// Equipment data
var equipData = EquipmentManager.Instance.GetSerializableData("player");
EquipmentManager.Instance.LoadEquipmentData("player", equipData);

// Auto-save support
InventoryManager.Instance.SaveAllInventories(); // Auto-called theo interval
InventoryManager.Instance.LoadAllInventories();
```

## Migration từ Hệ Thống Cũ

### Từ ItemData.cs → ItemDefinition.cs
```csharp
// Cũ: ItemData
public class ItemData
{
    public string itemName;
    public Sprite itemIcon;
    public ItemCategory category;
    // ...
}

// Mới: ItemDefinition (enhanced)
var itemDef = new ItemDefinition
{
    Id = "unique_id",
    Name = itemData.itemName,
    Icon = itemData.itemIcon,
    Category = itemData.category,
    // + nhiều tính năng mới
    Rarity = ItemRarity.Common,
    ItemStats = new ItemStats { /* stats */ },
    ItemUseEffect = new ItemUseEffect { /* effects */ }
};
```

### Từ InventoryService.cs → InventoryManager.cs
```csharp
// Cũ: InventoryService
InventoryService.Instance.AddItem(item);
InventoryService.Instance.RemoveItem(item);

// Mới: InventoryManager (enhanced)
InventoryManager.Instance.AddItemToInventory("player", "item_id", quantity);
InventoryManager.Instance.RemoveItemFromInventory("player", "item_id", quantity);
// + Multi-entity support, equipment integration, world drops, etc.
```

### Từ PlayerInventory.cs → InventoryCollection.cs
```csharp
// Cũ: PlayerInventory (single entity)
playerInventory.AddItem(item);
playerInventory.equipmentSlots[0] = item;

// Mới: InventoryCollection + EquipmentManager (multi-entity)
var inventory = InventoryManager.Instance.GetInventory("player");
inventory.TryAddItem(item);
EquipmentManager.Instance.TryEquipItem("player", item, EquipmentSlot.Weapon);
```

## Performance & Optimization

1. **Caching:** Fast lookup dictionaries cho item queries
2. **Event Batching:** Grouped inventory change events
3. **Lazy Loading:** Items loaded on-demand từ database
4. **Validation:** Optional validation modes cho debugging
5. **Memory Management:** Object pooling cho InventoryItem instances

## Configuration

```csharp
[Header("Inventory Configuration")]
public int defaultInventorySize = 30;
public bool autoSaveInventory = true;
public float autoSaveInterval = 60f;
public bool validateEquipmentOnLoad = true;

[Header("Drop System")]
public GameObject droppedItemPrefab;
public LayerMask dropLayerMask = -1;
public float dropRadius = 1f;
```

## Debug & Testing

```csharp
// Debug utilities
string summary = InventoryManager.Instance.GetInventorySummary("player");
string equipSummary = EquipmentManager.Instance.GetEquipmentSummary("player");

// Validation
InventoryManager.Instance.ValidateAllInventories();
int cleaned = InventoryManager.Instance.CleanupAllInventories();

// Testing helpers
ItemManager.Instance.LoadTestDatabase();
InventoryManager.Instance.CreateTestInventory("player");
```

## Best Practices

1. **Entity IDs:** Sử dụng consistent entity IDs across systems
2. **Item IDs:** Unique, descriptive item identifiers
3. **Event Handling:** Subscribe/unsubscribe properly để tránh memory leaks
4. **Validation:** Enable validation trong development, disable trong production
5. **Serialization:** Test save/load functionality regularly
6. **Performance:** Use queries sparingly, cache results khi cần thiết

## Kết Luận

Hệ thống Inventory mới cung cấp:
- ✅ **Enhanced functionality** so với hệ thống cũ
- ✅ **Multi-entity support** thay vì chỉ player
- ✅ **Foundation integration** với Events, Stats, Architecture
- ✅ **Equipment system** với automatic stat bonuses
- ✅ **World drop mechanics** cho immersive gameplay
- ✅ **Complete serialization** cho save/load
- ✅ **Event-driven architecture** cho UI reactivity
- ✅ **Performance optimizations** và caching
- ✅ **Backward compatibility** với migration path rõ ràng

Hệ thống sẵn sàng để tích hợp với UI, Combat, và các systems khác trong game.