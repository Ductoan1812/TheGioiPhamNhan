using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
    public class ItemStat
    {
        public string id;
        public float value;
    }

    [Serializable]
    public class ItemProp
    {
        public string key;
        public string value;
    }

    [Serializable]
    public class PlayerInventoryItem
    {
        public string id;
        // Slot 1-based để khớp UI/JSON
        public int Slot;
        public int quantity = 1;

        // Biến thể phổ quát
        public int level;
        public string rarity;                 // vd: common/rare/epic...
        public string element;                // vd: fire/ice/lightning...
        public int quality;                   // 0..100 (tuỳ game)
        public float durability = 1f;         // 0..1 hoặc giá trị tuyệt đối

        public ItemStat[] baseStats = Array.Empty<ItemStat>();            // stat gốc (atk/def/speed...)
        public PlayerInventoryAffix[] affixes = Array.Empty<PlayerInventoryAffix>(); // affix ngẫu nhiên
        public string[] tags = Array.Empty<string>();                     // nhãn biến thể (seasonal/limited…)
        public ItemProp[] custom = Array.Empty<ItemProp>();               // thuộc tính tuỳ ý (dạng key/value)

        // Tạo bản sao "instance" để thao tác an toàn
        public PlayerInventoryItem Clone()
        {
            return new PlayerInventoryItem
            {
                id = id,
                Slot = Slot,
                quantity = quantity,
                level = level,
                rarity = rarity,
                element = element,
                quality = quality,
                durability = durability,
                baseStats = CloneBaseStats(baseStats),
                affixes = CloneAffixes(affixes),
                tags = CloneStringArray(tags),
                custom = CloneProps(custom),
            };
        }

        public static ItemStat[] CloneBaseStats(ItemStat[] arr)
        {
            if (arr == null || arr.Length == 0) return Array.Empty<ItemStat>();
            var outArr = new ItemStat[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                outArr[i] = new ItemStat { id = arr[i].id, value = arr[i].value };
            }
            return outArr;
        }

        public static PlayerInventoryAffix[] CloneAffixes(PlayerInventoryAffix[] arr)
        {
            if (arr == null || arr.Length == 0) return Array.Empty<PlayerInventoryAffix>();
            var outArr = new PlayerInventoryAffix[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                var a = arr[i];
                outArr[i] = new PlayerInventoryAffix { id = a.id, tier = a.tier, value = a.value };
            }
            return outArr;
        }

        public static string[] CloneStringArray(string[] arr)
        {
            if (arr == null || arr.Length == 0) return Array.Empty<string>();
            var copy = new string[arr.Length];
            Array.Copy(arr, copy, arr.Length);
            return copy;
        }

        public static ItemProp[] CloneProps(ItemProp[] arr)
        {
            if (arr == null || arr.Length == 0) return Array.Empty<ItemProp>();
            var outArr = new ItemProp[arr.Length];
            for (int i = 0; i < arr.Length; i++)
            {
                var p = arr[i];
                outArr[i] = new ItemProp { key = p.key, value = p.value };
            }
            return outArr;
        }
    }

    [Serializable]
    public class PlayerData
    {
        public string id = "player_001";
        public int InventorySize = 30;
        public List<PlayerInventoryItem> inventory = new List<PlayerInventoryItem>();

        // Sự kiện để UI nghe và refresh
        public static event Action<string> OnInventoryChanged;
        public static void RaiseInventoryChanged(string playerId)
        {
            OnInventoryChanged?.Invoke(string.IsNullOrWhiteSpace(playerId) ? "default" : playerId);
        }

        // ===== Query/Set/Remove theo Slot =====
        public PlayerInventoryItem GetBySlot(int slot) => inventory.Find(x => x.Slot == slot);

        public void SetSlot(PlayerInventoryItem entry)
        {
            if (entry == null) return;
            var idx = inventory.FindIndex(x => x.Slot == entry.Slot);
            if (idx >= 0) inventory[idx] = entry;
            else inventory.Add(entry);
            RaiseInventoryChanged(id);
        }

        public void RemoveSlot(int slot)
        {
            if (inventory.RemoveAll(x => x.Slot == slot) > 0)
                RaiseInventoryChanged(id);
        }

        // ===== Thêm item: full-variant aware =====
        // Legacy: chỉ id/quantity (biến thể mặc định)
        public int AddItem(string itemId, int quantity, ItemDatabaseSO db)
        {
            return AddItem(new PlayerInventoryItem { id = itemId, quantity = quantity }, db);
        }

        // API tổng quát: Add theo instance (có đầy đủ biến thể)
        public int AddItem(PlayerInventoryItem incoming, ItemDatabaseSO db)
        {
            if (incoming == null || string.IsNullOrEmpty(incoming.id) || incoming.quantity <= 0)
                return incoming?.quantity ?? 0;

            // maxStack xác định khả năng gộp
            int maxStack = 1;
            var def = db != null ? db.GetById(incoming.id) : null;
            if (def != null && def.maxStack > 0) maxStack = def.maxStack;

            // Không gộp được => mỗi cái 1 stack
            if (maxStack <= 1)
            {
                int remain = incoming.quantity;
                while (remain > 0)
                {
                    int slot = FindFirstEmptySlot();
                    if (slot < 1) break;
                    var one = incoming.Clone();
                    one.Slot = slot;
                    one.quantity = 1;
                    inventory.Add(one);
                    remain -= 1;
                }
                if (incoming.quantity > 0) RaiseInventoryChanged(id);
                return remain;
            }

            // 1) Cộng vào stack sẵn có cùng khóa biến thể
            int remainQty = incoming.quantity;
            for (int i = 0; i < inventory.Count && remainQty > 0; i++)
            {
                var it = inventory[i];
                if (!CanStackWith(it, incoming, db)) continue;

                int space = Mathf.Max(0, maxStack - it.quantity);
                if (space <= 0) continue;

                int move = Mathf.Min(space, remainQty);
                it.quantity += move;
                inventory[i] = it;
                remainQty -= move;
            }

            // 2) Tạo stack mới cho phần còn lại
            while (remainQty > 0)
            {
                int slot = FindFirstEmptySlot();
                if (slot < 1) break;

                int add = Mathf.Min(maxStack, remainQty);
                var newEntry = incoming.Clone();
                newEntry.Slot = slot;
                newEntry.quantity = add;
                inventory.Add(newEntry);
                remainQty -= add;
            }

            if (incoming.quantity > 0) RaiseInventoryChanged(id);
            return remainQty;
        }

        // Gom stack theo khóa biến thể (id + signature biến thể) và maxStack
        public bool NormalizeStacks(ItemDatabaseSO db)
        {
            if (db == null) return false;

            bool changed = false;
            var groups = inventory.GroupBy(GetVariantKey).ToList();
            var newList = new List<PlayerInventoryItem>();

            foreach (var g in groups)
            {
                var items = g.ToList();
                if (items.Count == 0) continue;

                var def = db.GetById(items[0].id);
                int maxStack = (def != null && def.maxStack > 0) ? def.maxStack : 1;

                if (maxStack <= 1)
                {
                    // Mỗi cái 1 stack
                    foreach (var x in items)
                    {
                        if (x.quantity != 1)
                        {
                            for (int i = 0; i < x.quantity; i++)
                            {
                                var one = x.Clone();
                                one.quantity = 1;
                                newList.Add(one);
                            }
                            changed = true;
                        }
                        else newList.Add(x.Clone());
                    }
                }
                else
                {
                    int total = items.Sum(x => x.quantity);
                    while (total > 0)
                    {
                        int take = Mathf.Min(maxStack, total);
                        var baseItem = items[0].Clone();
                        baseItem.quantity = take;
                        baseItem.Slot = -1;
                        newList.Add(baseItem);
                        total -= take;
                    }
                    if (items.Count != newList.Count) changed = true;
                }
            }

            if (!SequenceEqualByContent(inventory, newList))
                changed = true;

            inventory = newList;
            RelayoutSlots();
            if (changed) RaiseInventoryChanged(id);
            return changed;
        }

        private void RelayoutSlots()
        {
            int s = 1;
            foreach (var it in inventory.OrderBy(x => x.Slot))
            {
                if (s > InventorySize) break;
                it.Slot = s++;
            }
        }

        private static bool SequenceEqualByContent(List<PlayerInventoryItem> a, List<PlayerInventoryItem> b)
        {
            if (a.Count != b.Count) return false;
            for (int i = 0; i < a.Count; i++)
            {
                var A = a[i]; var B = b[i];
                if (A.id != B.id) return false;
                if (A.level != B.level) return false;
                if (A.rarity != B.rarity) return false;
                if (A.element != B.element) return false;
                if (A.quality != B.quality) return false;
                if (!FloatEqual(A.durability, B.durability)) return false;
                if (A.quantity != B.quantity) return false;
                if (AffixFingerprint(A.affixes) != AffixFingerprint(B.affixes)) return false;
                if (StatsFingerprint(A.baseStats) != StatsFingerprint(B.baseStats)) return false;
                if (StringArrayFingerprint(A.tags) != StringArrayFingerprint(B.tags)) return false;
                if (PropsFingerprint(A.custom) != PropsFingerprint(B.custom)) return false;
            }
            return true;
        }

        // Slot trống đầu tiên (1..InventorySize)
        public int FindFirstEmptySlot()
        {
            if (InventorySize <= 0) InventorySize = 30;
            var used = new bool[InventorySize + 1];
            foreach (var it in inventory)
                if (it.Slot >= 1 && it.Slot <= InventorySize)
                    used[it.Slot] = true;

            for (int s = 1; s <= InventorySize; s++)
                if (!used[s]) return s;
            return -1;
        }

        // ===== IO theo playerId =====
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

        // ===== So sánh/fingerprint biến thể =====
        private static string GetVariantKey(PlayerInventoryItem it)
        {
            // Trả về signature duy nhất cho biến thể (không gồm quantity/slot)
            return string.Join("|", new[]
            {
                $"id:{it.id}",
                $"lvl:{it.level}",
                $"rar:{it.rarity ?? ""}",
                $"ele:{it.element ?? ""}",
                $"qual:{it.quality}",
                $"dur:{RoundF(it.durability)}",
                $"aff:{AffixFingerprint(it.affixes)}",
                $"bst:{StatsFingerprint(it.baseStats)}",
                $"tag:{StringArrayFingerprint(it.tags)}",
                $"cst:{PropsFingerprint(it.custom)}",
            });
        }

        private static bool CanStackWith(PlayerInventoryItem a, PlayerInventoryItem b, ItemDatabaseSO db)
        {
            if (a == null || b == null) return false;
            if (!string.Equals(a.id, b.id, StringComparison.Ordinal)) return false;

            // Item phải cho phép stack
            var def = db?.GetById(a.id);
            int maxStack = (def != null && def.maxStack > 0) ? def.maxStack : 1;
            if (maxStack <= 1) return false;

            return GetVariantKey(a) == GetVariantKey(b);
        }

        private static string AffixFingerprint(PlayerInventoryAffix[] arr)
        {
            if (arr == null || arr.Length == 0) return "-";
            var list = new List<string>(arr.Length);
            foreach (var a in arr)
                list.Add($"{a.id}#{a.tier}#{a.value.ToString(CultureInfo.InvariantCulture)}");
            list.Sort(StringComparer.Ordinal);
            return string.Join(",", list);
        }

        private static string StatsFingerprint(ItemStat[] arr)
        {
            if (arr == null || arr.Length == 0) return "-";
            var list = new List<string>(arr.Length);
            foreach (var s in arr)
                list.Add($"{s.id}#{s.value.ToString(CultureInfo.InvariantCulture)}");
            list.Sort(StringComparer.Ordinal);
            return string.Join(",", list);
        }

        private static string StringArrayFingerprint(string[] arr)
        {
            if (arr == null || arr.Length == 0) return "-";
            var copy = new List<string>(arr);
            copy.Sort(StringComparer.Ordinal);
            return string.Join(",", copy);
        }

        private static string PropsFingerprint(ItemProp[] arr)
        {
            if (arr == null || arr.Length == 0) return "-";
            var list = new List<string>(arr.Length);
            foreach (var p in arr)
                list.Add($"{p.key}={p.value}");
            list.Sort(StringComparer.Ordinal);
            return string.Join("&", list);
        }

        private static bool FloatEqual(float a, float b) => Mathf.Abs(a - b) <= 1e-5f;
        private static string RoundF(float v) => v.ToString("0.#####", CultureInfo.InvariantCulture);

        // ===== Defaults =====
        private static void EnsureDefaults(PlayerData d, string playerId)
        {
            d.id = string.IsNullOrWhiteSpace(d.id) ? (string.IsNullOrWhiteSpace(playerId) ? "default" : playerId) : d.id;
            d.inventory ??= new List<PlayerInventoryItem>();
            if (d.InventorySize <= 0) d.InventorySize = 30;
        }

        private static PlayerData NewDefault(string playerId)
        {
            return new PlayerData
            {
                id = string.IsNullOrWhiteSpace(playerId) ? "default" : playerId,
                InventorySize = 30,
                inventory = new List<PlayerInventoryItem>()
            };
        }
    }
}