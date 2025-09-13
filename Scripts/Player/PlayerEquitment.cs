using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Xianxia.PlayerDataSystem;
using Xianxia.Items;
using System;
// using System.Reflection; // no longer needed

namespace Xianxia.Player
{
    public class PlayerEquitment : MonoBehaviour
    {   
        
        public UnityEvent onStatsLoaded;
        public ItemDatabaseSO ItemDatabase;
        public PlayerRenderer playerRenderer;
        private PlayerStatsManager statsManager;
        private Xianxia.Stats.StatCollection stats; // tham chiếu StatCollection mới

        private EquipmentData Equipment;

        private void OnEnable()
        {
            StartCoroutine(WaitAndHook());
        }

        private IEnumerator WaitAndHook()
        {
            while (PlayerManager.Instance == null) yield return null;
            PlayerManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;

            var d = PlayerManager.Instance.Data;
            if (d != null) OnPlayerDataLoaded(d);
        }

        private void OnDisable()
        {
            if (PlayerManager.Instance != null)
                PlayerManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;

            // No event subscriptions; visuals refreshed on demand
        }
        private void OnPlayerDataLoaded(PlayerData data)
        {
            Equipment = data?.equipment;
            stats = data?.stats;
            statsManager = GetComponent<PlayerStatsManager>();
            if (Equipment != null)
            {
                Equipment.OnEquipped += OnEquipmentChanged;
                Equipment.OnUnequipped += OnEquipmentChanged;
            }
            // Áp lại toàn bộ modifier từ trang bị hiện có (sau load)
            ReapplyAllModifiers();
            ApplyToPlayerData();
            onStatsLoaded?.Invoke();
            Debug.Log($"[PlayerEquitment] Loaded equipment for {data.id}");
            
        }
        private void OnEquipmentChanged(string slotId, InventoryItem item)
        {
            var data = PlayerManager.Instance?.Data;
            // Cập nhật modifiers trong StatCollection
            if (stats != null && !string.IsNullOrEmpty(slotId))
            {
                stats.RemoveModifiersBySource(slotId);
                if (item != null && item.statBonuses != null)
                {
                    foreach (var b in item.statBonuses)
                    {
                        stats.AddModifier(b.id, b.add, b.pct, slotId);
                    }
                }
            }
            RefreshSlotVisual(slotId, item);
        }
        private void ReapplyAllModifiers()
        {
            if (Equipment == null || stats == null) return;
            // Clear all
            foreach (var s in Equipment.Slots)
            {
                if (!string.IsNullOrEmpty(s.idSlot))
                    stats.RemoveModifiersBySource(s.idSlot);
            }
            // Reapply
            foreach (var s in Equipment.Slots)
            {
                var it = s.item;
                if (it?.statBonuses == null) continue;
                foreach (var b in it.statBonuses)
                {
                    stats.AddModifier(b.id, b.add, b.pct, s.idSlot);
                }
            }
        }
        // hàm gán trang bị lên nhân vật khi có trang bị thay đổi, 
        public void ApplyToPlayerData(bool save = true)
        {
            if (Equipment == null || playerRenderer == null || ItemDatabase == null) return;

            // Render all current slots
            foreach (var pair in Equipment.EnumerateSlots())
            {
                ApplySlotVisual(pair.Item1, pair.Item2);
            }
            
        }

        public void RefreshSlotVisual(string slotId, InventoryItem item)
        {
            ApplySlotVisual(slotId, item);
        }

        private void ApplySlotVisual(string slotId, InventoryItem item)
        {
            if (string.IsNullOrEmpty(slotId) || playerRenderer == null) return;

            var group = GetRendererGroupBySlot(slotId);
            if (group == null) return; // slot chưa có renderer mapping (vd: ring)

            if (item == null || string.IsNullOrEmpty(item.id))
            {
                ClearSlotSpritesLocal(group);
                return;
            }

            var itemData = ItemDatabase != null ? ItemDatabase.GetById(item.id) : null;
            // Ưu tiên addressTexture từ DB, fallback về item.addressTexture rồi item.addressIcon
            var addressTexture = (!string.IsNullOrEmpty(itemData?.addressTexture))
                                    ? itemData.addressTexture
                                    : (!string.IsNullOrEmpty(item.addressTexture) ? item.addressTexture : item.addressIcon);
            if (string.IsNullOrEmpty(addressTexture))
            {
                ClearSlotSpritesLocal(group);
                return;
            }
            _ = playerRenderer.SetSlotSprites(group, addressTexture);
        }

        private PlayerRenderer.SlotRendererGroup[] GetRendererGroupBySlot(string slotId)
        {
            switch (slotId)
            {
                case "weapon_l": return playerRenderer.weapon_l;
                case "weapon_r": return playerRenderer.weapon_r;
                case "armor":    return playerRenderer.armor;
                case "cloth":    return playerRenderer.cloth;
                case "helmet":   return playerRenderer.helmet;
                case "foot":     return playerRenderer.foot;
                case "body":     return playerRenderer.body;
                case "pet":      return playerRenderer.pet;
                case "back":     return playerRenderer.back;
                default: return null;
            }
        }

        // Clear visuals for a slot group (when unequipped)
        private void ClearSlotSpritesLocal(PlayerRenderer.SlotRendererGroup[] group)
        {
            if (group == null || group.Length == 0) return;
            foreach (var slot in group)
            {
                if (slot == null || slot.renderers == null) continue;
                var sr = slot.renderers as SpriteRenderer;
                if (sr != null)
                {
                    sr.sprite = null;
                }
                else if (slot.renderers.material != null)
                {
                    slot.renderers.material.mainTexture = null;
                }
            }
        }

    }
}