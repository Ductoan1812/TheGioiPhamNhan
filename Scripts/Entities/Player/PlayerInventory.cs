using UnityEngine;
using GameSystems.Inventory;
using Foundation.Events;

namespace Entities.Player
{
    /// <summary>
    /// Player inventory wrapper component
    /// </summary>
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Inventory Settings")]
        [SerializeField] private int inventoryCapacity = 30;
        [SerializeField] private bool startWithItems = true;

        // Core inventory
        private Inventory inventory;

        // Properties
        public Inventory Inventory => inventory;
        public int Capacity => inventory?.Capacity ?? 0;
        public int UsedSlots => inventory?.UsedSlots ?? 0;
        public int FreeSlots => inventory?.FreeSlots ?? 0;
        public bool IsFull => inventory?.IsFull ?? true;

        // Events
        public System.Action<Item, int> OnItemAdded;
        public System.Action<Item, int> OnItemRemoved;
        public System.Action OnInventoryChanged;

        public void Initialize()
        {
            // Create inventory
            inventory = new Inventory(inventoryCapacity);
            
            // Subscribe to inventory events
            inventory.OnItemAdded += HandleItemAdded;
            inventory.OnItemRemoved += HandleItemRemoved;
            inventory.OnInventoryChanged += HandleInventoryChanged;

            // Add starting items if configured
            if (startWithItems)
            {
                AddStartingItems();
            }
        }

        private void OnDestroy()
        {
            // Unsubscribe from inventory events
            if (inventory != null)
            {
                inventory.OnItemAdded -= HandleItemAdded;
                inventory.OnItemRemoved -= HandleItemRemoved;
                inventory.OnInventoryChanged -= HandleInventoryChanged;
            }
        }

        private void HandleItemAdded(Item item, int quantity)
        {
            OnItemAdded?.Invoke(item, quantity);
            EventBus.Publish(new PlayerItemAddedEvent(item, quantity));
        }

        private void HandleItemRemoved(Item item, int quantity)
        {
            OnItemRemoved?.Invoke(item, quantity);
            EventBus.Publish(new PlayerItemRemovedEvent(item, quantity));
        }

        private void HandleInventoryChanged()
        {
            OnInventoryChanged?.Invoke();
            EventBus.Publish(new PlayerInventoryChangedEvent(inventory));
        }

        /// <summary>
        /// Add item to inventory
        /// </summary>
        public AddItemResult AddItem(Item item, int quantity = 1)
        {
            if (inventory == null)
            {
                Debug.LogError("Inventory not initialized!");
                return new AddItemResult(false, 0, "Inventory not initialized");
            }

            return inventory.AddItem(item, quantity);
        }

        /// <summary>
        /// Remove item from inventory
        /// </summary>
        public RemoveItemResult RemoveItem(string itemId, int quantity = 1)
        {
            if (inventory == null)
            {
                Debug.LogError("Inventory not initialized!");
                return new RemoveItemResult(false, 0, "Inventory not initialized");
            }

            return inventory.RemoveItem(itemId, quantity);
        }

        /// <summary>
        /// Remove item instance from inventory
        /// </summary>
        public RemoveItemResult RemoveItem(Item item, int quantity = 1)
        {
            return RemoveItem(item.Id, quantity);
        }

        /// <summary>
        /// Use item from inventory
        /// </summary>
        public bool UseItem(string itemId, object target = null)
        {
            var itemSlot = GetItemSlot(itemId);
            if (itemSlot != null && !itemSlot.IsEmpty)
            {
                var item = itemSlot.Item;
                
                // Try to use the item
                if (item.Use(target))
                {
                    // If consumable, remove from inventory
                    if (item.IsConsumable)
                    {
                        RemoveItem(itemId, 1);
                    }
                    
                    EventBus.Publish(new PlayerItemUsedEvent(item, target));
                    return true;
                }
            }
            
            return false;
        }

        /// <summary>
        /// Check if inventory has item
        /// </summary>
        public bool HasItem(string itemId, int quantity = 1)
        {
            return inventory?.HasItem(itemId, quantity) ?? false;
        }

        /// <summary>
        /// Get item count
        /// </summary>
        public int GetItemCount(string itemId)
        {
            return inventory?.GetItemCount(itemId) ?? 0;
        }

        /// <summary>
        /// Get item slot
        /// </summary>
        public ItemStack GetItemSlot(string itemId)
        {
            if (inventory == null) return null;

            for (int i = 0; i < inventory.Capacity; i++)
            {
                var slot = inventory.GetSlot(i);
                if (!slot.IsEmpty && slot.Item.Id == itemId)
                {
                    return slot;
                }
            }
            
            return null;
        }

        /// <summary>
        /// Get all items of specific type
        /// </summary>
        public System.Collections.Generic.List<ItemStack> GetItemsByType(ItemType itemType)
        {
            return inventory?.GetItemsByType(itemType) ?? new System.Collections.Generic.List<ItemStack>();
        }

        /// <summary>
        /// Drop item from inventory into world
        /// </summary>
        public bool DropItem(string itemId, int quantity = 1, Vector3? dropPosition = null)
        {
            var removeResult = RemoveItem(itemId, quantity);
            if (removeResult.Success)
            {
                var position = dropPosition ?? (transform.position + transform.forward * 2f);
                
                // Create dropped item in world (this would typically spawn a WorldItem prefab)
                EventBus.Publish(new PlayerItemDroppedEvent(itemId, removeResult.AmountRemoved, position));
                
                return true;
            }
            
            return false;
        }

        /// <summary>
        /// Toggle inventory UI
        /// </summary>
        public void ToggleInventoryUI()
        {
            EventBus.Publish(new InventoryUIToggleEvent());
        }

        /// <summary>
        /// Clear inventory
        /// </summary>
        public void ClearInventory()
        {
            inventory?.Clear();
        }

        private void AddStartingItems()
        {
            // Add some basic starting items
            var healthPotion = new ConsumableItem("health_potion", "Health Potion", healAmount: 50);
            var manaPotion = new ConsumableItem("mana_potion", "Mana Potion", manaAmount: 30);
            
            AddItem(healthPotion, 3);
            AddItem(manaPotion, 2);
        }

        #region Debug Methods
        
        [ContextMenu("Add Test Items")]
        private void AddTestItems()
        {
            if (inventory == null) return;

            var sword = new EquipmentItem("iron_sword", "Iron Sword", ItemType.Weapon);
            var shield = new EquipmentItem("wooden_shield", "Wooden Shield", ItemType.Equipment);
            var material = new MaterialItem("iron_ore", "Iron Ore", "Metal", 1);

            AddItem(sword, 1);
            AddItem(shield, 1);
            AddItem(material, 10);
        }

        [ContextMenu("Clear Inventory")]
        private void DebugClearInventory()
        {
            ClearInventory();
        }

        #endregion
    }

    /// <summary>
    /// Player inventory events
    /// </summary>
    public class PlayerItemAddedEvent : GameEvent<PlayerItemData>
    {
        public Item Item => Data.Item;
        public int Quantity => Data.Quantity;

        public PlayerItemAddedEvent(Item item, int quantity) 
            : base(new PlayerItemData(item, quantity))
        {
        }
    }

    public class PlayerItemRemovedEvent : GameEvent<PlayerItemData>
    {
        public Item Item => Data.Item;
        public int Quantity => Data.Quantity;

        public PlayerItemRemovedEvent(Item item, int quantity) 
            : base(new PlayerItemData(item, quantity))
        {
        }
    }

    public class PlayerItemUsedEvent : GameEvent<PlayerItemUsedData>
    {
        public Item Item => Data.Item;
        public object Target => Data.Target;

        public PlayerItemUsedEvent(Item item, object target) 
            : base(new PlayerItemUsedData(item, target))
        {
        }
    }

    public class PlayerItemDroppedEvent : GameEvent<PlayerItemDroppedData>
    {
        public string ItemId => Data.ItemId;
        public int Quantity => Data.Quantity;
        public Vector3 Position => Data.Position;

        public PlayerItemDroppedEvent(string itemId, int quantity, Vector3 position) 
            : base(new PlayerItemDroppedData(itemId, quantity, position))
        {
        }
    }

    public class PlayerInventoryChangedEvent : GameEvent<Inventory>
    {
        public PlayerInventoryChangedEvent(Inventory inventory) : base(inventory)
        {
        }
    }

    public class InventoryUIToggleEvent : GameEvent
    {
        public InventoryUIToggleEvent() : base("PlayerInventory")
        {
        }
    }

    // Data classes
    [System.Serializable]
    public class PlayerItemData
    {
        public Item Item { get; }
        public int Quantity { get; }

        public PlayerItemData(Item item, int quantity)
        {
            Item = item;
            Quantity = quantity;
        }
    }

    [System.Serializable]
    public class PlayerItemUsedData
    {
        public Item Item { get; }
        public object Target { get; }

        public PlayerItemUsedData(Item item, object target)
        {
            Item = item;
            Target = target;
        }
    }

    [System.Serializable]
    public class PlayerItemDroppedData
    {
        public string ItemId { get; }
        public int Quantity { get; }
        public Vector3 Position { get; }

        public PlayerItemDroppedData(string itemId, int quantity, Vector3 position)
        {
            ItemId = itemId;
            Quantity = quantity;
            Position = position;
        }
    }
}
