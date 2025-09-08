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
            ApplyToPlayerData();
            onStatsLoaded?.Invoke();
            Debug.Log($"[PlayerEquitment] Loaded equipment for {data.id}");
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