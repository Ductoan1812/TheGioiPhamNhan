using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using Xianxia.PlayerDataSystem;
using Xianxia.Items;
using System;
using Xianxia.Stats;
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

            var svc = InventoryService.Instance;
            if (svc != null)
            {
                svc.OnEquipmentChanged -= HandleServiceEquipmentChanged;
            }

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
            var svc = InventoryService.Instance ?? InventoryService.EnsureInstance();
            svc.OnEquipmentChanged -= HandleServiceEquipmentChanged; // avoid double
            svc.OnEquipmentChanged += HandleServiceEquipmentChanged;
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
                // Debug log (optional):
                // Debug.Log($"[PlayerEquitment] Cleared old modifiers for slot {slotId}");
                if (item != null)
                {
                    // 1. statBonuses list (instance)
                    if (item.statBonuses != null)
                    {
                        foreach (var b in item.statBonuses)
                        {
                            stats.AddModifier(b.id, b.add, b.pct, slotId);
                        }
                    }

                    // 2. Merge baseStats: DB definition + instance override (additive)
                    var def = ItemDatabase != null ? ItemDatabase.GetById(item.id) : null;
                    var dbBs = def?.baseStats;
                    var instBs = item.baseStats;
                    float atk = (dbBs?.atk ?? 0) + (instBs?.atk ?? 0);
                    float defPhys = (dbBs?.defense ?? 0) + (instBs?.defense ?? 0);
                    float hp = (dbBs?.hp ?? 0) + (instBs?.hp ?? 0);
                    float qi = (dbBs?.qi ?? 0) + (instBs?.qi ?? 0);
                    float moveSpd = (dbBs?.moveSpd ?? 0) + (instBs?.moveSpd ?? 0);
                    float critRate = (dbBs?.critRate ?? 0) + (instBs?.critRate ?? 0);
                    float critDmg = (dbBs?.critDmg ?? 0) + (instBs?.critDmg ?? 0);
                    float penetration = (dbBs?.penetration ?? 0) + (instBs?.penetration ?? 0);
                    float lifestealQi = (dbBs?.lifestealQi ?? 0) + (instBs?.lifestealQi ?? 0);
                    float resTotal = 0;
                    if (dbBs?.res != null)
                        resTotal += dbBs.res.kim + dbBs.res.moc + dbBs.res.thuy + dbBs.res.hoa + dbBs.res.tho + dbBs.res.loi + dbBs.res.am + dbBs.res.duong;
                    if (instBs?.res != null)
                        resTotal += instBs.res.kim + instBs.res.moc + instBs.res.thuy + instBs.res.hoa + instBs.res.tho + instBs.res.loi + instBs.res.am + instBs.res.duong;

                    if (atk > 0) stats.AddModifier(StatId.CongVatLy, atk, 0, slotId);
                    if (defPhys > 0) stats.AddModifier(StatId.PhongVatLy, defPhys, 0, slotId);
                    if (hp > 0) stats.AddModifier(StatId.KhiHuyetMax, hp, 0, slotId);
                    if (qi > 0) stats.AddModifier(StatId.LinhLucMax, qi, 0, slotId);
                    if (moveSpd > 0) stats.AddModifier(StatId.TocDo, moveSpd, 0, slotId);
                    if (critRate > 0) stats.AddModifier(StatId.TiLeBaoKich, critRate, 0, slotId);
                    if (critDmg > 0) stats.AddModifier(StatId.SatThuongBaoKich, critDmg, 0, slotId);
                    if (penetration > 0) stats.AddModifier(StatId.XuyenPhong, penetration, 0, slotId);
                    if (lifestealQi > 0) stats.AddModifier(StatId.HutMau, lifestealQi, 0, slotId);
                    if (resTotal > 0) stats.AddModifier(StatId.PhongPhapThuat, resTotal, 0, slotId);

                    // 3. Affixes: merge DB + instance
                    if (def?.affixes != null)
                        ApplyAffixArray(def.affixes, stats, slotId);
                    if (item.affixes != null)
                        ApplyAffixArray(item.affixes, stats, slotId);
                }
            }
            RefreshSlotVisual(slotId, item);
        }

        private void ApplyAffixArray(Xianxia.Items.AffixEntry[] arr, Xianxia.Stats.StatCollection stats, string slotSource)
        {
            if (arr == null) return;
            foreach (var af in arr)
            {
                if (af == null) continue;
                string idLower = af.id?.ToLowerInvariant();
                if (string.IsNullOrEmpty(idLower)) continue;
                float v = af.value;
                if (v == 0) continue;
                if (idLower.Contains("atk")) stats.AddModifier(StatId.CongVatLy, v, 0, slotSource);
                else if (idLower.Contains("def")) stats.AddModifier(StatId.PhongVatLy, v, 0, slotSource);
                else if (idLower.Contains("hp")) stats.AddModifier(StatId.KhiHuyetMax, v, 0, slotSource);
                else if (idLower.Contains("qi")) stats.AddModifier(StatId.LinhLucMax, v, 0, slotSource);
                else if (idLower.Contains("critdmg")) stats.AddModifier(StatId.SatThuongBaoKich, v, 0, slotSource);
                else if (idLower.Contains("crit")) stats.AddModifier(StatId.TiLeBaoKich, v, 0, slotSource);
                else if (idLower.Contains("spd")) stats.AddModifier(StatId.TocDo, v, 0, slotSource);
                else if (idLower.Contains("pierce") || idLower.Contains("penetr")) stats.AddModifier(StatId.XuyenPhong, v, 0, slotSource);
                else if (idLower.Contains("lifesteal")) stats.AddModifier(StatId.HutMau, v, 0, slotSource);
                else if (idLower.Contains("regen")) stats.AddModifier(StatId.HoiPhuc, v, 0, slotSource);
            }
        }

        private void HandleServiceEquipmentChanged(string slotId, InventoryItem newItem, InventoryItem oldItem)
        {
            // Mirror service event to existing handler logic
            OnEquipmentChanged(slotId, newItem);
        }
        private void ReapplyAllModifiers()
        {
            if (Equipment == null || stats == null) return;
            // Clear all existing equipment-sourced modifiers first
            foreach (var s in Equipment.Slots)
            {
                if (!string.IsNullOrEmpty(s.idSlot)) stats.RemoveModifiersBySource(s.idSlot);
            }
            // Reapply full logic for each equipped item (mirror OnEquipmentChanged)
            foreach (var s in Equipment.Slots)
            {
                if (string.IsNullOrEmpty(s.idSlot)) continue;
                var item = s.item;
                if (item == null) continue;

                // 1. statBonuses list (instance)
                if (item.statBonuses != null)
                {
                    foreach (var b in item.statBonuses)
                    {
                        stats.AddModifier(b.id, b.add, b.pct, s.idSlot);
                    }
                }

                // 2. Merge baseStats: DB definition + instance override (additive)
                var def = ItemDatabase != null ? ItemDatabase.GetById(item.id) : null;
                var dbBs = def?.baseStats;
                var instBs = item.baseStats;
                float atk = (dbBs?.atk ?? 0) + (instBs?.atk ?? 0);
                float defPhys = (dbBs?.defense ?? 0) + (instBs?.defense ?? 0);
                float hp = (dbBs?.hp ?? 0) + (instBs?.hp ?? 0);
                float qi = (dbBs?.qi ?? 0) + (instBs?.qi ?? 0);
                float moveSpd = (dbBs?.moveSpd ?? 0) + (instBs?.moveSpd ?? 0);
                float critRate = (dbBs?.critRate ?? 0) + (instBs?.critRate ?? 0);
                float critDmg = (dbBs?.critDmg ?? 0) + (instBs?.critDmg ?? 0);
                float penetration = (dbBs?.penetration ?? 0) + (instBs?.penetration ?? 0);
                float lifestealQi = (dbBs?.lifestealQi ?? 0) + (instBs?.lifestealQi ?? 0);
                float resTotal = 0;
                if (dbBs?.res != null)
                    resTotal += dbBs.res.kim + dbBs.res.moc + dbBs.res.thuy + dbBs.res.hoa + dbBs.res.tho + dbBs.res.loi + dbBs.res.am + dbBs.res.duong;
                if (instBs?.res != null)
                    resTotal += instBs.res.kim + instBs.res.moc + instBs.res.thuy + instBs.res.hoa + instBs.res.tho + instBs.res.loi + instBs.res.am + instBs.res.duong;

                if (atk > 0) stats.AddModifier(StatId.CongVatLy, atk, 0, s.idSlot);
                if (defPhys > 0) stats.AddModifier(StatId.PhongVatLy, defPhys, 0, s.idSlot);
                if (hp > 0) stats.AddModifier(StatId.KhiHuyetMax, hp, 0, s.idSlot);
                if (qi > 0) stats.AddModifier(StatId.LinhLucMax, qi, 0, s.idSlot);
                if (moveSpd > 0) stats.AddModifier(StatId.TocDo, moveSpd, 0, s.idSlot);
                if (critRate > 0) stats.AddModifier(StatId.TiLeBaoKich, critRate, 0, s.idSlot);
                if (critDmg > 0) stats.AddModifier(StatId.SatThuongBaoKich, critDmg, 0, s.idSlot);
                if (penetration > 0) stats.AddModifier(StatId.XuyenPhong, penetration, 0, s.idSlot);
                if (lifestealQi > 0) stats.AddModifier(StatId.HutMau, lifestealQi, 0, s.idSlot);
                if (resTotal > 0) stats.AddModifier(StatId.PhongPhapThuat, resTotal, 0, s.idSlot);

                // 3. Affixes: merge DB + instance
                if (def?.affixes != null) ApplyAffixArray(def.affixes, stats, s.idSlot);
                if (item.affixes != null) ApplyAffixArray(item.affixes, stats, s.idSlot);
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