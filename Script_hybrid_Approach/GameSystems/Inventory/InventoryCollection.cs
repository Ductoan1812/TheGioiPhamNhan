using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Foundation.Events;
using Foundation.Utils;
using GameSystems.Inventory.Core;

namespace GameSystems.Inventory
{
    /// <summary>
    /// InventoryCollection - cải tiến từ PlayerInventory hiện tại.
    /// Quản lý inventory cho 1 entity với slots, stacking, events.
    /// </summary>
    [Serializable]
    public class InventoryCollection
    {
        [SerializeField] private string ownerId;
        [SerializeField] private int maxSlots = 30;
        [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();
        
        // Fast lookup cache
        private Dictionary<int, InventoryItem> slotLookup;
        private bool lookupDirty = true;
        
        // Events
        public event Action<InventoryItem, int> OnItemAdded;    // (item, slot)
        public event Action<InventoryItem, int> OnItemRemoved; // (item, slot)
        public event Action<InventoryItem, int, int> OnItemQuantityChanged; // (item, slot, newQuantity)
        public event Action<int, int> OnItemMoved; // (fromSlot, toSlot)
        public event Action OnInventoryChanged;
        
        // Properties
        public string OwnerId => ownerId;
        public int MaxSlots => maxSlots;
        public int UsedSlots => items.Count(i => !i.IsEmpty);
        public int FreeSlots => maxSlots - UsedSlots;
        public IReadOnlyList<InventoryItem> AllItems => items;
        public bool IsFull => FreeSlots <= 0;
        
        public InventoryCollection(string ownerId, int maxSlots = 30)
        {
            this.ownerId = ownerId;
            this.maxSlots = maxSlots;
            InitializeSlots();
        }
        
        #region Initialization
        
        /// <summary>
        /// Initialize empty slots
        /// </summary>
        private void InitializeSlots()
        {
            items.Clear();
            for (int i = 0; i < maxSlots; i++)
            {
                items.Add(new InventoryItem());
            }
            MarkLookupDirty();
        }
        
        /// <summary>
        /// Resize inventory
        /// </summary>
        public void Resize(int newMaxSlots)
        {
            if (newMaxSlots < 1) return;
            
            int oldSize = maxSlots;
            maxSlots = newMaxSlots;
            
            if (newMaxSlots > oldSize)
            {
                // Add new empty slots
                for (int i = oldSize; i < newMaxSlots; i++)
                {
                    items.Add(new InventoryItem());
                }
            }
            else if (newMaxSlots < oldSize)
            {
                // Remove excess slots (move items if possible)
                for (int i = oldSize - 1; i >= newMaxSlots; i--)
                {
                    var item = GetItemAtSlot(i);
                    if (!item.IsEmpty)
                    {
                        // Try to move item to available slot
                        if (!TryAddItem(item).success)
                        {
                            // Drop item if can't fit
                            DebugUtils.LogWarning($"[InventoryCollection] Item dropped during resize: {item}");
                            EventBus.Dispatch(new ItemDroppedEvent(ownerId, item, "Inventory resize"));
                        }
                    }
                    items.RemoveAt(i);
                }
            }
            
            MarkLookupDirty();
            OnInventoryChanged?.Invoke();
            
            DebugUtils.Log($"[InventoryCollection] Resized inventory from {oldSize} to {newMaxSlots} slots");
        }
        
        #endregion
        
        #region Slot Management
        
        /// <summary>
        /// Get item at specific slot
        /// </summary>
        public InventoryItem GetItemAtSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= items.Count) return new InventoryItem();
            return items[slotIndex];
        }
        
        /// <summary>
        /// Set item at specific slot
        /// </summary>
        public bool SetItemAtSlot(int slotIndex, InventoryItem item)
        {
            if (slotIndex < 0 || slotIndex >= maxSlots) return false;
            
            var oldItem = GetItemAtSlot(slotIndex);
            
            // Ensure items list is large enough
            while (items.Count <= slotIndex)
            {
                items.Add(new InventoryItem());
            }
            
            items[slotIndex] = item ?? new InventoryItem();
            if (item != null)
            {
                item.SlotIndex = slotIndex;
            }
            
            MarkLookupDirty();
            
            // Fire events
            if (!oldItem.IsEmpty)
            {
                OnItemRemoved?.Invoke(oldItem, slotIndex);
            }
            
            if (item != null && !item.IsEmpty)
            {
                OnItemAdded?.Invoke(item, slotIndex);
            }
            
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Clear slot
        /// </summary>
        public bool ClearSlot(int slotIndex)
        {
            return SetItemAtSlot(slotIndex, new InventoryItem());
        }
        
        /// <summary>
        /// Check if slot is empty
        /// </summary>
        public bool IsSlotEmpty(int slotIndex)
        {
            return GetItemAtSlot(slotIndex).IsEmpty;
        }
        
        /// <summary>
        /// Find first empty slot
        /// </summary>
        public int FindEmptySlot()
        {
            for (int i = 0; i < maxSlots; i++)
            {
                if (IsSlotEmpty(i)) return i;
            }
            return -1;
        }
        
        /// <summary>
        /// Find all empty slots
        /// </summary>
        public int[] FindEmptySlots()
        {
            var emptySlots = new List<int>();
            for (int i = 0; i < maxSlots; i++)
            {
                if (IsSlotEmpty(i)) emptySlots.Add(i);
            }
            return emptySlots.ToArray();
        }
        
        #endregion
        
        #region Item Operations
        
        /// <summary>
        /// Add item to inventory (with stacking)
        /// </summary>
        public (bool success, int remainingQuantity) TryAddItem(InventoryItem item)
        {
            if (item == null || item.IsEmpty) return (false, 0);
            
            int remainingQuantity = item.Quantity;
            
            // First pass: try to stack with existing items
            for (int i = 0; i < maxSlots && remainingQuantity > 0; i++)
            {
                var slotItem = GetItemAtSlot(i);
                if (slotItem.IsEmpty || !slotItem.CanStackWith(item)) continue;
                
                int canStack = slotItem.GetRemainingStackSpace();
                if (canStack > 0)
                {
                    int toAdd = Mathf.Min(canStack, remainingQuantity);
                    slotItem.AddQuantity(toAdd);
                    remainingQuantity -= toAdd;
                    
                    OnItemQuantityChanged?.Invoke(slotItem, i, slotItem.Quantity);
                }
            }
            
            // Second pass: use empty slots for remaining quantity
            while (remainingQuantity > 0)
            {
                int emptySlot = FindEmptySlot();
                if (emptySlot == -1) break; // No more space
                
                var maxStack = item.GetMaxStackSize();
                int toPlace = Mathf.Min(remainingQuantity, maxStack);
                
                var newItem = item.Clone();
                newItem.Quantity = toPlace;
                SetItemAtSlot(emptySlot, newItem);
                
                remainingQuantity -= toPlace;
            }
            
            bool success = remainingQuantity < item.Quantity;
            if (success)
            {
                OnInventoryChanged?.Invoke();
                EventBus.Dispatch(new ItemAddedToInventoryEvent(ownerId, item, item.Quantity - remainingQuantity));
            }
            
            return (success, remainingQuantity);
        }
        
        /// <summary>
        /// Remove item from inventory
        /// </summary>
        public bool TryRemoveItem(string itemId, int quantity)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return false;
            
            int remainingToRemove = quantity;
            var slotsToModify = new List<(int slot, int removeAmount)>();
            
            // Find items to remove
            for (int i = 0; i < maxSlots && remainingToRemove > 0; i++)
            {
                var item = GetItemAtSlot(i);
                if (item.IsEmpty || item.ItemId != itemId) continue;
                
                int canRemove = Mathf.Min(item.Quantity, remainingToRemove);
                slotsToModify.Add((i, canRemove));
                remainingToRemove -= canRemove;
            }
            
            // Check if we can remove the requested amount
            if (remainingToRemove > 0) return false;
            
            // Actually remove items
            foreach (var (slot, removeAmount) in slotsToModify)
            {
                var item = GetItemAtSlot(slot);
                if (item.Quantity <= removeAmount)
                {
                    // Remove entire stack
                    ClearSlot(slot);
                }
                else
                {
                    // Reduce quantity
                    item.RemoveQuantity(removeAmount);
                    OnItemQuantityChanged?.Invoke(item, slot, item.Quantity);
                }
            }
            
            OnInventoryChanged?.Invoke();
            EventBus.Dispatch(new ItemRemovedFromInventoryEvent(ownerId, itemId, quantity));
            return true;
        }
        
        /// <summary>
        /// Remove specific item instance
        /// </summary>
        public bool TryRemoveItem(InventoryItem item, int quantity = -1)
        {
            if (item == null || item.IsEmpty) return false;
            
            int targetQuantity = quantity < 0 ? item.Quantity : quantity;
            return TryRemoveItem(item.ItemId, targetQuantity);
        }
        
        /// <summary>
        /// Move item from one slot to another
        /// </summary>
        public bool TryMoveItem(int fromSlot, int toSlot)
        {
            if (fromSlot < 0 || fromSlot >= maxSlots || toSlot < 0 || toSlot >= maxSlots)
                return false;
            
            if (fromSlot == toSlot) return true;
            
            var fromItem = GetItemAtSlot(fromSlot);
            var toItem = GetItemAtSlot(toSlot);
            
            if (fromItem.IsEmpty) return false;
            
            if (toItem.IsEmpty)
            {
                // Simple move
                SetItemAtSlot(toSlot, fromItem);
                ClearSlot(fromSlot);
            }
            else if (fromItem.CanStackWith(toItem))
            {
                // Stack merge
                if (toItem.TryMergeWith(fromItem))
                {
                    if (fromItem.IsEmpty)
                    {
                        ClearSlot(fromSlot);
                    }
                    OnItemQuantityChanged?.Invoke(toItem, toSlot, toItem.Quantity);
                    if (!fromItem.IsEmpty)
                    {
                        OnItemQuantityChanged?.Invoke(fromItem, fromSlot, fromItem.Quantity);
                    }
                }
                else
                {
                    return false;
                }
            }
            else
            {
                // Swap items
                SetItemAtSlot(fromSlot, toItem);
                SetItemAtSlot(toSlot, fromItem);
            }
            
            OnItemMoved?.Invoke(fromSlot, toSlot);
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        /// <summary>
        /// Split item stack
        /// </summary>
        public bool TrySplitStack(int slotIndex, int splitAmount)
        {
            if (slotIndex < 0 || slotIndex >= maxSlots || splitAmount <= 0)
                return false;
            
            var item = GetItemAtSlot(slotIndex);
            if (item.IsEmpty || splitAmount >= item.Quantity) return false;
            
            var splitItem = item.SplitStack(splitAmount);
            if (splitItem == null) return false;
            
            var result = TryAddItem(splitItem);
            if (result.success && result.remainingQuantity == 0)
            {
                OnItemQuantityChanged?.Invoke(item, slotIndex, item.Quantity);
                OnInventoryChanged?.Invoke();
                return true;
            }
            else
            {
                // Revert split if couldn't place new stack
                item.AddQuantity(splitAmount);
                return false;
            }
        }
        
        #endregion
        
        #region Queries
        
        /// <summary>
        /// Check if inventory contains item
        /// </summary>
        public bool HasItem(string itemId, int minQuantity = 1)
        {
            return GetItemQuantity(itemId) >= minQuantity;
        }
        
        /// <summary>
        /// Get total quantity of item
        /// </summary>
        public int GetItemQuantity(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            
            int total = 0;
            for (int i = 0; i < maxSlots; i++)
            {
                var item = GetItemAtSlot(i);
                if (!item.IsEmpty && item.ItemId == itemId)
                {
                    total += item.Quantity;
                }
            }
            return total;
        }
        
        /// <summary>
        /// Find all items by ID
        /// </summary>
        public (InventoryItem item, int slot)[] FindItems(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return Array.Empty<(InventoryItem, int)>();
            
            var results = new List<(InventoryItem, int)>();
            for (int i = 0; i < maxSlots; i++)
            {
                var item = GetItemAtSlot(i);
                if (!item.IsEmpty && item.ItemId == itemId)
                {
                    results.Add((item, i));
                }
            }
            return results.ToArray();
        }
        
        /// <summary>
        /// Find items by category
        /// </summary>
        public (InventoryItem item, int slot)[] FindItemsByCategory(ItemCategory category)
        {
            var results = new List<(InventoryItem, int)>();
            for (int i = 0; i < maxSlots; i++)
            {
                var item = GetItemAtSlot(i);
                if (!item.IsEmpty && item.Definition?.Category == category)
                {
                    results.Add((item, i));
                }
            }
            return results.ToArray();
        }
        
        /// <summary>
        /// Get all non-empty items
        /// </summary>
        public (InventoryItem item, int slot)[] GetAllItems()
        {
            var results = new List<(InventoryItem, int)>();
            for (int i = 0; i < maxSlots; i++)
            {
                var item = GetItemAtSlot(i);
                if (!item.IsEmpty)
                {
                    results.Add((item, i));
                }
            }
            return results.ToArray();
        }
        
        #endregion
        
        #region Validation & Cleanup
        
        /// <summary>
        /// Validate inventory integrity
        /// </summary>
        public bool ValidateInventory()
        {
            bool isValid = true;
            
            for (int i = 0; i < maxSlots; i++)
            {
                var item = GetItemAtSlot(i);
                if (!item.IsEmpty)
                {
                    if (!item.IsValidItem())
                    {
                        DebugUtils.LogWarning($"[InventoryCollection] Invalid item at slot {i}: {item}");
                        isValid = false;
                    }
                    
                    if (item.SlotIndex != i)
                    {
                        DebugUtils.LogWarning($"[InventoryCollection] Item slot index mismatch at {i}: expected {i}, got {item.SlotIndex}");
                        item.SlotIndex = i;
                    }
                }
            }
            
            return isValid;
        }
        
        /// <summary>
        /// Clean up inventory (remove invalid items, fix indices)
        /// </summary>
        public int CleanupInventory()
        {
            int removedCount = 0;
            
            for (int i = 0; i < maxSlots; i++)
            {
                var item = GetItemAtSlot(i);
                if (!item.IsEmpty)
                {
                    if (!item.IsValidItem())
                    {
                        ClearSlot(i);
                        removedCount++;
                    }
                    else
                    {
                        item.SlotIndex = i;
                    }
                }
            }
            
            if (removedCount > 0)
            {
                OnInventoryChanged?.Invoke();
                DebugUtils.Log($"[InventoryCollection] Cleaned up {removedCount} invalid items");
            }
            
            return removedCount;
        }
        
        /// <summary>
        /// Compact inventory (move all items to beginning)
        /// </summary>
        public void CompactInventory()
        {
            var allItems = GetAllItems().Select(x => x.item).ToList();
            
            // Clear all slots
            for (int i = 0; i < maxSlots; i++)
            {
                items[i] = new InventoryItem();
            }
            
            // Place items in order
            for (int i = 0; i < allItems.Count && i < maxSlots; i++)
            {
                SetItemAtSlot(i, allItems[i]);
            }
            
            OnInventoryChanged?.Invoke();
            DebugUtils.Log($"[InventoryCollection] Compacted inventory: {allItems.Count} items");
        }
        
        #endregion
        
        #region Serialization
        
        /// <summary>
        /// Get serializable data
        /// </summary>
        public SerializableInventoryData GetSerializableData()
        {
            var itemsData = new List<SerializableItemData>();
            
            for (int i = 0; i < maxSlots; i++)
            {
                var item = GetItemAtSlot(i);
                if (!item.IsEmpty)
                {
                    var itemData = item.GetSerializableData();
                    itemData.slotIndex = i;
                    itemsData.Add(itemData);
                }
            }
            
            return new SerializableInventoryData
            {
                ownerId = ownerId,
                maxSlots = maxSlots,
                items = itemsData.ToArray()
            };
        }
        
        /// <summary>
        /// Load from serializable data
        /// </summary>
        public void LoadFromSerializableData(SerializableInventoryData data)
        {
            if (data == null) return;
            
            ownerId = data.ownerId;
            maxSlots = data.maxSlots;
            
            InitializeSlots();
            
            if (data.items != null)
            {
                foreach (var itemData in data.items)
                {
                    if (itemData.slotIndex >= 0 && itemData.slotIndex < maxSlots)
                    {
                        var item = new InventoryItem();
                        item.LoadFromSerializableData(itemData);
                        SetItemAtSlot(itemData.slotIndex, item);
                    }
                }
            }
            
            DebugUtils.Log($"[InventoryCollection] Loaded inventory for {ownerId}: {UsedSlots}/{maxSlots} slots used");
        }
        
        #endregion
        
        #region Internal Helpers
        
        private void MarkLookupDirty()
        {
            lookupDirty = true;
        }
        
        private void EnsureLookup()
        {
            if (lookupDirty || slotLookup == null)
            {
                slotLookup = new Dictionary<int, InventoryItem>();
                for (int i = 0; i < items.Count; i++)
                {
                    if (!items[i].IsEmpty)
                    {
                        slotLookup[i] = items[i];
                    }
                }
                lookupDirty = false;
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Serializable inventory data
    /// </summary>
    [Serializable]
    public class SerializableInventoryData
    {
        public string ownerId;
        public int maxSlots;
        public SerializableItemData[] items;
    }
}
