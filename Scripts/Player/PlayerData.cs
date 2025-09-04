using System;
using System.Collections.Generic;

namespace Xianxia.PlayerDataSystem
{
    [Serializable]
    public class PlayerInventoryAffix
    {
        public string id;
        public float value;
        public int tier;
    }

    [Serializable]
    public class PlayerInventoryItem
    {
        public string id;
        public int Slot; // 1-based theo JSON
        public int quantity = 1;

        public int level;
        public PlayerInventoryAffix[] affixes = Array.Empty<PlayerInventoryAffix>();
    }

    [Serializable]
    public class PlayerData
    {
        public int InventorySize = 30;
        public List<PlayerInventoryItem> inventory = new List<PlayerInventoryItem>();

        public PlayerInventoryItem GetBySlot(int slot)
        {
            return inventory.Find(x => x.Slot == slot);
        }

        public void SetSlot(PlayerInventoryItem entry)
        {
            if (entry == null) return;
            var idx = inventory.FindIndex(x => x.Slot == entry.Slot);
            if (idx >= 0) inventory[idx] = entry;
            else inventory.Add(entry);
        }

        public void RemoveSlot(int slot)
        {
            inventory.RemoveAll(x => x.Slot == slot);
        }
    }
}