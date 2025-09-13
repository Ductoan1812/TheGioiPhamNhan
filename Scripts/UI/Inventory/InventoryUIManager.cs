using System.Linq;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using System.Collections;
using Xianxia.PlayerDataSystem;
using Xianxia.Player;

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
    var service = InventoryService.Instance ?? InventoryService.EnsureInstance();
        if (service != null)
        {
            service.OnInventoryChanged += HandleInventoryChanged;
            service.OnEquipmentChanged += HandleEquipmentChanged;
        }
    }

    public void RefreshFromCurrentData()
    {
        var data = PlayerManager.Instance != null ? PlayerManager.Instance.Data : null;
        RebuildFromData(data);
    }

    private void HandleInventoryChanged(System.Collections.Generic.IReadOnlyList<InventoryItem> items)
    {
        var data = PlayerManager.Instance?.Data;
        if (data == null) return;
        data.inventory = items.ToList();
        PopulateItems(data);
    }

    private void HandleEquipmentChanged(string slotId, InventoryItem newItem, InventoryItem oldItem)
    {
        // Equipment changes consume or return inventory items -> refresh inventory visuals.
        RefreshFromCurrentData();
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataLoaded -= HandlePlayerDataLoaded;
    var service = InventoryService.Instance ?? InventoryService.EnsureInstance();
        if (service != null)
        {
            service.OnInventoryChanged -= HandleInventoryChanged;
            service.OnEquipmentChanged -= HandleEquipmentChanged;
        }
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
            // Nếu kéo ra ngoài, drop xuống thế giới
            slot.DroppedOutside += HandleDropOutsideFromInventory;
            // Khi bắt đầu kéo: mở Inventory + Equipment (trì hoãn 1 frame để không cắt đứt sự kiện kéo)
            slot.BeganDrag += _ => StartCoroutine(ShowEquipNextFrame());
            // Khi click: mở Inventory + InfoItem (và đổ thông tin)
            slot.Clicked += HandleSlotClicked;
            slots.Add(slot);
        }
    }

    private void PopulateItems(PlayerData data)
    {
        // Clear all first to avoid ghost items
        foreach (var s in slots) s.SetItem(null);
        if (data == null || data.inventory == null) return;
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
            var service = Xianxia.Player.InventoryService.Instance ?? FindFirstObjectByType<Xianxia.Player.InventoryService>();
            if (service != null)
            {
                var res = service.UnEquipEx(equipSlotName);
                if (res != Xianxia.Player.InventoryService.EquipResult.Success)
                {
                    Debug.LogWarning($"[InventoryUIManager] UnEquipEx {equipSlotName} failed: {res}");
                }
                // InventoryService tự thêm item vào inventory (tự chọn slot). Force refresh UI.
                RefreshFromCurrentData();
            }
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

    // Khi kéo một ô từ inventory và thả ra ngoài mọi Slot -> drop item ra thế giới
    private void HandleDropOutsideFromInventory(SlotItem source)
    {
        if (source == null) return;
        var item = source.CurrentItem;
        if (item == null || item.quantity <= 0) return;
        // Chỉ xử lý nếu thực sự là slot inventory (có chỉ số slot hợp lệ)
        if (source.slotIndex < 0) return;
        // Nếu con trỏ vẫn đang nằm trong vùng lưới inventory -> không drop (thả vào khoảng trống)
        if (IsPointerOverInventoryArea())
        {
            return;
        }
        if (playerInventory == null) playerInventory = FindFirstObjectByType<PlayerInventory>();
    // Thả toàn bộ stack hiện tại
    playerInventory?.DropItemFromInventory(item, item.quantity, source.slotIndex);
        // UI sẽ được PlayerInventory cập nhật/Save tự xử lý
    }

    private bool IsPointerOverInventoryArea()
    {
        if (gridRoot == null) return false;
        var rect = gridRoot as RectTransform;
        if (rect == null) return false;
        var canvas = gridRoot.GetComponentInParent<Canvas>();
        Camera cam = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = canvas.worldCamera;
        Vector2 screenPos = GetScreenPointerPosition();
        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPos, cam);
    }

    private Vector2 GetScreenPointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            return Mouse.current.position.ReadValue();
        }
        return Vector2.zero;
#else
        return Input.mousePosition;
#endif
    }

    private IEnumerator ShowEquipNextFrame()
    {
        yield return null; // đợi hết frame hiện tại để tránh gián đoạn drag
        UIManager.Instance?.ShowInventoryAndEquipment();
    }

    private void HandleSlotClicked(SlotItem source)
    {
        if (source == null) return;
        var item = source.CurrentItem;
        if (item == null || item.quantity <= 0) return;
        UIManager.Instance?.ShowInventoryAndInfoItem();
        // Tìm panel chi tiết và hiển thị
        var details = FindFirstObjectByType<InfoItem>(FindObjectsInactive.Include);
        if (details != null)
        {
            details.Show(item);
        }
    }
}
