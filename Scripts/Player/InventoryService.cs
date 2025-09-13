using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Xianxia.PlayerDataSystem;
using Xianxia.Items;

namespace Xianxia.Player
{
    /// <summary>
    /// Pure logic service (Mono for UnityEvent hookup) that manipulates PlayerData inventory & equipment.
    /// Emits events so UI / other systems can subscribe instead of tight coupling.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class InventoryService : MonoBehaviour
    {
        public static InventoryService Instance { get; private set; }

        public static InventoryService EnsureInstance()
        {
            if (Instance != null) return Instance;
            var existing = FindFirstObjectByType<InventoryService>();
            if (existing != null)
            {
                Instance = existing;
                return Instance;
            }
            var go = new GameObject("InventoryService");
            DontDestroyOnLoad(go);
            Instance = go.AddComponent<InventoryService>();
            return Instance;
        }

        public enum EquipResult
        {
            Success,
            InvalidArguments,
            NoPlayerData,
            InvalidSlot,
            SlotRuleMismatch,
            ItemNotFound,
            InventoryFull,
            OverwriteFailed,
            SameSlot,
            SwapInvalid
        }

        public event Action<IReadOnlyList<InventoryItem>> OnInventoryChanged;
        public event Action<string, InventoryItem, InventoryItem> OnEquipmentChanged; // slotId, newItem, oldItem
        public event Action<IReadOnlyList<InventoryDelta>> OnInventoryDelta; // batched changes emitted on save

        public class InventoryDelta
        {
            public enum DeltaType { Added, Removed, QuantityChanged }
            public DeltaType Type;
            public string ItemId;
            public int Slot;
            public int QuantityChange;
            public int NewQuantity;
        }

        private PlayerData data;
        private List<InventoryItem> inventory;
        private EquipmentData equipment;
        private int cachedCapacity;
    private bool savePending;
    private readonly List<InventoryDelta> pendingDeltas = new();
    private float lastSaveRequestTime;
    [SerializeField] private float saveDebounceSeconds = 0.05f;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnEnable()
        {
            if (PlayerManager.Instance != null)
                PlayerManager.Instance.OnPlayerDataLoaded += HandleLoaded;
            if (PlayerManager.Instance?.Data != null)
                HandleLoaded(PlayerManager.Instance.Data);
        }

        private void OnDisable()
        {
            if (PlayerManager.Instance != null)
                PlayerManager.Instance.OnPlayerDataLoaded -= HandleLoaded;
        }

        private void HandleLoaded(PlayerData d)
        {
            data = d;
            inventory = (d.inventory ?? new List<InventoryItem>()).Where(HasRealItem).ToList();
            equipment = d.equipment;
            cachedCapacity = d.InventorySize > 0 ? d.InventorySize : 30;
            OnInventoryChanged?.Invoke(inventory);
            // Fire equipment for all current slots
            if (equipment != null)
            {
                foreach (var p in equipment.EnumerateSlots())
                {
                    OnEquipmentChanged?.Invoke(p.Item1, p.Item2, null);
                }
            }
        }

        // == Inventory Ops ==
        public (int added, int remainder) AddItem(InventoryItem source)
        {
            if (source == null || source.quantity <= 0 || data == null) return (0, source?.quantity ?? 0);
            EnsureCapacity();
            int remaining = source.quantity;
            int added = 0;
            int maxStack = source.maxStack > 0 ? source.maxStack : int.MaxValue;
            // Fill existing
            foreach (var it in inventory)
            {
                if (!IsSameItem(it, source)) continue;
                if (it.quantity >= it.maxStack) continue;
                int can = Math.Min(it.maxStack - it.quantity, remaining);
                if (can > 0)
                {
                    int oldQty = it.quantity;
                    it.quantity += can; remaining -= can; added += can;
                    QueueDelta(InventoryDelta.DeltaType.QuantityChanged, it, it.quantity - oldQty, it.quantity);
                }
                if (remaining <= 0) break;
            }
            // New stacks
            while (remaining > 0)
            {
                int slot = GetEmptySlot();
                if (slot == -1) break;
                int stackAmount = Math.Min(maxStack, remaining);
                var clone = CloneWithOverride(source, stackAmount, slot);
                inventory.Add(clone);
                QueueDelta(InventoryDelta.DeltaType.Added, clone, stackAmount, clone.quantity);
                remaining -= stackAmount; added += stackAmount;
            }
            RequestSave();
            return (added, remaining);
        }

        public bool RemoveItem(InventoryItem template, int quantity)
        {
            if (template == null || quantity <= 0) return false;
            int remaining = quantity;
            foreach (var it in inventory.Where(i => IsSameItem(i, template)).ToList())
            {
                if (remaining <= 0) break;
                if (it.quantity > remaining) { it.quantity -= remaining; QueueDelta(InventoryDelta.DeltaType.QuantityChanged, it, -remaining, it.quantity); remaining = 0; }
                else { remaining -= it.quantity; QueueDelta(InventoryDelta.DeltaType.Removed, it, -it.quantity, 0); it.quantity = 0; }
            }
            inventory.RemoveAll(x => x.quantity <= 0);
            RequestSave();
            return remaining == 0;
        }

        public bool UseItem(InventoryItem item, int qty)
        {
            if (item == null || qty <= 0) return false;
            var stack = inventory.FirstOrDefault(i => i.Slot == item.Slot && IsSameItem(i, item)) ?? inventory.FirstOrDefault(i => IsSameItem(i, item));
            if (stack == null) return false;
            int remove = Math.Min(qty, stack.quantity);
            stack.quantity -= remove;
            if (stack.quantity <= 0)
            {
                QueueDelta(InventoryDelta.DeltaType.Removed, stack, -remove, 0);
                inventory.Remove(stack);
            }
            else
            {
                QueueDelta(InventoryDelta.DeltaType.QuantityChanged, stack, -remove, stack.quantity);
            }
            RequestSave();
            // TODO: trigger effect
            return true;
        }

        public bool SplitStack(InventoryItem item, int splitQty)
        {
            if (item == null || splitQty <= 0) return false;
            var stack = inventory.FirstOrDefault(i => i.Slot == item.Slot && IsSameItem(i, item)) ?? inventory.FirstOrDefault(i => IsSameItem(i, item));
            if (stack == null || splitQty >= stack.quantity) return false;
            int empty = GetEmptySlot(); if (empty == -1) return false;
            stack.quantity -= splitQty;
            QueueDelta(InventoryDelta.DeltaType.QuantityChanged, stack, -splitQty, stack.quantity);
            var newStack = CloneWithOverride(stack, splitQty, empty);
            inventory.Add(newStack);
            QueueDelta(InventoryDelta.DeltaType.Added, newStack, splitQty, newStack.quantity);
            RequestSave();
            return true;
        }

        // == Equipment Ops ==
        [Obsolete("Use EquipEx for detailed result")] public bool Equip(InventoryItem item, string slotId) => EquipEx(item, slotId) == EquipResult.Success;

        public EquipResult EquipEx(InventoryItem item, string slotId)
        {
            if (equipment == null || data == null) return EquipResult.NoPlayerData;
            if (item == null || string.IsNullOrEmpty(slotId)) return EquipResult.InvalidArguments;
            slotId = slotId.ToLowerInvariant();
            if (!EquipmentSlotRules.IsKnownSlot(slotId)) return EquipResult.InvalidSlot;
            if (!EquipmentSlotRules.IsValidForSlot(slotId, item)) return EquipResult.SlotRuleMismatch;
            var single = CloneWithOverride(item, 1, -1);
            var old = equipment.Unequip(slotId);
            if (!equipment.Equip(slotId, single, overwrite: true))
            {
                if (old != null) equipment.Equip(slotId, old, overwrite: true);
                return EquipResult.OverwriteFailed;
            }
            RemoveItem(item, 1);
            OnEquipmentChanged?.Invoke(slotId, single, old);
            RequestSave();
            return EquipResult.Success;
        }

        [Obsolete("Use UnEquipEx for detailed result")] public bool UnEquip(string slotId) => UnEquipEx(slotId) == EquipResult.Success;

        public EquipResult UnEquipEx(string slotId)
        {
            if (equipment == null) return EquipResult.NoPlayerData;
            if (string.IsNullOrEmpty(slotId)) return EquipResult.InvalidArguments;
            slotId = slotId.ToLowerInvariant();
            if (!EquipmentSlotRules.IsKnownSlot(slotId)) return EquipResult.InvalidSlot;
            var old = equipment.Unequip(slotId);
            if (old == null) return EquipResult.ItemNotFound;
            old.quantity = 1;
            var (added, rem) = AddItem(old);
            if (rem > 0)
            {
                equipment.Equip(slotId, old, overwrite: true);
                return EquipResult.InventoryFull;
            }
            OnEquipmentChanged?.Invoke(slotId, null, old);
            RequestSave();
            return EquipResult.Success;
        }

        [Obsolete("Use MoveEquipmentEx for detailed result")] public bool MoveEquipment(string fromSlot, string toSlot) => MoveEquipmentEx(fromSlot, toSlot) == EquipResult.Success;

        public EquipResult MoveEquipmentEx(string fromSlot, string toSlot)
        {
            if (equipment == null) return EquipResult.NoPlayerData;
            fromSlot = fromSlot?.ToLowerInvariant();
            toSlot = toSlot?.ToLowerInvariant();
            if (string.IsNullOrEmpty(fromSlot) || string.IsNullOrEmpty(toSlot)) return EquipResult.InvalidArguments;
            if (fromSlot == toSlot) return EquipResult.SameSlot;
            if (!EquipmentSlotRules.IsKnownSlot(fromSlot) || !EquipmentSlotRules.IsKnownSlot(toSlot)) return EquipResult.InvalidSlot;
            equipment.TryGet(fromSlot, out var fromItem);
            equipment.TryGet(toSlot, out var toItem);
            if (fromItem == null) return EquipResult.ItemNotFound;
            if (!EquipmentSlotRules.IsValidForSlot(toSlot, fromItem)) return EquipResult.SlotRuleMismatch;
            if (toItem != null && !EquipmentSlotRules.IsValidForSlot(fromSlot, toItem)) return EquipResult.SwapInvalid;
            bool setTo = equipment.Equip(toSlot, fromItem, overwrite: true);
            bool setFrom = equipment.Equip(fromSlot, toItem, overwrite: true);
            if (!setTo || !setFrom) return EquipResult.OverwriteFailed;
            OnEquipmentChanged?.Invoke(fromSlot, toItem, fromItem);
            OnEquipmentChanged?.Invoke(toSlot, fromItem, toItem);
            RequestSave();
            return EquipResult.Success;
        }

        // == Debounced Save & Delta Helpers ==
        private void RequestSave()
        {
            lastSaveRequestTime = Time.unscaledTime;
            if (!savePending)
            {
                savePending = true;
                StartCoroutine(SaveDebounceCoroutine());
            }
        }

        private System.Collections.IEnumerator SaveDebounceCoroutine()
        {
            while (savePending)
            {
                if (Time.unscaledTime - lastSaveRequestTime >= saveDebounceSeconds)
                {
                    Save();
                    yield break;
                }
                yield return null;
            }
        }

        private void FlushSaveImmediate()
        {
            if (savePending) Save();
        }

        private void QueueDelta(InventoryDelta.DeltaType type, InventoryItem item, int qtyChange, int newQty)
        {
            if (item == null) return;
            pendingDeltas.Add(new InventoryDelta
            {
                Type = type,
                ItemId = item.id,
                Slot = item.Slot,
                QuantityChange = qtyChange,
                NewQuantity = newQty
            });
        }

        // == Helpers ==
        private void EnsureCapacity()
        {
            if (data == null) return;
            cachedCapacity = data.InventorySize > 0 ? data.InventorySize : 30;
        }

        private int GetEmptySlot()
        {
            EnsureCapacity();
            var used = new HashSet<int>(inventory.Select(i => i.Slot));
            for (int i = 0; i < cachedCapacity; i++) if (!used.Contains(i)) return i;
            return -1;
        }

        private static bool IsSameItem(InventoryItem a, InventoryItem b) => a != null && b != null && a.id == b.id;
        private static bool HasRealItem(InventoryItem i) => i != null && !string.IsNullOrEmpty(i.id) && i.quantity > 0;

        private InventoryItem CloneWithOverride(InventoryItem src, int quantity, int slot)
        {
            var clone = new InventoryItem
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
                baseStats = new Xianxia.Items.BaseStats
                {
                    atk = src.baseStats?.atk ?? 0,
                    defense = src.baseStats?.defense ?? 0,
                    hp = src.baseStats?.hp ?? 0,
                    qi = src.baseStats?.qi ?? 0,
                    moveSpd = src.baseStats?.moveSpd ?? 0,
                    critRate = src.baseStats?.critRate ?? 0,
                    critDmg = src.baseStats?.critDmg ?? 0,
                    penetration = src.baseStats?.penetration ?? 0,
                    lifestealQi = src.baseStats?.lifestealQi ?? 0,
                    res = new Xianxia.Items.Resist
                    {
                        kim = src.baseStats?.res?.kim ?? 0,
                        moc = src.baseStats?.res?.moc ?? 0,
                        thuy = src.baseStats?.res?.thuy ?? 0,
                        hoa = src.baseStats?.res?.hoa ?? 0,
                        tho = src.baseStats?.res?.tho ?? 0,
                        loi = src.baseStats?.res?.loi ?? 0,
                        am = src.baseStats?.res?.am ?? 0,
                        duong = src.baseStats?.res?.duong ?? 0
                    }
                },
                sockets = src.sockets,
                affixes = src.affixes != null ? src.affixes.Select(a => a == null ? null : new Xianxia.Items.AffixEntry { id = a.id, value = a.value, tier = a.tier }).ToArray() : Array.Empty<Xianxia.Items.AffixEntry>(),
                useEffect = src.useEffect,
                flavor = src.flavor,
                statBonuses = src.statBonuses != null ? new List<Xianxia.Items.StatBonus>(src.statBonuses.Select(b => new Xianxia.Items.StatBonus { id = b.id, add = b.add, pct = b.pct })) : new List<Xianxia.Items.StatBonus>(),
                quantity = quantity,
                Slot = slot
            };
            return clone;
        }

        private void Save()
        {
            if (data == null) return;
            inventory = inventory.Where(HasRealItem).ToList();
            data.inventory = inventory;
            data.equipment = equipment;
            PlayerManager.Instance?.SavePlayer();
            OnInventoryChanged?.Invoke(inventory);
            if (pendingDeltas.Count > 0)
            {
                OnInventoryDelta?.Invoke(pendingDeltas.ToList());
                pendingDeltas.Clear();
            }
            savePending = false;
        }
    }
}
