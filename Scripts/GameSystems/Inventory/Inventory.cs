using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Foundation.Events;

namespace GameSystems.Inventory
{
    /// <summary>
    /// Item stack in inventory
    /// </summary>
    [Serializable]
    public class ItemStack
    {
        [SerializeField] private Item item;
        [SerializeField] private int quantity;

        public Item Item => item;
        public int Quantity => quantity;
        public bool IsEmpty => item == null || quantity <= 0;
        public bool IsFull => item != null && quantity >= item.MaxStackSize;

        public ItemStack(Item item, int quantity = 1)
        {
            this.item = item;
            this.quantity = Math.Max(0, quantity);
        }

        /// <summary>
        /// Add items to stack
        /// </summary>
        public int AddItems(int amount)
        {
            if (item == null) return amount;

            var maxCanAdd = item.MaxStackSize - quantity;
            var actualAdded = Math.Min(amount, maxCanAdd);
            quantity += actualAdded;
            
            return amount - actualAdded; // Return remaining amount
        }

        /// <summary>
        /// Remove items from stack
        /// </summary>
        public int RemoveItems(int amount)
        {
            var actualRemoved = Math.Min(amount, quantity);
            quantity -= actualRemoved;
            
            if (quantity <= 0)
            {
                item = null;
                quantity = 0;
            }
            
            return actualRemoved;
        }

        /// <summary>
        /// Split stack into two
        /// </summary>
        public ItemStack Split(int splitAmount)
        {
            if (splitAmount >= quantity) return null;

            var splitStack = new ItemStack(item.Clone(), splitAmount);
            quantity -= splitAmount;
            
            return splitStack;
        }

        public ItemStack Clone()
        {
            return new ItemStack(item?.Clone(), quantity);
        }
    }

    /// <summary>
    /// Main inventory system
    /// </summary>
    [Serializable]
    public class Inventory
    {
        [SerializeField] private List<ItemStack> slots;
        [SerializeField] private int capacity;

        public int Capacity => capacity;
        public int UsedSlots => slots.Count(slot => !slot.IsEmpty);
        public int FreeSlots => capacity - UsedSlots;
        public bool IsFull => FreeSlots <= 0;

        // Events
        public event Action<Item, int> OnItemAdded;
        public event Action<Item, int> OnItemRemoved;
        public event Action OnInventoryChanged;

        public Inventory(int capacity)
        {
            this.capacity = capacity;
            this.slots = new List<ItemStack>(capacity);
            
            // Initialize empty slots
            for (int i = 0; i < capacity; i++)
            {
                slots.Add(new ItemStack(null, 0));
            }
        }

        /// <summary>
        /// Add item to inventory
        /// </summary>
        public AddItemResult AddItem(Item item, int quantity = 1)
        {
            if (item == null || quantity <= 0)
                return new AddItemResult(false, 0, "Invalid item or quantity");

            var remainingQuantity = quantity;

            // First, try to add to existing stacks
            if (item.IsStackable)
            {
                for (int i = 0; i < slots.Count && remainingQuantity > 0; i++)
                {
                    var slot = slots[i];
                    if (!slot.IsEmpty && slot.Item.CanStackWith(item))
                    {
                        var remaining = slot.AddItems(remainingQuantity);
                        var added = remainingQuantity - remaining;
                        remainingQuantity = remaining;
                        
                        if (added > 0)
                        {
                            OnItemAdded?.Invoke(item, added);
                        }
                    }
                }
            }

            // Then, try to add to empty slots
            for (int i = 0; i < slots.Count && remainingQuantity > 0; i++)
            {
                var slot = slots[i];
                if (slot.IsEmpty)
                {
                    var amountToAdd = Math.Min(remainingQuantity, item.MaxStackSize);
                    slots[i] = new ItemStack(item.Clone(), amountToAdd);
                    remainingQuantity -= amountToAdd;
                    
                    OnItemAdded?.Invoke(item, amountToAdd);
                }
            }

            var totalAdded = quantity - remainingQuantity;
            if (totalAdded > 0)
            {
                OnInventoryChanged?.Invoke();
                EventBus.Publish(new InventoryChangedEvent(this));
            }

            return new AddItemResult(
                remainingQuantity == 0, 
                totalAdded, 
                remainingQuantity > 0 ? "Inventory full" : "Success"
            );
        }

        /// <summary>
        /// Remove item from inventory
        /// </summary>
        public RemoveItemResult RemoveItem(string itemId, int quantity = 1)
        {
            var totalRemoved = 0;
            var remainingToRemove = quantity;

            for (int i = 0; i < slots.Count && remainingToRemove > 0; i++)
            {
                var slot = slots[i];
                if (!slot.IsEmpty && slot.Item.Id == itemId)
                {
                    var removed = slot.RemoveItems(remainingToRemove);
                    totalRemoved += removed;
                    remainingToRemove -= removed;
                    
                    if (removed > 0)
                    {
                        OnItemRemoved?.Invoke(slot.Item, removed);
                    }
                }
            }

            if (totalRemoved > 0)
            {
                OnInventoryChanged?.Invoke();
                EventBus.Publish(new InventoryChangedEvent(this));
            }

            return new RemoveItemResult(
                remainingToRemove == 0,
                totalRemoved,
                remainingToRemove > 0 ? "Not enough items" : "Success"
            );
        }

        /// <summary>
        /// Get item count
        /// </summary>
        public int GetItemCount(string itemId)
        {
            return slots.Where(slot => !slot.IsEmpty && slot.Item.Id == itemId)
                       .Sum(slot => slot.Quantity);
        }

        /// <summary>
        /// Check if inventory contains item
        /// </summary>
        public bool HasItem(string itemId, int quantity = 1)
        {
            return GetItemCount(itemId) >= quantity;
        }

        /// <summary>
        /// Get item at specific slot
        /// </summary>
        public ItemStack GetSlot(int index)
        {
            return index >= 0 && index < slots.Count ? slots[index] : null;
        }

        /// <summary>
        /// Get all items of specific type
        /// </summary>
        public List<ItemStack> GetItemsByType(ItemType itemType)
        {
            return slots.Where(slot => !slot.IsEmpty && slot.Item.Type == itemType).ToList();
        }

        /// <summary>
        /// Clear inventory
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i] = new ItemStack(null, 0);
            }
            
            OnInventoryChanged?.Invoke();
            EventBus.Publish(new InventoryChangedEvent(this));
        }

        /// <summary>
        /// Get all non-empty slots
        /// </summary>
        public List<ItemStack> GetAllItems()
        {
            return slots.Where(slot => !slot.IsEmpty).ToList();
        }
    }

    /// <summary>
    /// Result classes for inventory operations
    /// </summary>
    public class AddItemResult
    {
        public bool Success { get; }
        public int AmountAdded { get; }
        public string Message { get; }

        public AddItemResult(bool success, int amountAdded, string message)
        {
            Success = success;
            AmountAdded = amountAdded;
            Message = message;
        }
    }

    public class RemoveItemResult
    {
        public bool Success { get; }
        public int AmountRemoved { get; }
        public string Message { get; }

        public RemoveItemResult(bool success, int amountRemoved, string message)
        {
            Success = success;
            AmountRemoved = amountRemoved;
            Message = message;
        }
    }

    /// <summary>
    /// Inventory changed event
    /// </summary>
    public class InventoryChangedEvent : GameEvent<Inventory>
    {
        public InventoryChangedEvent(Inventory inventory) : base(inventory) { }
    }
}
