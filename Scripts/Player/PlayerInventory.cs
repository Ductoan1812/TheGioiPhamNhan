using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
// using System.Reflection; // legacy static-slot reflection removed
using UnityEngine;
using Xianxia.Items;
using Xianxia.Player;
using Xianxia.PlayerDataSystem;

/// Quản lý Inventory: thêm/xóa item dựa trên PlayerManager.Data và preload icon/texture cho UI.
public class PlayerInventory : MonoBehaviour
{
    public static PlayerInventory Instance { get; private set; }

    private List<InventoryItem> inventory;
    private EquipmentData equipment;
    private int maxSlots = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }
    private void OnEnable()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerDataLoaded += HandlePlayerDataLoaded;
            if (PlayerManager.Instance.Data != null)
            {
                HandlePlayerDataLoaded(PlayerManager.Instance.Data);
            }
        }
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerDataLoaded -= HandlePlayerDataLoaded;
        }
    }

    private void HandlePlayerDataLoaded(object playerData)
    {
        var data = playerData as PlayerData;
        if (data == null)
        {
            Debug.LogWarning("[PlayerInventory] failed to load player data");
            return;
        }
        maxSlots = data.InventorySize;
        inventory = data.inventory;
        equipment = data.equipment;
    }

    public bool AddItem(InventoryItem item)
    {
        if (item.quantity <= 0) return false;
        var data = PlayerManager.Instance?.Data;
        if (data == null)
        {
            Debug.LogWarning("[PlayerInventory] No player data");
            return false;
        }

        int remaining = item.quantity;
        int maxStack = item.maxStack > 0 ? item.maxStack : int.MaxValue;
        foreach (var exist in inventory)
        {
            if (IsSameItem(exist, item) && exist.quantity < exist.maxStack)
            {
                int canAdd = Math.Min(exist.maxStack - exist.quantity, remaining);
                exist.quantity += canAdd;
                remaining -= canAdd;
                if (remaining <= 0) break;
            }
        }
        while (remaining > 0)
        {
            int slot = GetEmptySlot();
            if (slot == -1)
            {
                Debug.LogWarning("[PlayerInventory] Max slot reached, cannot add item");
                break;
            }
            int stackAmount = Math.Min(maxStack, remaining);
            var newItem = item;
            newItem.quantity = stackAmount;
            newItem.Slot = slot;
            inventory.Add(newItem);
            remaining -= stackAmount;
        }
        SaveInventory();
        return remaining == 0;
    }

    public bool RemoveItem(InventoryItem item, int quantity)
    {
        if (quantity <= 0) return false;
        var data = PlayerManager.Instance?.Data;
        if (data == null)
        {
            Debug.LogWarning("[PlayerInventory] No player data");
            return false;
        }
        if (inventory == null || inventory.Count == 0)
        {
            Debug.LogWarning("[PlayerInventory] Inventory empty");
            return false;
        }
        var slots = inventory.Where(x => IsSameItem(x, item)).ToList();
        if (slots.Count == 0) return false;

        int remaining = quantity;
        foreach (var slot in slots)
        {
            if (remaining <= 0) break;
            if (slot.quantity > remaining)
            {
                slot.quantity -= remaining;
                remaining = 0;
            }
            else
            {
                remaining -= slot.quantity;
                slot.quantity = 0;
            }
        }
        inventory.RemoveAll(x => x.quantity == 0);
        SaveInventory();
        return remaining == 0;
    }
    public void EquipItem(InventoryItem item, string equipSlot)
    {
        if (item == null) return;
        if (equipment == null)
        {
            Debug.LogWarning("[PlayerInventory] No equipment data");
            return;
        }
        var data = PlayerManager.Instance?.Data;
        if (data == null)
        {
            Debug.LogWarning("[PlayerInventory] No Inventory data");
            return;
        }
        var slotCategory = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "weapon_l", "weapon" },
            { "weapon_r", "weapon" },
            { "helmet",   "helmet" },
            { "armor",    "armor"  },
            { "ring_l",   "accessory" },
            { "ring_r",   "accessory" },
            { "body",     "body"   },
            { "foot",     "foot"   },
            { "cloth",    "cloth"  },
            { "back",     "back"   },
            { "pet",      "pet"    },
        };
        if (!slotCategory.TryGetValue(equipSlot, out var validCategory))
        {
            Debug.LogWarning($"[PlayerInventory] Unknown equip slot '{equipSlot}'");
            return;
        }

        if (item.category.ToString().ToLower() != validCategory)
        {
            Debug.LogWarning($"[PlayerInventory] Item category '{item.category}' does not match equip slot '{equipSlot}'");
            return;
        }
        string slotId = equipSlot?.ToLowerInvariant();

        var oldItem = equipment.Unequip(slotId);
        if (oldItem != null) AddItem(oldItem);

        bool ok = equipment.Equip(slotId, item, overwrite: true);
        if (!ok)
        {
            if (oldItem != null) equipment.Equip(slotId, oldItem, overwrite: true);
            Debug.LogWarning($"[PlayerInventory] Equip failed for slot '{slotId}'");
            return;
        }

        RemoveItem(item, 1);
        SaveInventory();
        Debug.Log($"[PlayerInventory] Equipped {item.id} to {slotId} and saved.");

        var eqUi = FindFirstObjectByType<EquipmentUIManager>();
        if (eqUi != null) eqUi.UpdateSlotUI(slotId, item);
        var eqVisual = FindFirstObjectByType<Xianxia.Player.PlayerEquitment>();
        if (eqVisual != null) eqVisual.RefreshSlotVisual(slotId, item);
    }
    public void UnEquipItem(string equipSlot)
    {
        if (equipment == null)
        {
            Debug.LogWarning("[PlayerInventory] No equipment data");
            return;
        }
        var data = PlayerManager.Instance?.Data;
        if (data == null)
        {
            Debug.LogWarning("[PlayerInventory] No Inventory data");
            return;
        }
        string slotId = equipSlot?.ToLowerInvariant();
        var oldItem = equipment.Unequip(slotId);
        if (oldItem != null)
        {
            AddItem(oldItem);
            SaveInventory();
            Debug.Log($"[PlayerInventory] Unequipped {oldItem.id} from {slotId} and saved.");
            var eqUi = FindFirstObjectByType<EquipmentUIManager>();
            if (eqUi != null) eqUi.UpdateSlotUI(slotId, null);
            var eqVisual = FindFirstObjectByType<Xianxia.Player.PlayerEquitment>();
            if (eqVisual != null) eqVisual.RefreshSlotVisual(slotId, null);
        }
    }
    //========================= hàm tiên ích ========================
    public bool IsSameItem(InventoryItem a, InventoryItem b)
    {
        if (a == null || b == null) return false;
        return a.id == b.id
            && a.addressIcon == b.addressIcon
            && a.addressTexture == b.addressTexture
            && a.name == b.name
            && a.category == b.category
            && a.rarity == b.rarity
            && a.element == b.element
            && a.realmRequirement == b.realmRequirement
            && a.bindType == b.bindType
            && a.level == b.level
            && a.stackSize == b.stackSize
            && a.maxStack == b.maxStack
            && a.baseStats == b.baseStats
            && a.sockets == b.sockets
            && a.affixes == b.affixes
            && a.useEffect == b.useEffect
            && a.flavor == b.flavor;
    }
    public int GetEmptySlot()
    {
        HashSet<int> usedSlots = new HashSet<int>(inventory.Select(item => item.Slot));
        for (int i = 0; i < maxSlots; i++)
        {
            if (!usedSlots.Contains(i))
                return i;
        }
        return -1;
    }
    private void SaveInventory()
    {
        var data = PlayerManager.Instance?.Data;
        if (data == null) { Debug.LogWarning("[PlayerInventory] No player data"); return; }
        data.inventory = inventory;
        data.equipment = equipment;
        PlayerManager.Instance.SavePlayer();
    }
}