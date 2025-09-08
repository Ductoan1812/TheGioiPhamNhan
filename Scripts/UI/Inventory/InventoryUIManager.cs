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

        // Nếu nguồn là ô trang bị: tháo và đặt vào ô inventory đích
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
            var pd = PlayerManager.Instance?.Data;
            if (pd == null) return;
            var oldItem = pd.equipment.Unequip(equipSlotName);
            if (oldItem == null)
            {
                // Không có gì để tháo
                if (eq != null) eq.UpdateSlotUI(equipSlotName, null);
                var eqVisual0 = FindFirstObjectByType<Xianxia.Player.PlayerEquitment>();
                if (eqVisual0 != null) eqVisual0.RefreshSlotVisual(equipSlotName, null);
                return;
            }

            var b = target.CurrentItem;
            // Trường hợp ô đích trống -> đặt trực tiếp
            if (b == null || b.quantity <= 0)
            {
                oldItem.Slot = target.slotIndex;
                pd.inventory.Add(oldItem);
                target.SetItem(oldItem);
            }
            else if (playerInventory != null && playerInventory.IsSameItem(oldItem, b))
            {
                // Ghép stack nếu cùng item
                int canAdd = Mathf.Max(0, b.maxStack - b.quantity);
                if (canAdd > 0)
                {
                    int move = Mathf.Min(canAdd, oldItem.quantity);
                    b.quantity += move;
                    oldItem.quantity -= move;
                    target.SetItem(b);

                    if (oldItem.quantity > 0)
                    {
                        // Phần còn lại: tìm slot trống
                        int empty = playerInventory.GetEmptySlot();
                        if (empty != -1)
                        {
                            oldItem.Slot = empty;
                            pd.inventory.Add(oldItem);
                            // Nếu UI đã build đủ slot, cập nhật
                            if (empty >= 0 && empty < slots.Count)
                                slots[empty].SetItem(oldItem);
                        }
                        else
                        {
                            // Không còn chỗ -> trả ngược lại trang bị để tránh mất item
                            pd.equipment.Equip(equipSlotName, oldItem, overwrite: true);
                        }
                    }
                }
                else
                {
                    // Không thể ghép nữa -> xử lý như swap nhẹ: tìm chỗ trống cho b
                    int empty = playerInventory != null ? playerInventory.GetEmptySlot() : -1;
                    if (empty != -1)
                    {
                        b.Slot = empty;
                        if (empty >= 0 && empty < slots.Count)
                            slots[empty].SetItem(b);
                        // Đặt oldItem vào ô target
                        oldItem.Slot = target.slotIndex;
                        // b đang là reference trong data.inventory, oldItem chưa có -> thêm
                        pd.inventory.Add(oldItem);
                        target.SetItem(oldItem);
                    }
                    else
                    {
                        // Hết chỗ -> trả lại trang bị
                        pd.equipment.Equip(equipSlotName, oldItem, overwrite: true);
                        return;
                    }
                }
            }
            else
            {
                // Ô đích có item khác -> đổi chỗ: đẩy item b sang chỗ trống, đặt oldItem vào target
                int empty = playerInventory != null ? playerInventory.GetEmptySlot() : -1;
                if (empty != -1)
                {
                    b.Slot = empty;
                    if (empty >= 0 && empty < slots.Count)
                        slots[empty].SetItem(b);
                    oldItem.Slot = target.slotIndex;
                    pd.inventory.Add(oldItem);
                    target.SetItem(oldItem);
                }
                else
                {
                    // Không có chỗ -> hủy thao tác
                    pd.equipment.Equip(equipSlotName, oldItem, overwrite: true);
                    return;
                }
            }

            // Cập nhật ô trang bị (clear icon) và visual
            if (eq != null) eq.UpdateSlotUI(equipSlotName, null);
            var eqVisual = FindFirstObjectByType<Xianxia.Player.PlayerEquitment>();
            if (eqVisual != null) eqVisual.RefreshSlotVisual(equipSlotName, null);

            // Lưu
            Save();
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
