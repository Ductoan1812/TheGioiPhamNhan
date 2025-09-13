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
    private InventoryService service;
    private InventoryUIManager cachedInvUI;
    private EquipmentUIManager cachedEqUI;
    private Xianxia.Player.PlayerEquitment cachedEqVisual;
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
        service = InventoryService.Instance ?? FindFirstObjectByType<InventoryService>();
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerDataLoaded += HandlePlayerDataLoaded;
            if (PlayerManager.Instance.Data != null) HandlePlayerDataLoaded(PlayerManager.Instance.Data);
        }
        CacheUI();
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataLoaded -= HandlePlayerDataLoaded;
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

    [Obsolete("Use InventoryService.AddItem")]
    public bool AddItem(InventoryItem item)
    {
        service = service ?? InventoryService.Instance;
        if (service == null) return false;
        var result = service.AddItem(item);
        SyncLocalFromData();
        return result.remainder == 0;
    }

    [Obsolete("Use InventoryService.RemoveItem")]
    public bool RemoveItem(InventoryItem item, int quantity)
    {
        service = service ?? InventoryService.Instance;
        if (service == null) return false;
        bool ok = service.RemoveItem(item, quantity);
        SyncLocalFromData();
        return ok;
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

    [Obsolete("Use InventoryService.Equip")] public void EquipItem(InventoryItem item, string equipSlot)
    {
        service = service ?? InventoryService.Instance;
        if (service == null) return;
        if (service.Equip(item, equipSlot))
        {
            SyncLocalFromData();
            RefreshUIEquip(equipSlot);
        }
    }

    public void UnEquipItem(string equipSlot)
    {
        service = service ?? InventoryService.Instance;
        if (service == null) return;
        var result = service.UnEquipEx(equipSlot);
        if (result == InventoryService.EquipResult.Success)
        {
            SyncLocalFromData();
            RefreshUIEquip(equipSlot);
        }
    }

    [Obsolete("Use InventoryService.UnEquip")] public void UnEquipItem(string equipSlot, int? targetIndex)
    {
        service = service ?? InventoryService.Instance;
        if (service == null) return;
        var result = service.UnEquipEx(equipSlot);
        if (result == InventoryService.EquipResult.Success)
        {
            SyncLocalFromData();
            RefreshUIEquip(equipSlot);
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
    [Obsolete("Use InventoryService.MoveEquipment")] public bool MoveEquipment(string fromSlot, string toSlot)
    {
        service = service ?? InventoryService.Instance;
        if (service == null) return false;
        bool ok = service.MoveEquipment(fromSlot, toSlot);
        if (ok)
        {
            SyncLocalFromData();
            RefreshUIEquip(fromSlot);
            RefreshUIEquip(toSlot);
        }
        return ok;
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
    private void SaveInventory() { /* delegated to InventoryService.Save() */ }

    // Một entry có item thật sự: id hợp lệ và quantity > 0
    private bool HasRealItem(InventoryItem it)
    {
        return it != null && !string.IsNullOrEmpty(it.id) && it.quantity > 0;
    }

    // Thử thả vật phẩm unequip ra thế giới nếu túi đầy
    private bool TryDropUnequipped(InventoryItem item, string fromSlot)
    {
        // TODO: move to InventoryService with injectable dropper
        if (item == null) return false;
        Vector3 pos = Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 1.5f : transform.position;
        var dropMgr = FindFirstObjectByType<ItemDropManager>();
        if (dropMgr == null) return false;
        var dropItem = CloneAsSingle(item);
        dropMgr.Spawn(dropItem, pos + Vector3.right * 0.5f);
        return true;
    }

    private void CacheUI()
    {
        cachedInvUI = cachedInvUI ?? FindFirstObjectByType<InventoryUIManager>();
        cachedEqUI = cachedEqUI ?? FindFirstObjectByType<EquipmentUIManager>();
        cachedEqVisual = cachedEqVisual ?? FindFirstObjectByType<Xianxia.Player.PlayerEquitment>();
    }

    private void RefreshUIEquip(string slotId)
    {
        CacheUI();
        if (cachedEqUI != null && equipment != null)
        {
            equipment.TryGet(slotId?.ToLowerInvariant(), out var itm);
            cachedEqUI.UpdateSlotUI(slotId, itm);
        }
        if (cachedEqVisual != null && equipment != null)
        {
            equipment.TryGet(slotId?.ToLowerInvariant(), out var itm2);
            cachedEqVisual.RefreshSlotVisual(slotId, itm2);
        }
        cachedInvUI?.RefreshFromCurrentData();
    }

    private void SyncLocalFromData()
    {
        var d = PlayerManager.Instance?.Data;
        if (d == null) return;
        inventory = d.inventory;
        equipment = d.equipment;
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