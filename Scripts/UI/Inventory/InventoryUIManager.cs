using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Xianxia.PlayerDataSystem;

/// Quản lý logic kéo/thả giữa các SlotItem: gộp stack, hoán đổi, di chuyển.
public class InventoryUIManager : MonoBehaviour
{
    [Header("Tham chiếu")]
    public PlayerInventory playerInventory;
    [Tooltip("Parent SlotItem (GridLayoutGroup)")]
    public Transform gridRoot;
    [Tooltip("Prefab SlotItem")]
    public SlotItem slotPrefab;
    [Tooltip("Tự tạo UI khi có dữ liệu Player")] public bool autoBuildOnStart = true;

    private readonly List<SlotItem> slots = new List<SlotItem>();

    private void OnEnable()
    {
        if (autoBuildOnStart && PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerDataLoaded += HandlePlayerDataLoaded;
            if (PlayerManager.Instance.Data != null)
                HandlePlayerDataLoaded(PlayerManager.Instance.Data);
        }
    }

    public void RefreshFromCurrentData()
    {
        var data = PlayerManager.Instance != null ? PlayerManager.Instance.Data : null;
        RebuildFromData(data);
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataLoaded -= HandlePlayerDataLoaded;
    }

    private void HandlePlayerDataLoaded(PlayerData data)
    {
        RebuildFromData(data);
    }

    /// Tạo số slot theo InventorySize và đổ item từ PlayerData
    public void Rebuild()
    {
        var data = PlayerManager.Instance != null ? PlayerManager.Instance.Data : null;
        RebuildFromData(data);
    }

    private void RebuildFromData(PlayerData data)
    {
        if (gridRoot == null || slotPrefab == null)
        {
            Debug.LogWarning("[InventoryUIManager] Chưa gán gridRoot/slotPrefab");
            return;
        }

        int size = data != null && data.InventorySize > 0 ? data.InventorySize : 30;
        BuildSlots(size);
        PopulateItems(data);
    }
    private void BuildSlots(int count)
    {
        slots.Clear();
        for (int i = gridRoot.childCount - 1; i >= 0; i--)
        {
            Destroy(gridRoot.GetChild(i).gameObject);
        }
        for (int i = 0; i < count; i++)
        {
            var slot = Instantiate(slotPrefab, gridRoot);
            slot.slotIndex = i;
            slot.SetItem(null);
            slot.onDropOnThis.AddListener(OnDropOnSlot);
            slots.Add(slot);
        }
    }

    private void PopulateItems(PlayerData data)
    {
        if (data == null) return;
        if (data.inventory == null) return;
        foreach (var it in data.inventory)
        {
            if (it == null) continue;
            int idx = it.Slot;
            if (idx >= 0 && idx < slots.Count)
            {
                slots[idx].SetItem(it);
            }
        }
    }
    // Gắn script này vào cùng GameObject cha chứa các SlotItem.
    // Liên kết bằng Inspector: với mỗi SlotItem, thêm listener: onDropOnThis -> InventoryUIManager.OnDropOnSlot
    public void OnDropOnSlot(SlotItem source, SlotItem target)
    {
        if (source == null || target == null || source == target) return;

        var eq = FindFirstObjectByType<EquipmentUIManager>();
        string equipSlotName = null;
        if (eq != null && eq.equipSlots != null)
        {
            foreach (var es in eq.equipSlots)
            {
                if (es != null && es.slotItem == source)
                {
                    equipSlotName = es.equipSlotName;
                    break;
                }
            }
        }
        if (!string.IsNullOrEmpty(equipSlotName))
        {
            // Chuyển sang luồng thống nhất: UnEquipItem(slot, targetIndex)
            playerInventory?.UnEquipItem(equipSlotName, target.slotIndex);
            return;
        }

        // ===== Hành vi mặc định: kéo thả giữa các ô inventory =====
        var a = source.CurrentItem;
        var b2 = target.CurrentItem;
        if (a == null || a.quantity <= 0) return;
        if (b2 == null || b2.quantity <= 0)
        {
            MoveItem(source, target);
            return;
        }
        if (playerInventory.IsSameItem(a, b2))
        {
            int canAdd = Mathf.Max(0, b2.maxStack - b2.quantity);
            if (canAdd > 0)
            {
                int move = Mathf.Min(canAdd, a.quantity);
                b2.quantity += move;
                a.quantity -= move;
                if (a.quantity == 0)
                {
                    source.SetItem(null);
                }
                target.SetItem(b2);
                source.SetItem(a);
                Save();
                return;
            }
        }
        SwapItems(source, target);
    }

    private void MoveItem(SlotItem source, SlotItem target)
    {
        var a = source.CurrentItem;
        if (a == null) return;
        a.Slot = target.slotIndex;
        target.SetItem(a);
        source.SetItem(null);
        Save();
    }

    private void SwapItems(SlotItem s, SlotItem t)
    {
        var a = s.CurrentItem;
        var b = t.CurrentItem;
        if (a == null && b == null) return;
        if (a != null) a.Slot = t.slotIndex;
        if (b != null) b.Slot = s.slotIndex;
        t.SetItem(a);
        s.SetItem(b);
        Save();
    }

    private void Save()
    {
        playerInventory?.SendMessage("SaveInventory", SendMessageOptions.DontRequireReceiver);
    }
}
