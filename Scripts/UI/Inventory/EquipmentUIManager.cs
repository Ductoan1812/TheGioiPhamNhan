using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;
using Xianxia.PlayerDataSystem;
using Xianxia.Items;

public class EquipmentUIManager : MonoBehaviour
{
    [System.Serializable]
    public class EquipSlot
    {
        public string equipSlotName;
        public SlotItem slotItem;
    }

    public UnityEvent onStatsLoaded;
    public EquipmentData equipmentData;
    public List<EquipSlot> equipSlots = new List<EquipSlot>();
    public ItemDatabaseSO itemDatabase;

    private Dictionary<string, EquipSlot> _slotMap;

    public void OnEnable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;

        EnsureLists();
        BuildMap();
        SubscribeSlotHandlers();

        // Nếu dữ liệu đã có sẵn trước khi script này bật, refresh ngay
        if (PlayerManager.Instance != null && PlayerManager.Instance.Data != null)
        {
            OnPlayerDataLoaded(PlayerManager.Instance.Data);
        }
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;

        UnsubscribeSlotHandlers();
    }

    public void OnPlayerDataLoaded(PlayerData data)
    {
        equipmentData = data?.equipment;
        if (equipmentData != null)
        {
            RefreshAllSlots();
        }
        onStatsLoaded?.Invoke();
    }

    private void OnValidate()
    {
        EnsureLists();
        BuildMap();
    }

    private void EnsureLists()
    {
        if (equipSlots == null) equipSlots = new List<EquipSlot>();
        if (_slotMap == null) _slotMap = new Dictionary<string, EquipSlot>(System.StringComparer.OrdinalIgnoreCase);
    }

    private void BuildMap()
    {
        if (_slotMap == null)
            _slotMap = new Dictionary<string, EquipSlot>(System.StringComparer.OrdinalIgnoreCase);
        _slotMap.Clear();
        if (equipSlots == null) return;
        foreach (var s in equipSlots)
        {
            if (s == null || string.IsNullOrEmpty(s.equipSlotName)) continue;
            _slotMap[s.equipSlotName] = s;
        }
    }

    private void SubscribeSlotHandlers()
    {
        if (equipSlots == null) return;
        foreach (var s in equipSlots)
        {
            if (s != null && s.slotItem != null)
            {
                s.slotItem.DroppedOnThis += HandleDroppedOnEquipSlot;
                s.slotItem.DoubleClicked += HandleEquipSlotDoubleClick;
                s.slotItem.DroppedOutside += HandleEquipSlotDropOutside;
                s.slotItem.BeganDrag += _ => UIManager.Instance?.ShowInventoryAndEquipment();
            }
        }
    }

    private void UnsubscribeSlotHandlers()
    {
        if (equipSlots == null) return;
        foreach (var s in equipSlots)
        {
            if (s != null && s.slotItem != null)
            {
                s.slotItem.DroppedOnThis -= HandleDroppedOnEquipSlot;
                s.slotItem.DoubleClicked -= HandleEquipSlotDoubleClick;
                s.slotItem.DroppedOutside -= HandleEquipSlotDropOutside;
            }
        }
    }

    public void RefreshAllSlots()
    {
        if (equipmentData == null || _slotMap == null) return;
        foreach (var kv in _slotMap)
        {
            InventoryItem item = null;
            foreach (var pair in equipmentData.EnumerateSlots())
            {
                if (string.Equals(pair.Item1, kv.Key, System.StringComparison.OrdinalIgnoreCase))
                {
                    item = pair.Item2; break;
                }
            }
            UpdateSlotUI(kv.Key, item);
        }
    }

    public async void UpdateSlotUI(string slotId, InventoryItem item)
    {
        if (_slotMap == null || !_slotMap.TryGetValue(slotId, out var slot)) return;
        if (slot.slotItem == null) return;

        if (item == null)
        {
            slot.slotItem.Clear();
            return;
        }

        var data = itemDatabase != null ? itemDatabase.GetById(item.id) : null;
        string iconAddr = data != null && !string.IsNullOrEmpty(data.addressIcon) ? data.addressIcon : item.addressIcon;
        await slot.slotItem.SetItemFromAddressAsync(item, iconAddr, showQuantity:false);
    }

    // Drag from inventory SlotItem and drop onto an equipment SlotItem
    private void HandleDroppedOnEquipSlot(SlotItem source, SlotItem target)
    {
        if (source == null || target == null) return;
        string toSlotId = null;
        foreach (var kv in _slotMap)
        {
            if (kv.Value.slotItem == target) { toSlotId = kv.Key; break; }
        }
        if (string.IsNullOrEmpty(toSlotId)) return;

        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[EquipmentUIManager] PlayerInventory.Instance is null");
            return;
        }

        // Check if source is also an equipment slot -> move/swap within equipment
        string fromSlotId = null;
        foreach (var kv in _slotMap)
        {
            if (kv.Value.slotItem == source) { fromSlotId = kv.Key; break; }
        }

        if (!string.IsNullOrEmpty(fromSlotId))
        {
            // Move between equipment slots without touching inventory
            PlayerInventory.Instance.MoveEquipment(fromSlotId, toSlotId);
        }
        else
        {
            var item = source.CurrentItem;
            if (item == null || item.quantity <= 0) return;
            // Source is inventory -> Equip (will remove 1 from inventory and handle old item return)
            PlayerInventory.Instance.EquipItem(item, toSlotId);
        }
    }

    // Double click on an equipment slot to unequip to inventory
    private void HandleEquipSlotDoubleClick(SlotItem slot)
    {
        if (slot == null) return;
        string slotId = null;
        foreach (var kv in _slotMap)
        {
            if (kv.Value.slotItem == slot) { slotId = kv.Key; break; }
        }
        if (string.IsNullOrEmpty(slotId)) return;
        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[EquipmentUIManager] PlayerInventory.Instance is null");
            return;
        }
        PlayerInventory.Instance.UnEquipItem(slotId);
    }

    // Kéo từ ô trang bị ra ngoài -> drop vật phẩm ra thế giới (không trả về túi)
    private void HandleEquipSlotDropOutside(SlotItem slot)
    {
        if (slot == null) return;
        string slotId = null;
        foreach (var kv in _slotMap)
        {
            if (kv.Value.slotItem == slot) { slotId = kv.Key; break; }
        }
        if (string.IsNullOrEmpty(slotId)) return;
        if (PlayerInventory.Instance == null)
        {
            Debug.LogWarning("[EquipmentUIManager] PlayerInventory.Instance is null");
            return;
        }
        PlayerInventory.Instance.DropEquippedItem(slotId);
    }
}