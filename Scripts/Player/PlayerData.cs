using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Xianxia.Items;

namespace Xianxia.PlayerDataSystem
{

    [Serializable]
    public class PlayerStats
    {
        public float hp;
        public float qi;
        public float atk;
        public float def;
        public float critRate;
        public float moveSpd;
    }
    [Serializable]
    public class InventoryItem : ItemData
    {
        public int Slot;
        public int quantity;

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
    public class CurrencyData
    {
        public int gold;
        public int spirit_stone;
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
        public int playTime; // giây
    }

    [Serializable]
    public class PositionData
    {
        public string mapId;
        public float x;
        public float y;
    }

    [Serializable]
    public class PlayerData
    {
        public string id = "User_001123";
        public string name = "Đức Toàn";
        public int level = 1;
        public int exp = 0;
        public string realm = "truc_co";

        public int InventorySize = 30;

        public PlayerStats stats = new PlayerStats();
        public EquipmentData equipment = new EquipmentData();
        public List<InventoryItem> inventory = new List<InventoryItem>();
        public List<SkillData> skills = new List<SkillData>();
        public List<QuestData> quests = new List<QuestData>();
        public CurrencyData currency = new CurrencyData();
        public SettingsData settings = new SettingsData();
        public MetaData meta = new MetaData();
        public PositionData position = new PositionData();

        // ===== IO =====
        private const string FilePrefix = "";
        private const string FileExt = ".json";

        public static string GetPathForPlayer(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) playerId = "default";
            string file = $"{FilePrefix}{playerId}{FileExt}";
            return Path.Combine(Application.persistentDataPath, file);
        }

        public static PlayerData LoadForPlayer(string playerId)
        {
            string path = GetPathForPlayer(playerId);
            PlayerData data;

            if (!File.Exists(path))
            {
                data = new PlayerData { id = playerId };
            }
            else
            {
                string json = File.ReadAllText(path);
                // Try parse, attempt minimal fix if corrupted, else fallback to new data
                try
                {
                    data = JsonUtility.FromJson<PlayerData>(json);
                    if (data == null) throw new Exception("Parsed null PlayerData");
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[PlayerData] Failed to parse save JSON: {ex.Message}. Attempting cleanup...");
                    if (TryFixJson(json, out var fixedJson))
                    {
                        try
                        {
                            data = JsonUtility.FromJson<PlayerData>(fixedJson);
                        }
                        catch
                        {
                            data = null;
                        }
                    }
                    else data = null;

                    if (data == null)
                    {
                        // Backup bad file and create fresh data
                        try
                        {
                            string backup = path + ".bak_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                            File.Copy(path, backup, overwrite: true);
                            Debug.LogWarning($"[PlayerData] Backed up corrupted save to {backup}");
                        }
                        catch { /* ignore backup failure */ }

                        data = new PlayerData { id = playerId };
                    }
                }
            }

            // Danh sách slot mặc định (tùy chỉnh theo game)
            string[] defaultSlots = { "weapon_l","weapon_r","armor","cloth","helmet","ring_r","ring_l","foot","body","pet" };
            data.equipment.EnsureSlots(defaultSlots);

            return data;
        }

        // Remove BOM/whitespace and clip to the first '{' .. last '}' block
        private static bool TryFixJson(string raw, out string fixedJson)
        {
            fixedJson = null;
            if (string.IsNullOrWhiteSpace(raw)) return false;
            // Trim BOM if present
            raw = raw.Trim();
            if (raw.Length > 0 && raw[0] == '\uFEFF') raw = raw.Substring(1);
            int start = raw.IndexOf('{');
            int end = raw.LastIndexOf('}');
            if (start >= 0 && end >= start)
            {
                fixedJson = raw.Substring(start, end - start + 1);
                return true;
            }
            return false;
        }

        public void SaveForPlayer(string playerId = null, bool prettyPrint = true)
        {
            if (!string.IsNullOrWhiteSpace(playerId))
                id = playerId;

            string path = GetPathForPlayer(id);
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            string json = JsonUtility.ToJson(this, prettyPrint);
            File.WriteAllText(path, json);
        }
    }
}
