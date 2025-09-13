using System;
using System.Collections.Generic;
using UnityEngine;
using Xianxia.Items;
using Xianxia.Stats;

namespace Xianxia.PlayerDataSystem
{
    // Legacy only for migration
    [Serializable]
    public class PlayerStatsLegacy
    {
        public float khiHuyet_toida;
        public float khiHuyet;
        public float linhLuc_toida;
        public float linhLuc;
        public float thoNguyen_toida;
        public float thoNguyen;
        public float tuVi;
        public float daoHanh;
        public float daoTam;
        public float ngoTinh;
        public float canCot;
        public float congVatLy;
        public float congPhapThuat;
        public float phongVatLy;
        public float phongPhapThuat;
        public float thanHon;
        public float tocDo;
        public float khiVan;
        public float nghiepLuc;
        public float InventorySize;
        public float tiLeBaoKich;
        public float satThuongBaoKich;
        public float hutMau;
        public float xuyenPhong;
        public float hoiPhuc;
    }

    [Serializable]
    public class InventoryItem : ItemData
    {
        public int Slot;
        public int quantity;
        public List<StatBonus> statBonuses = new List<StatBonus>();
    }

    [Serializable]
    public class EquipmentData : ISerializationCallbackReceiver
    {
        public event Action<string, InventoryItem> OnEquipped;
        public event Action<string, InventoryItem> OnUnequipped;

        [Serializable]
        public class Slot
        {
            public string idSlot;
            public InventoryItem item;
        }

        [SerializeField] private List<Slot> _slots = new List<Slot>();
        [NonSerialized] private Dictionary<string, InventoryItem> _map;
        public IReadOnlyList<Slot> Slots => _slots;

        public bool Equip(string slotId, InventoryItem item, bool overwrite = true)
        {
            if (_map == null) BuildMap();
            var slot = _slots.Find(s => s.idSlot == slotId);
            if (slot == null) { slot = new Slot { idSlot = slotId, item = null }; _slots.Add(slot); }
            if (!overwrite && slot.item != null) return false;
            slot.item = item;
            if (item == null) _map.Remove(slotId); else _map[slotId] = item;
            OnEquipped?.Invoke(slotId, item);
            return true;
        }

        public InventoryItem Unequip(string slotId)
        {
            if (_map == null) BuildMap();
            var slot = _slots.Find(s => s.idSlot == slotId);
            if (slot == null || slot.item == null) return null;
            var old = slot.item;
            slot.item = null;
            _map.Remove(slotId);
            OnUnequipped?.Invoke(slotId, old);
            return old;
        }

        public bool TryGet(string slotId, out InventoryItem item)
        {
            if (_map == null) BuildMap();
            return _map.TryGetValue(slotId, out item);
        }

        public void EnsureSlots(IEnumerable<string> slotIds)
        {
            bool changed = false;
            foreach (var id in slotIds)
            {
                if (!_slots.Exists(s => s.idSlot == id))
                {
                    _slots.Add(new Slot { idSlot = id, item = null });
                    changed = true;
                }
            }
            if (changed) BuildMap();
            else if (_map == null) BuildMap();
        }

        private void BuildMap()
        {
            _map = new Dictionary<string, InventoryItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in _slots)
            {
                if (!string.IsNullOrEmpty(s.idSlot))
                    _map[s.idSlot] = s.item;
            }
        }

        public void OnBeforeSerialize() { }
        public void OnAfterDeserialize() { BuildMap(); }
    }

    [Serializable]
    public class SkillData
    {
        public string id;
        public int level;
    }

    [Serializable]
    public class QuestData
    {
        public string id;
        public string status; // completed / in_progress / failed
    }

    [Serializable]
    public class SettingsData
    {
        public bool sound;
        public float musicVolume;
        public string lang;
    }

    [Serializable]
    public class MetaData
    {
        public string lastLogin;
        public int playTime; // seconds
    }

    [Serializable]
    public class PositionData
    {
        public string mapId;
        public float x;
        public float y;
    }
}
