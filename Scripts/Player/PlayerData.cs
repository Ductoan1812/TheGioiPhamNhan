using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Xianxia.Items;

namespace Xianxia.PlayerDataSystem
{
    [Serializable]
    public class PlayerInventoryAffix
    {
        public string id;
        public float value;
        public int tier;
    }

    [Serializable]
    public class PlayerInventoryItem
    {
        public string id;
        // Giữ chữ S hoa để khớp JSON và UI (1-based)
        public int Slot;
        public int quantity = 1;

        // Tuỳ chọn
        public int level;
        public PlayerInventoryAffix[] affixes = Array.Empty<PlayerInventoryAffix>();
    }

    [Serializable]
    public class PlayerData
    {
        // Định danh player (phục vụ nhặt item theo người)
        public string id = "player_001";

        // Số ô inventory
        public int InventorySize = 30;

        // Danh sách item
        public List<PlayerInventoryItem> inventory = new List<PlayerInventoryItem>();

        // ===== Helpers Inventory =====
        public PlayerInventoryItem GetBySlot(int slot)
        {
            return inventory.Find(x => x.Slot == slot);
        }

        public void SetSlot(PlayerInventoryItem entry)
        {
            if (entry == null) return;
            var idx = inventory.FindIndex(x => x.Slot == entry.Slot);
            if (idx >= 0) inventory[idx] = entry;
            else inventory.Add(entry);
        }

        public void RemoveSlot(int slot)
        {
            inventory.RemoveAll(x => x.Slot == slot);
        }

        // Thêm item theo maxStack/InventorySize. Trả về số còn lại không thể thêm.
        public int AddItem(string itemId, int quantity, ItemDatabaseSO db)
        {
            if (string.IsNullOrEmpty(itemId) || quantity <= 0) return quantity;

            int maxStack = 1;
            var def = db != null ? db.GetById(itemId) : null;
            if (def != null && def.maxStack > 0) maxStack = def.maxStack;

            int remain = quantity;

            // 1) Cộng vào stack sẵn có
            for (int i = 0; i < inventory.Count && remain > 0; i++)
            {
                var it = inventory[i];
                if (it.id != itemId) continue;
                int space = Mathf.Max(0, maxStack - it.quantity);
                if (space <= 0) continue;
                int move = Mathf.Min(space, remain);
                it.quantity += move;
                inventory[i] = it;
                remain -= move;
            }

            // 2) Thêm stack mới vào slot trống
            while (remain > 0)
            {
                int emptySlot = FindFirstEmptySlot();
                if (emptySlot < 1) break;

                int add = Mathf.Min(maxStack, remain);
                var newEntry = new PlayerInventoryItem
                {
                    id = itemId,
                    Slot = emptySlot,
                    quantity = add
                };
                inventory.Add(newEntry);
                remain -= add;
            }

            return remain;
        }

        // Tìm slot trống đầu tiên (1..InventorySize)
        public int FindFirstEmptySlot()
        {
            if (InventorySize <= 0) InventorySize = 30;
            var used = new bool[InventorySize + 1]; // bỏ qua index 0
            foreach (var it in inventory)
            {
                if (it.Slot >= 1 && it.Slot <= InventorySize)
                    used[it.Slot] = true;
            }
            for (int s = 1; s <= InventorySize; s++)
                if (!used[s]) return s;
            return -1;
        }

        // ===== IO: file duy nhất (InventoryUI dùng) =====
        public static string GetDefaultPath(string fileName = "PlayerData.json")
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        public static PlayerData LoadFromFile(string path = null)
        {
            path ??= GetDefaultPath();
            try
            {
                if (!File.Exists(path)) return new PlayerData();
                string json = File.ReadAllText(path);
                var data = string.IsNullOrWhiteSpace(json)
                    ? new PlayerData()
                    : JsonUtility.FromJson<PlayerData>(json) ?? new PlayerData();
                EnsureDefaults(data);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"PlayerData.LoadFromFile: {e.Message}");
                return new PlayerData();
            }
        }

        public static PlayerData LoadOrCreate(string path = null)
        {
            var d = LoadFromFile(path);
            EnsureDefaults(d);
            return d;
        }

        public bool SaveToFile(string path = null, bool prettyPrint = true)
        {
            path ??= GetDefaultPath();
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonUtility.ToJson(this, prettyPrint);
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"PlayerData.SaveToFile: {e.Message}");
                return false;
            }
        }

        // ===== IO: theo playerId (ItemPrefab dùng) =====
        private const string FilePrefix = "PlayerData_";
        private const string FileExt = ".json";
        private static readonly Dictionary<string, PlayerData> _cache = new Dictionary<string, PlayerData>();

        public static string GetPathForPlayer(string playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId)) playerId = "default";
            string file = $"{FilePrefix}{playerId}{FileExt}";
            return Path.Combine(Application.persistentDataPath, file);
        }

        public static PlayerData LoadForPlayer(string playerId)
        {
            string path = GetPathForPlayer(playerId);
            try
            {
                if (!File.Exists(path)) return NewDefault(playerId);
                string json = File.ReadAllText(path);
                var data = string.IsNullOrWhiteSpace(json)
                    ? NewDefault(playerId)
                    : JsonUtility.FromJson<PlayerData>(json) ?? NewDefault(playerId);
                EnsureDefaults(data, playerId);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"PlayerData.LoadForPlayer('{playerId}'): {e.Message}");
                return NewDefault(playerId);
            }
        }

        public bool SaveForPlayer(string playerId = null, bool prettyPrint = true)
        {
            if (!string.IsNullOrWhiteSpace(playerId)) id = playerId;
            string path = GetPathForPlayer(id);
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonUtility.ToJson(this, prettyPrint);
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"PlayerData.SaveForPlayer('{id}'): {e.Message}");
                return false;
            }
        }

        public static PlayerData GetForPlayer(string playerId, bool autoCreateIfMissing = true)
        {
            if (string.IsNullOrWhiteSpace(playerId)) playerId = "default";
            if (_cache.TryGetValue(playerId, out var d)) return d;

            var loaded = LoadForPlayer(playerId);
            if (loaded == null && autoCreateIfMissing)
            {
                loaded = NewDefault(playerId);
                loaded.SaveForPlayer(playerId);
            }

            EnsureDefaults(loaded, playerId);
            _cache[playerId] = loaded;
            return loaded;
        }

        // ===== Defaults =====
        private static void EnsureDefaults(PlayerData d, string playerId = null)
        {
            if (d == null) return;
            if (!string.IsNullOrWhiteSpace(playerId) && string.IsNullOrWhiteSpace(d.id)) d.id = playerId;
            d.inventory ??= new List<PlayerInventoryItem>();
            if (d.InventorySize <= 0) d.InventorySize = 30;
        }

        private static PlayerData NewDefault(string playerId)
        {
            return new PlayerData
            {
                id = playerId,
                InventorySize = 30,
                inventory = new List<PlayerInventoryItem>()
            };
        }
    }
}