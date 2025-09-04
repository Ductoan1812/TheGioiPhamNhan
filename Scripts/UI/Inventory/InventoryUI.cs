using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Xianxia.Items;
using Xianxia.PlayerDataSystem;

namespace Xianxia.UI.Inventory
{
    [DisallowMultipleComponent]
    public class InventoryUI : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private RectTransform gridParent;
        [SerializeField] private GameObject slotPrefab;
        [Tooltip("Nếu PlayerData.Slot là 1-based (1..N), bật cờ này.")]
        [SerializeField] private bool oneBasedIndex = true;

        [Header("Controls")]
        [SerializeField] private Button useButton;
        [SerializeField] private Button splitButton;
        [SerializeField] private TMP_InputField amountInput;

        [Header("Drop to World")]
        [SerializeField] private Transform dropSpawn;
        [SerializeField] private ItemSpawner spawner;

        [Header("Save file")]
        [SerializeField] private string persistentFileName = "PlayerData.json";

        private PlayerData _player;
        private readonly List<SlotPrefab> _slots = new List<SlotPrefab>();
        private int _selectedSlotIndex = -1;

        private void Start()
        {
            if (gridParent == null || slotPrefab == null)
            {
                Debug.LogError("InventoryUI: gridParent hoặc slotPrefab chưa gán.");
                return;
            }

            // Đọc từ persistent, nếu chưa có thì tạo rỗng
            _player = PlayerData.LoadOrCreate(PlayerData.GetDefaultPath(persistentFileName));
            BuildGridSlots(_player.InventorySize);
            HookButtons();
            RefreshAll();
        }

        private void OnDestroy()
        {
            UnhookButtons();
        }

        private void HookButtons()
        {
            if (useButton != null) useButton.onClick.AddListener(OnClickUse);
            if (splitButton != null) splitButton.onClick.AddListener(OnClickSplit);
        }

        private void UnhookButtons()
        {
            if (useButton != null) useButton.onClick.RemoveListener(OnClickUse);
            if (splitButton != null) splitButton.onClick.RemoveListener(OnClickSplit);
        }

        private void BuildGridSlots(int size)
        {
            while (gridParent.childCount > size)
                Destroy(gridParent.GetChild(gridParent.childCount - 1).gameObject);

            while (gridParent.childCount < size)
            {
                var go = Instantiate(slotPrefab, gridParent);
                go.name = $"Slot_{gridParent.childCount + 1}";
            }

            _slots.Clear();
            for (int i = 0; i < gridParent.childCount; i++)
            {
                var child = gridParent.GetChild(i);
                var slot = child.GetComponent<SlotPrefab>();
                if (slot == null) slot = child.gameObject.AddComponent<SlotPrefab>();
                slot.SetIndex(i);
                slot.SetOwner(this);
                slot.SetEmpty();
                _slots.Add(slot);
            }
        }

        public void SavePlayerData()
        {
            _player?.SaveToFile(PlayerData.GetDefaultPath(persistentFileName));
        }

        // ===== API từ SlotPrefab (click) =====
        public void OnSlotClicked(int slotIndex)
        {
            SelectSlot(slotIndex);
        }

        // ===== Use & Split =====
        private void OnClickUse()
        {
            int idx = _selectedSlotIndex;
            if (!IsValidSlotIndex(idx)) return;

            var inv = FindInventoryBySlotIndex(idx);
            if (inv == null) return;

            int amount = ParseAmountOrDefault(1);
            if (amount <= 0) return;

            amount = Mathf.Min(amount, inv.quantity);
            inv.quantity -= amount;

            // TODO: áp dụng hiệu ứng tiêu thụ
            if (inv.quantity <= 0)
            {
                _player.RemoveSlot(inv.Slot);
                ClearSelectionIf(idx);
            }

            SavePlayerData();
            RefreshAll();
        }

        private void OnClickSplit()
        {
            int idx = _selectedSlotIndex;
            if (!IsValidSlotIndex(idx)) return;

            var inv = FindInventoryBySlotIndex(idx);
            if (inv == null || inv.quantity <= 1)
            {
                Debug.LogWarning("Split: không thể tách (item không stack hoặc số lượng <= 1).");
                return;
            }

            int amount = ParseAmountOrDefault(1);
            if (amount <= 0 || amount >= inv.quantity)
            {
                Debug.LogWarning("Split: số lượng tách phải từ 1 đến (quantity - 1).");
                return;
            }

            int empty = FindFirstEmptySlotIndex();
            if (empty < 0)
            {
                Debug.LogWarning("Split: không còn slot trống.");
                return;
            }

            var newEntry = new PlayerInventoryItem
            {
                id = inv.id,
                Slot = ToPlayerSlotIndex(empty),
                quantity = amount,
                level = inv.level,
                affixes = inv.affixes
            };
            _player.SetSlot(newEntry);

            inv.quantity -= amount;
            _player.SetSlot(inv);

            SavePlayerData();
            RefreshAll();
            SelectSlot(empty);
        }

        // ===== Helpers =====
        private int ParseAmountOrDefault(int def)
        {
            if (amountInput == null || string.IsNullOrWhiteSpace(amountInput.text))
                return def;
            if (int.TryParse(amountInput.text, out var v))
                return Mathf.Max(0, v);
            return def;
        }

        private void SelectSlot(int slotIndex)
        {
            if (!IsValidSlotIndex(slotIndex))
            {
                _selectedSlotIndex = -1;
                UpdateSelectionHighlight();
                return;
            }
            _selectedSlotIndex = slotIndex;
            UpdateSelectionHighlight();
        }

        private void ClearSelectionIf(int slotIndex)
        {
            if (_selectedSlotIndex == slotIndex)
            {
                _selectedSlotIndex = -1;
                UpdateSelectionHighlight();
            }
        }

        private void UpdateSelectionHighlight()
        {
            for (int i = 0; i < _slots.Count; i++)
                _slots[i]?.SetSelected(i == _selectedSlotIndex);
        }

        private bool IsValidSlotIndex(int i) => i >= 0 && i < _slots.Count;

        private PlayerInventoryItem FindInventoryBySlotIndex(int uiIndex)
        {
            int playerSlot = ToPlayerSlotIndex(uiIndex);
            return _player?.GetBySlot(playerSlot);
        }

        private int ToPlayerSlotIndex(int uiIndex) => oneBasedIndex ? (uiIndex + 1) : uiIndex;
        private int FromPlayerSlotIndex(int slotField) => oneBasedIndex ? (slotField - 1) : slotField;

        private int FindFirstEmptySlotIndex()
        {
            var used = new HashSet<int>();
            foreach (var it in _player.inventory)
            {
                int ui = FromPlayerSlotIndex(it.Slot);
                if (ui >= 0 && ui < _slots.Count) used.Add(ui);
            }
            for (int i = 0; i < _slots.Count; i++)
                if (!used.Contains(i)) return i;
            return -1;
        }

        private void RefreshAll()
        {
            if (_player == null) return;

            if (_slots.Count != _player.InventorySize)
                BuildGridSlots(_player.InventorySize);

            foreach (var s in _slots) s.SetEmpty();

            foreach (var it in _player.inventory)
            {
                int uiIndex = FromPlayerSlotIndex(it.Slot);
                if (!IsValidSlotIndex(uiIndex)) continue;

                var def = ItemDatabaseSO.Instance?.GetById(it.id);
                _slots[uiIndex].BindItem(it.id, it.quantity, def);
            }

            UpdateSelectionHighlight();
        }

        // Không dùng kéo-thả ở bản đơn giản. Nếu cần, sẽ bổ sung lại sau.

        private bool DropToWorld(string itemId, int quantity)
        {
            if (spawner == null)
            {
                Debug.LogError("InventoryUI: spawner chưa gán.");
                return false;
            }
            Vector3 pos = dropSpawn != null ? dropSpawn.position : Vector3.zero;
            var go = spawner.Spawn(itemId, quantity, pos);

            var rb = go != null ? go.GetComponent<Rigidbody2D>() : null;
            if (rb != null)
            {
                var dir = UnityEngine.Random.insideUnitCircle.normalized;
                rb.AddForce(dir * 2.5f, ForceMode2D.Impulse);
            }
            return true;
        }
    }
}