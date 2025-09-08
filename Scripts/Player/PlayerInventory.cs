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
    // Tạo bản sao item với quantity=1 để lưu ở ô trang bị (không dùng chung reference với stack trong túi)
    private InventoryItem CloneAsSingle(InventoryItem src)
    {
        if (src == null) return null;
        return new InventoryItem
        {
            id = src.id,
            addressIcon = src.addressIcon,
            addressTexture = src.addressTexture,
            name = src.name,
            category = src.category,
            rarity = src.rarity,
            element = src.element,
            realmRequirement = src.realmRequirement,
            bindType = src.bindType,
            level = src.level,
            stackSize = src.stackSize,
            maxStack = src.maxStack,
            baseStats = src.baseStats,
            sockets = src.sockets,
            affixes = src.affixes,
            useEffect = src.useEffect,
            flavor = src.flavor,
            quantity = 1,
            Slot = -1,
        };
    }

    // Cố gắng đặt item vào chỉ số slot cụ thể trong túi
    private bool TryAddItemAtIndex(InventoryItem item, int index)
    {
        if (item == null) return false;
        if (index < 0 || index >= maxSlots) return false;
        // Tìm item đang ở slot index (nếu có)
        var existing = inventory.FirstOrDefault(x => x.Slot == index);
        if (existing == null)
        {
            item.Slot = index;
            inventory.Add(item);
            return true;
        }
        // Nếu cùng loại và còn chỗ thì gộp
        if (IsSameItem(existing, item) && existing.quantity < existing.maxStack)
        {
            int canAdd = Math.Max(0, existing.maxStack - existing.quantity);
            int move = Math.Min(canAdd, item.quantity);
            existing.quantity += move;
            item.quantity -= move;
            // Nếu item hết sau khi gộp thì coi như thành công
            return item.quantity == 0;
        }
        return false;
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

        // Luôn tạo bản sao số lượng 1 để trang bị
        var single = CloneAsSingle(item);

        // Nếu slot đã có item: tháo ra trước và trả về túi
        var oldItem = equipment.Unequip(slotId);
        if (oldItem != null)
        {
            // Đảm bảo oldItem.quantity = 1
            oldItem.quantity = 1;
            AddItem(oldItem);
        }

        bool ok = equipment.Equip(slotId, single, overwrite: true);
        if (!ok)
        {
            if (oldItem != null) equipment.Equip(slotId, oldItem, overwrite: true);
            Debug.LogWarning($"[PlayerInventory] Equip failed for slot '{slotId}'");
            return;
        }

        // Trừ 1 từ stack của inventory gốc
        RemoveItem(item, 1);
        SaveInventory();
        Debug.Log($"[PlayerInventory] Equipped {single.id} (x1) to {slotId} and saved.");

        var invUi = FindFirstObjectByType<InventoryUIManager>();
        if (invUi != null) invUi.RefreshFromCurrentData();
        var eqUi = FindFirstObjectByType<EquipmentUIManager>();
        if (eqUi != null) eqUi.UpdateSlotUI(slotId, single);
        var eqVisual = FindFirstObjectByType<Xianxia.Player.PlayerEquitment>();
        if (eqVisual != null) eqVisual.RefreshSlotVisual(slotId, single);
    }

    public void UnEquipItem(string equipSlot)
    {
        UnEquipItem(equipSlot, null);
    }

    public void UnEquipItem(string equipSlot, int? targetIndex)
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
            // Bảo toàn quy tắc: item từ trang bị luôn số lượng 1
            oldItem.quantity = 1;
            bool placed = false;
            if (targetIndex.HasValue)
            {
                var clone = CloneAsSingle(oldItem);
                placed = TryAddItemAtIndex(clone, targetIndex.Value);
                if (placed == false)
                {
                    // Nếu không đặt được đúng ô, thêm theo quy tắc bình thường
                    AddItem(clone);
                    placed = true;
                }
            }
            else
            {
                AddItem(oldItem);
                placed = true;
            }

            if (placed)
            {
                SaveInventory();
                Debug.Log($"[PlayerInventory] Unequipped {oldItem.id} (x1) from {slotId} and saved.");
                var invUi = FindFirstObjectByType<InventoryUIManager>();
                if (invUi != null) invUi.RefreshFromCurrentData();
                var eqUi = FindFirstObjectByType<EquipmentUIManager>();
                if (eqUi != null) eqUi.UpdateSlotUI(slotId, null);
                var eqVisual = FindFirstObjectByType<Xianxia.Player.PlayerEquitment>();
                if (eqVisual != null) eqVisual.RefreshSlotVisual(slotId, null);
            }
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