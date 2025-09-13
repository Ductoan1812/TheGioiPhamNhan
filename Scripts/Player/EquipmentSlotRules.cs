using System;
using System.Collections.Generic;
using Xianxia.PlayerDataSystem; // InventoryItem definition

namespace Xianxia.Player
{
    /// <summary>
    /// Central mapping & validation for equipment slots.
    /// Avoids duplicated dictionaries across inventory/equipment/UI.
    /// Extend with more rules (two-hand, level requirement...) later.
    /// </summary>
    public static class EquipmentSlotRules
    {
        private static readonly Dictionary<string, Xianxia.Items.ItemCategory> _slotCategory = new(StringComparer.OrdinalIgnoreCase)
        {
            { "weapon_l", Xianxia.Items.ItemCategory.weapon },
            { "weapon_r", Xianxia.Items.ItemCategory.weapon },
            { "helmet",   Xianxia.Items.ItemCategory.helmet },
            { "armor",    Xianxia.Items.ItemCategory.armor  },
            { "ring_l",   Xianxia.Items.ItemCategory.accessory },
            { "ring_r",   Xianxia.Items.ItemCategory.accessory },
            { "body",     Xianxia.Items.ItemCategory.armor  },
            { "foot",     Xianxia.Items.ItemCategory.foot   },
            { "cloth",    Xianxia.Items.ItemCategory.cloth  },
            { "back",     Xianxia.Items.ItemCategory.back   },
            { "pet",      Xianxia.Items.ItemCategory.pet    },
        };

    public static bool IsKnownSlot(string slotId) => !string.IsNullOrEmpty(slotId) && _slotCategory.ContainsKey(slotId);

        public static bool IsValidForSlot(string slotId, InventoryItem item)
        {
            if (item == null || string.IsNullOrEmpty(slotId)) return false;
            return _slotCategory.TryGetValue(slotId, out var cat) && item.category == cat;
        }

        public static Xianxia.Items.ItemCategory? GetCategory(string slotId)
        {
            if (_slotCategory.TryGetValue(slotId, out var cat)) return cat;
            return null;
        }

        public static IEnumerable<string> AllSlots => _slotCategory.Keys;
    }
}
