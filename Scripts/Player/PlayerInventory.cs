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
    private const int DefaultInventorySize = 30;

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
        if (data.InventorySize <= 0)
        {
            Debug.LogWarning($"[PlayerInventory] Invalid InventorySize={data.InventorySize}. Auto-correcting to {DefaultInventorySize}.");
            data.InventorySize = DefaultInventorySize;
            // Persist fix so it doesn't recur
            PlayerManager.Instance?.SavePlayer();
        }
        maxSlots = data.InventorySize;
        // Clean corrupted/empty entries (only keep entries that truly have an item)
        inventory = (data.inventory ?? new List<InventoryItem>())
            .Where(HasRealItem)
            .ToList();
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
            if (!HasRealItem(exist)) continue;
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
            var newItem = new InventoryItem
            {
                id = item.id,
                addressIcon = item.addressIcon,
                addressTexture = item.addressTexture,
                name = item.name,
                category = item.category,
                rarity = item.rarity,
                element = item.element,
                realmRequirement = item.realmRequirement,
                bindType = item.bindType,
                level = item.level,
                maxStack = item.maxStack,
                baseStats = item.baseStats,
                sockets = item.sockets,
                affixes = item.affixes,
                useEffect = item.useEffect,
                flavor = item.flavor,
                quantity = stackAmount,
                Slot = slot,
            };
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
        int capacity = maxSlots > 0 ? maxSlots : Math.Max(1, PlayerManager.Instance?.Data?.InventorySize ?? 0);
        if (capacity <= 0) capacity = DefaultInventorySize;
        if (maxSlots <= 0) maxSlots = capacity; // cache for future calls
        if (index < 0 || index >= capacity) return false;
        // Tìm item đang ở slot index (nếu có)
        var existing = inventory.FirstOrDefault(x => x.Slot == index);
        if (existing == null || !HasRealItem(existing))
        {
            // Xóa mọi entry rác ở slot này trước khi đặt
            inventory.RemoveAll(x => x.Slot == index && !HasRealItem(x));
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
                if (!placed)
                {
                    placed = AddItem(clone);
                }
            }
            else
            {
                placed = AddItem(oldItem);
            }

            if (!placed)
            {
                // Túi đầy -> thả vật phẩm ra thế giới tại vị trí người chơi
                bool dropped = TryDropUnequipped(oldItem, slotId);
                if (!dropped)
                {
                    // Không thể drop (thiếu ItemDropManager). Thử re-equip để không mất item.
                    Debug.LogWarning($"[PlayerInventory] Inventory full and no ItemDropManager. Re-equipping {oldItem.id} back to {slotId}.");
                    equipment.Equip(slotId, oldItem, overwrite: true);
                    return;
                }
            }

            // Save + refresh UI/visuals (đã unequip khỏi slot)
            SaveInventory();
            Debug.Log($"[PlayerInventory] Unequipped {oldItem.id} (x1) from {slotId}. {(placed ? "Added to inventory" : "Dropped to world")} and saved.");
            var invUi = FindFirstObjectByType<InventoryUIManager>();
            if (invUi != null) invUi.RefreshFromCurrentData();
            var eqUi = FindFirstObjectByType<EquipmentUIManager>();
            if (eqUi != null) eqUi.UpdateSlotUI(slotId, null);
            var eqVisual = FindFirstObjectByType<Xianxia.Player.PlayerEquitment>();
            if (eqVisual != null) eqVisual.RefreshSlotVisual(slotId, null);
        }
    }

    // Kéo từ ô trang bị ra ngoài -> drop vật phẩm ra thế giới, không trả về túi
    public bool DropEquippedItem(string equipSlot)
    {
        if (equipment == null) return false;
        string slotId = equipSlot?.ToLowerInvariant();
        equipment.TryGet(slotId, out var itm);
        if (itm == null) return false;
        // giữ nguyên trên slot cho tới khi drop thành công
        var clone = CloneAsSingle(itm);
        bool dropped = TryDropUnequipped(clone, slotId);
        if (!dropped)
        {
            Debug.LogWarning("[PlayerInventory] Failed to drop equipped item; keeping it equipped.");
            return false;
        }
        // Xoá khỏi slot trang bị sau khi đã drop
        var removed = equipment.Unequip(slotId);
        if (removed == null)
        {
            Debug.LogWarning("[PlayerInventory] DropEquippedItem: could not unequip after drop");
        }
        SaveInventory();
        // làm mới UI/visual
        var eqUi = FindFirstObjectByType<EquipmentUIManager>();
        if (eqUi != null) eqUi.UpdateSlotUI(slotId, null);
        var eqVisual = FindFirstObjectByType<Xianxia.Player.PlayerEquitment>();
        if (eqVisual != null) eqVisual.RefreshSlotVisual(slotId, null);
        return true;
    }

    // Move or swap items between two equipment slots without modifying inventory stacks
    public bool MoveEquipment(string fromSlot, string toSlot)
    {
        if (equipment == null)
        {
            Debug.LogWarning("[PlayerInventory] No equipment data");
            return false;
        }
        if (string.IsNullOrEmpty(fromSlot) || string.IsNullOrEmpty(toSlot)) return false;
        fromSlot = fromSlot.ToLowerInvariant();
        toSlot = toSlot.ToLowerInvariant();
        if (fromSlot == toSlot) return true;

        // Map of equip slot -> allowed category
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

        if (!slotCategory.ContainsKey(fromSlot) || !slotCategory.ContainsKey(toSlot))
        {
            Debug.LogWarning($"[PlayerInventory] Unknown slot(s): '{fromSlot}' or '{toSlot}'");
            return false;
        }

        // Read current items
        equipment.TryGet(fromSlot, out var fromItem);
        equipment.TryGet(toSlot, out var toItem);
        if (fromItem == null)
        {
            // nothing to move
            return false;
        }

        string catFrom = fromItem.category.ToString().ToLowerInvariant();
        if (!string.Equals(catFrom, slotCategory[toSlot], StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogWarning($"[PlayerInventory] Cannot move item '{fromItem.name}' ({catFrom}) to slot '{toSlot}' (expects {slotCategory[toSlot]})");
            return false;
        }
        if (toItem != null)
        {
            string catTo = toItem.category.ToString().ToLowerInvariant();
            if (!string.Equals(catTo, slotCategory[fromSlot], StringComparison.OrdinalIgnoreCase))
            {
                Debug.LogWarning($"[PlayerInventory] Cannot swap: target item '{toItem.name}' ({catTo}) incompatible with slot '{fromSlot}' (expects {slotCategory[fromSlot]})");
                return false;
            }
        }

        // Perform move/swap in equipment only (no inventory change), keep original references
        bool setTo = equipment.Equip(toSlot, fromItem, overwrite: true);
        bool setFrom = equipment.Equip(fromSlot, toItem, overwrite: true);
        if (!setTo || !setFrom)
        {
            Debug.LogWarning("[PlayerInventory] MoveEquipment failed to set slots");
            return false;
        }

        // Persist and refresh relevant UIs/visuals
        SaveInventory();
        var eqUi = FindFirstObjectByType<EquipmentUIManager>();
        if (eqUi != null)
        {
            eqUi.UpdateSlotUI(fromSlot, toItem);
            eqUi.UpdateSlotUI(toSlot, fromItem);
        }
        var eqVisual = FindFirstObjectByType<Xianxia.Player.PlayerEquitment>();
        if (eqVisual != null)
        {
            eqVisual.RefreshSlotVisual(fromSlot, toItem);
            eqVisual.RefreshSlotVisual(toSlot, fromItem);
        }
        return true;
    }
    //========================= hàm tiên ích ========================
    // So sánh để gộp stack/nhận biết cùng loại: chỉ dựa trên id định danh.
    // Lưu ý: số lượng runtime dùng InventoryItem.quantity; ItemData.stackSize chỉ là mặc định khi spawn.
    public bool IsSameItem(InventoryItem a, InventoryItem b)
    {
        if (a == null || b == null) return false;
        return a.id == b.id;
    }

    // Sử dụng vật phẩm tiêu hao: trừ số lượng và (TODO) áp dụng hiệu ứng
    public bool UseItem(InventoryItem item, int quantity)
    {
        if (item == null || quantity <= 0) return false;
        // Giảm từ đúng stack đang mở trong UI (theo Slot), nếu không có thì theo id
        var slotEntry = inventory?.FirstOrDefault(x => x.Slot == item.Slot && IsSameItem(x, item));
        if (slotEntry == null)
            slotEntry = inventory?.FirstOrDefault(x => IsSameItem(x, item));
        if (slotEntry == null || slotEntry.quantity <= 0) return false;

        int remove = Mathf.Min(quantity, slotEntry.quantity);
        slotEntry.quantity -= remove;
        if (slotEntry.quantity <= 0)
            inventory.Remove(slotEntry);

        // TODO: Áp dụng useEffect của item (buff, heal...), hiện để trống
        SaveInventory();

        // Refresh UI
        var invUi = FindFirstObjectByType<InventoryUIManager>();
        invUi?.RefreshFromCurrentData();
        return true;
    }

    // Tách stack: tạo một stack mới với số lượng chỉ định từ stack hiện tại
    public bool SplitStack(InventoryItem item, int splitQuantity)
    {
        if (item == null || splitQuantity <= 0) return false;
        var slotEntry = inventory?.FirstOrDefault(x => x.Slot == item.Slot && IsSameItem(x, item));
        if (slotEntry == null)
            slotEntry = inventory?.FirstOrDefault(x => IsSameItem(x, item));
        if (slotEntry == null) return false;
        if (splitQuantity >= slotEntry.quantity) return false; // phải chừa lại ít nhất 1

        int empty = GetEmptySlot();
        if (empty == -1)
        {
            Debug.LogWarning("[PlayerInventory] No empty slot to split stack");
            return false;
        }
        slotEntry.quantity -= splitQuantity;
        var newItem = new InventoryItem
        {
            id = slotEntry.id,
            addressIcon = slotEntry.addressIcon,
            addressTexture = slotEntry.addressTexture,
            name = slotEntry.name,
            category = slotEntry.category,
            rarity = slotEntry.rarity,
            element = slotEntry.element,
            realmRequirement = slotEntry.realmRequirement,
            bindType = slotEntry.bindType,
            level = slotEntry.level,
            maxStack = slotEntry.maxStack,
            baseStats = slotEntry.baseStats,
            sockets = slotEntry.sockets,
            affixes = slotEntry.affixes,
            useEffect = slotEntry.useEffect,
            flavor = slotEntry.flavor,
            quantity = splitQuantity,
            Slot = empty,
        };
        inventory.Add(newItem);
        SaveInventory();

        var invUi = FindFirstObjectByType<InventoryUIManager>();
        invUi?.RefreshFromCurrentData();
        return true;
    }
    public int GetEmptySlot()
    {
        int capacity = maxSlots > 0 ? maxSlots : Math.Max(1, PlayerManager.Instance?.Data?.InventorySize ?? 0);
        if (capacity <= 0) capacity = DefaultInventorySize;
        if (maxSlots <= 0) maxSlots = capacity; // cache the resolved capacity

        var usedSlots = new HashSet<int>(
            inventory != null 
                ? inventory.Where(HasRealItem).Select(it => it.Slot) 
                : System.Linq.Enumerable.Empty<int>()
        );
        // Debug ra các slot đã dùng
        Debug.Log("Used slots: " + string.Join(", ", usedSlots));

        for (int i = 0; i < capacity; i++)
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
        // Chỉ lưu những entry thật sự có item
        inventory = (inventory ?? new List<InventoryItem>()).Where(HasRealItem).ToList();
        data.inventory = inventory;
        data.equipment = equipment;
        PlayerManager.Instance.SavePlayer();
    }

    // Một entry có item thật sự: id hợp lệ và quantity > 0
    private bool HasRealItem(InventoryItem it)
    {
        return it != null && !string.IsNullOrEmpty(it.id) && it.quantity > 0;
    }

    // Thử thả vật phẩm unequip ra thế giới nếu túi đầy
    private bool TryDropUnequipped(InventoryItem item, string fromSlot)
    {
        if (item == null) return false;
        // Tìm vị trí người chơi: ưu tiên Camera.main (center), fallback vị trí của PlayerInventory GameObject
        Vector3 pos;
        if (Camera.main != null)
            pos = Camera.main.transform.position + Camera.main.transform.forward * 1.5f;
        else
            pos = this.transform.position;

        var dropMgr = FindFirstObjectByType<ItemDropManager>();
        if (dropMgr == null)
        {
            Debug.LogWarning("[PlayerInventory] No ItemDropManager found in scene, cannot drop unequipped item.");
            return false;
        }
        // Dùng clone x1 để rơi
        var dropItem = CloneAsSingle(item);
        dropMgr.Spawn(dropItem, pos + Vector3.right * 0.5f);
        Debug.Log($"[PlayerInventory] Dropped unequipped item {item.id} from {fromSlot} at {pos}");
        return true;
    }

    // Kéo item từ inventory ra ngoài -> drop ra thế giới và trừ số lượng trong túi
    public bool DropItemFromInventory(InventoryItem item, int quantity, int sourceSlotIndex)
    {
        if (item == null || quantity <= 0) return false;
        if (inventory == null || inventory.Count == 0) return false;

        // Tìm đúng entry tại slot chỉ định nếu có, để trừ chính xác
        InventoryItem slotEntry = inventory.FirstOrDefault(x => x.Slot == sourceSlotIndex && HasRealItem(x));
        if (slotEntry == null)
        {
            // fallback: tìm theo id nếu không có entry đúng index (phòng khi UI chưa sync)
            slotEntry = inventory.FirstOrDefault(x => IsSameItem(x, item) && HasRealItem(x));
        }
        if (slotEntry == null) return false;

        int remove = Mathf.Min(quantity, slotEntry.quantity);
        // Tạo bản sao x1 để drop (nếu remove > 1, thả nhiều lần hoặc gom quantity tùy ItemDropManager)
        for (int i = 0; i < remove; i++)
        {
            var one = CloneAsSingle(slotEntry);
            // dùng fromSlot = $"inv:{sourceSlotIndex}" để log
            TryDropUnequipped(one, $"inv:{sourceSlotIndex}");
        }

        slotEntry.quantity -= remove;
        if (slotEntry.quantity <= 0)
        {
            // xóa entry khỏi inventory
            inventory.Remove(slotEntry);
        }
        SaveInventory();

        // Cập nhật UI inventory sau khi thay đổi
        var invUi = FindFirstObjectByType<InventoryUIManager>();
        if (invUi != null) invUi.RefreshFromCurrentData();
        return true;
    }
}