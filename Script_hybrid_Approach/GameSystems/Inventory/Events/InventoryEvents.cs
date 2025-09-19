using System;
using UnityEngine;
using Foundation.Events;
using GameSystems.Inventory.Core;

namespace GameSystems.Inventory
{
    /// <summary>
    /// Event khi item request được tạo
    /// </summary>
    [Serializable]
    public class ItemRequestEvent : GameEvent
    {
        public string ItemId { get; private set; }
        public int Quantity { get; private set; }
        public string RequesterId { get; private set; }
        
        public ItemRequestEvent(string itemId, int quantity, string requesterId, string source = null) 
            : base(source ?? "ItemManager")
        {
            ItemId = itemId;
            Quantity = quantity;
            RequesterId = requesterId;
        }
    }
    
    /// <summary>
    /// Event khi item database được load
    /// </summary>
    [Serializable]
    public class ItemDatabaseLoadedEvent : GameEvent
    {
        public int ItemCount { get; private set; }
        public string DatabasePath { get; private set; }
        
        public ItemDatabaseLoadedEvent(int itemCount, string databasePath, string source = null) 
            : base(source ?? "ItemManager")
        {
            ItemCount = itemCount;
            DatabasePath = databasePath;
        }
    }
    
    /// <summary>
    /// Event khi item được register
    /// </summary>
    [Serializable]
    public class ItemRegisteredEvent : GameEvent
    {
        public string ItemId { get; private set; }
        public ItemDefinition ItemDefinition { get; private set; }
        
        public ItemRegisteredEvent(string itemId, ItemDefinition itemDefinition, string source = null) 
            : base(source ?? "ItemManager")
        {
            ItemId = itemId;
            ItemDefinition = itemDefinition;
        }
    }
    
    /// <summary>
    /// Event khi item được unregister
    /// </summary>
    [Serializable]
    public class ItemUnregisteredEvent : GameEvent
    {
        public string ItemId { get; private set; }
        
        public ItemUnregisteredEvent(string itemId, string source = null) 
            : base(source ?? "ItemManager")
        {
            ItemId = itemId;
        }
    }
    
    /// <summary>
    /// Event khi item definition được update
    /// </summary>
    [Serializable]
    public class ItemUpdatedEvent : GameEvent
    {
        public string ItemId { get; private set; }
        public ItemDefinition NewDefinition { get; private set; }
        
        public ItemUpdatedEvent(string itemId, ItemDefinition newDefinition, string source = null) 
            : base(source ?? "ItemManager")
        {
            ItemId = itemId;
            NewDefinition = newDefinition;
        }
    }
    
    /// <summary>
    /// Event khi item được tạo mới
    /// </summary>
    [Serializable]
    public class ItemCreatedEvent : GameEvent
    {
        public InventoryItem Item { get; private set; }
        public string ItemId { get; private set; }
        
        public ItemCreatedEvent(InventoryItem item, string itemId, string source = null) 
            : base(source ?? "ItemManager")
        {
            Item = item;
            ItemId = itemId;
        }
    }
    
    /// <summary>
    /// Event khi item request được fulfill
    /// </summary>
    [Serializable]
    public class ItemProvidedEvent : GameEvent
    {
        public string ItemId { get; private set; }
        public int Quantity { get; private set; }
        public string RequesterId { get; private set; }
        public InventoryItem ProvidedItem { get; private set; }
        
        public ItemProvidedEvent(string itemId, int quantity, string requesterId, InventoryItem providedItem, string source = null) 
            : base(source ?? "ItemManager")
        {
            ItemId = itemId;
            Quantity = quantity;
            RequesterId = requesterId;
            ProvidedItem = providedItem;
        }
    }
    
    /// <summary>
    /// Event khi item request thất bại
    /// </summary>
    [Serializable]
    public class ItemRequestFailedEvent : GameEvent
    {
        public string ItemId { get; private set; }
        public int RequestedQuantity { get; private set; }
        public string RequesterId { get; private set; }
        public string FailureReason { get; private set; }
        
        public ItemRequestFailedEvent(string itemId, int requestedQuantity, string requesterId, string failureReason, string source = null) 
            : base(source ?? "ItemManager")
        {
            ItemId = itemId;
            RequestedQuantity = requestedQuantity;
            RequesterId = requesterId;
            FailureReason = failureReason;
        }
    }
    
    /// <summary>
    /// Event khi item được drop
    /// </summary>
    [Serializable]
    public class ItemDroppedEvent : GameEvent
    {
        public InventoryItem Item { get; private set; }
        public Vector3 DropPosition { get; private set; }
        public string DropperId { get; private set; }
        
        public ItemDroppedEvent(InventoryItem item, Vector3 dropPosition, string dropperId, string source = null) 
            : base(source ?? "InventoryManager")
        {
            Item = item;
            DropPosition = dropPosition;
            DropperId = dropperId;
        }
    }
    
    /// <summary>
    /// Event khi item được pick up từ world
    /// </summary>
    [Serializable]
    public class ItemPickedUpEvent : GameEvent
    {
        public InventoryItem Item { get; private set; }
        public Vector3 PickupPosition { get; private set; }
        public string PickerId { get; private set; }
        
        public ItemPickedUpEvent(InventoryItem item, Vector3 pickupPosition, string pickerId, string source = null) 
            : base(source ?? "InventoryManager")
        {
            Item = item;
            PickupPosition = pickupPosition;
            PickerId = pickerId;
        }
    }
    
    /// <summary>
    /// Event khi item được add vào inventory
    /// </summary>
    [Serializable]
    public class ItemAddedToInventoryEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public InventoryItem Item { get; private set; }
        public int SlotIndex { get; private set; }
        
        public ItemAddedToInventoryEvent(string entityId, InventoryItem item, int slotIndex, string source = null) 
            : base(source ?? "InventoryCollection")
        {
            EntityId = entityId;
            Item = item;
            SlotIndex = slotIndex;
        }
    }
    
    /// <summary>
    /// Event khi item được remove khỏi inventory
    /// </summary>
    [Serializable]
    public class ItemRemovedFromInventoryEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public InventoryItem Item { get; private set; }
        public int SlotIndex { get; private set; }
        
        public ItemRemovedFromInventoryEvent(string entityId, InventoryItem item, int slotIndex, string source = null) 
            : base(source ?? "InventoryCollection")
        {
            EntityId = entityId;
            Item = item;
            SlotIndex = slotIndex;
        }
    }
    
    /// <summary>
    /// Event khi item được sử dụng
    /// </summary>
    [Serializable]
    public class ItemUsedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public InventoryItem Item { get; private set; }
        public string UsageContext { get; private set; }
        
        public ItemUsedEvent(string entityId, InventoryItem item, string usageContext, string source = null) 
            : base(source ?? "InventoryManager")
        {
            EntityId = entityId;
            Item = item;
            UsageContext = usageContext;
        }
    }
    
    /// <summary>
    /// Event khi item được equip từ inventory
    /// </summary>
    [Serializable]
    public class ItemEquippedFromInventoryEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public InventoryItem Item { get; private set; }
        public EquipmentSlot Slot { get; private set; }
        
        public ItemEquippedFromInventoryEvent(string entityId, InventoryItem item, EquipmentSlot slot, string source = null) 
            : base(source ?? "InventoryManager")
        {
            EntityId = entityId;
            Item = item;
            Slot = slot;
        }
    }
    
    /// <summary>
    /// Event khi item được unequip về inventory
    /// </summary>
    [Serializable]
    public class ItemUnequippedToInventoryEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public InventoryItem Item { get; private set; }
        public EquipmentSlot Slot { get; private set; }
        
        public ItemUnequippedToInventoryEvent(string entityId, InventoryItem item, EquipmentSlot slot, string source = null) 
            : base(source ?? "InventoryManager")
        {
            EntityId = entityId;
            Item = item;
            Slot = slot;
        }
    }
    
    /// <summary>
    /// Event khi item được drop vào world
    /// </summary>
    [Serializable]
    public class ItemDroppedInWorldEvent : GameEvent
    {
        public InventoryItem Item { get; private set; }
        public Vector3 WorldPosition { get; private set; }
        public string EntityId { get; private set; }
        
        public ItemDroppedInWorldEvent(InventoryItem item, Vector3 worldPosition, string entityId, string source = null) 
            : base(source ?? "InventoryManager")
        {
            Item = item;
            WorldPosition = worldPosition;
            EntityId = entityId;
        }
    }
    
    /// <summary>
    /// Event cho custom item use effects
    /// </summary>
    [Serializable]
    public class CustomItemUseEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public InventoryItem Item { get; private set; }
        public string EffectType { get; private set; }
        public float EffectValue { get; private set; }
        
        public CustomItemUseEvent(string entityId, InventoryItem item, string effectType, float effectValue, string source = null) 
            : base(source ?? "InventoryManager")
        {
            EntityId = entityId;
            Item = item;
            EffectType = effectType;
            EffectValue = effectValue;
        }
    }
    
    /// <summary>
    /// Event khi item được equip
    /// </summary>
    [Serializable]
    public class ItemEquippedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public InventoryItem Item { get; private set; }
        public EquipmentSlot Slot { get; private set; }
        
        public ItemEquippedEvent(string entityId, InventoryItem item, EquipmentSlot slot, string source = null) 
            : base(source ?? "EquipmentManager")
        {
            EntityId = entityId;
            Item = item;
            Slot = slot;
        }
    }
    
    /// <summary>
    /// Event khi item được unequip
    /// </summary>
    [Serializable]
    public class ItemUnequippedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public InventoryItem Item { get; private set; }
        public EquipmentSlot Slot { get; private set; }
        
        public ItemUnequippedEvent(string entityId, InventoryItem item, EquipmentSlot slot, string source = null) 
            : base(source ?? "EquipmentManager")
        {
            EntityId = entityId;
            Item = item;
            Slot = slot;
        }
    }
    
    /// <summary>
    /// Event khi equipment được swap
    /// </summary>
    [Serializable]
    public class EquipmentSwappedEvent : GameEvent
    {
        public string EntityId { get; private set; }
        public InventoryItem OldItem { get; private set; }
        public InventoryItem NewItem { get; private set; }
        public EquipmentSlot Slot { get; private set; }
        
        public EquipmentSwappedEvent(string entityId, InventoryItem oldItem, InventoryItem newItem, EquipmentSlot slot, string source = null) 
            : base(source ?? "EquipmentManager")
        {
            EntityId = entityId;
            OldItem = oldItem;
            NewItem = newItem;
            Slot = slot;
        }
    }
}
