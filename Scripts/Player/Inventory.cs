using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Inventory
{
    public List<InventorySlot> slots = new List<InventorySlot>();

    // Fired whenever the inventory content changes (add, swap/merge, clear index, etc.)
    public event Action OnInventoryChanged;

    private void NotifyChanged()
    {
        OnInventoryChanged?.Invoke();
    }

    public void AddItem(string itemId, int amount)
    {
        if (string.IsNullOrEmpty(itemId) || amount <= 0) return;
        var slot = slots.Find(s => s.itemId == itemId);
        if (slot != null)
        {
            slot.amount += amount;
        }
        else
        {
            slots.Add(new InventorySlot(itemId, amount));
        }

    // Notify listeners (e.g., UI) that inventory changed
    NotifyChanged();
    }

    public void EnsureSize(int size)
    {
        if (size <= 0) return;
        while (slots.Count < size)
        {
            slots.Add(new InventorySlot(null, 0));
        }
    }

    public bool SwapOrMergeByIndex(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex) return false;
        int max = Mathf.Max(fromIndex, toIndex) + 1;
        EnsureSize(max);
        if (fromIndex < 0 || toIndex < 0 || fromIndex >= slots.Count || toIndex >= slots.Count) return false;

        var from = slots[fromIndex];
        var to = slots[toIndex];

        bool fromEmpty = string.IsNullOrEmpty(from.itemId) || from.amount <= 0;
        bool toEmpty = string.IsNullOrEmpty(to.itemId) || to.amount <= 0;
        if (fromEmpty) return false;

        if (toEmpty)
        {
            // Move
            to.itemId = from.itemId;
            to.amount = from.amount;
            from.itemId = null;
            from.amount = 0;
            NotifyChanged();
            return true;
        }

        if (from.itemId == to.itemId)
        {
            // Merge stacks
            to.amount += from.amount;
            from.itemId = null;
            from.amount = 0;
            NotifyChanged();
            return true;
        }

        // Swap
        var tmpId = to.itemId;
        var tmpAmt = to.amount;
        to.itemId = from.itemId;
        to.amount = from.amount;
        from.itemId = tmpId;
        from.amount = tmpAmt;
        NotifyChanged();
        return true;
    }

    public void ClearIndex(int index)
    {
        if (index < 0) return;
        EnsureSize(index + 1);
        slots[index].itemId = null;
        slots[index].amount = 0;
    NotifyChanged();
    }
}

[System.Serializable]
public class InventorySlot
{
    public string itemId;
    public int amount;

    public InventorySlot(string id, int amt)
    {
        itemId = id;
        amount = amt;
    }
}