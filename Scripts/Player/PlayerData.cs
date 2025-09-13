using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Xianxia.Items;
using Xianxia.Stats;

namespace Xianxia.PlayerDataSystem
{

    // Các class phụ đã chuyển sang PlayerDataTypes.cs để gọn file.

    [Serializable]
    public class PlayerData
    {
        public string id = "User_001123"; 
        public string name = ""; // tên hiển thị
        public Realm realm = Realm.PhamNhan; // cảnh giới
        public float bac = 0; // bạc
        public float kim_ngan = 0; // kim ngân
        public float nguyen_bao = 0; // nguyên bảo
        public int InventorySize = 30;
    public int level = 1; // cấp độ nhân vật (cơ bản)

    // New extensible stats system
    public StatCollection stats = new StatCollection();
    // Legacy field for migrating older JSON (will be null for new saves)
    [SerializeField] private PlayerStatsLegacy legacyStats;
        public EquipmentData equipment = new EquipmentData();
        public List<InventoryItem> inventory = new List<InventoryItem>();
        public List<SkillData> skills = new List<SkillData>();
        public List<QuestData> quests = new List<QuestData>();
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
            string[] defaultSlots = { "weapon_l","weapon_r","armor","cloth","helmet","ring_r","ring_l","foot","body","pet","back" };
            data.equipment.EnsureSlots(defaultSlots);

            // Migration from legacy stats if present
            data.MigrateLegacyStatsIfNeeded();
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

        private void MigrateLegacyStatsIfNeeded()
        {
            if (legacyStats == null) return;
            // Map legacy to new stat ids (base values)
            stats.SetBase(StatId.KhiHuyetMax, legacyStats.khiHuyet_toida);
            stats.SetBase(StatId.KhiHuyet, legacyStats.khiHuyet);
            stats.SetBase(StatId.LinhLucMax, legacyStats.linhLuc_toida);
            stats.SetBase(StatId.LinhLuc, legacyStats.linhLuc);
            stats.SetBase(StatId.ThoNguyenMax, legacyStats.thoNguyen_toida);
            stats.SetBase(StatId.ThoNguyen, legacyStats.thoNguyen);
            stats.SetBase(StatId.TuVi, legacyStats.tuVi);
            stats.SetBase(StatId.DaoHanh, legacyStats.daoHanh);
            stats.SetBase(StatId.DaoTam, legacyStats.daoTam);
            stats.SetBase(StatId.NgoTinh, legacyStats.ngoTinh);
            stats.SetBase(StatId.CanCot, legacyStats.canCot);
            stats.SetBase(StatId.CongVatLy, legacyStats.congVatLy);
            stats.SetBase(StatId.CongPhapThuat, legacyStats.congPhapThuat);
            stats.SetBase(StatId.PhongVatLy, legacyStats.phongVatLy);
            stats.SetBase(StatId.PhongPhapThuat, legacyStats.phongPhapThuat);
            stats.SetBase(StatId.ThanHon, legacyStats.thanHon);
            stats.SetBase(StatId.TocDo, legacyStats.tocDo);
            stats.SetBase(StatId.KhiVan, legacyStats.khiVan);
            stats.SetBase(StatId.NghiepLuc, legacyStats.nghiepLuc);
            stats.SetBase(StatId.InventorySize, legacyStats.InventorySize);
            stats.SetBase(StatId.TiLeBaoKich, legacyStats.tiLeBaoKich);
            stats.SetBase(StatId.SatThuongBaoKich, legacyStats.satThuongBaoKich);
            stats.SetBase(StatId.HutMau, legacyStats.hutMau);
            stats.SetBase(StatId.XuyenPhong, legacyStats.xuyenPhong);
            stats.SetBase(StatId.HoiPhuc, legacyStats.hoiPhuc);
            // Clear legacy so it won't be serialized again
            legacyStats = null;
            // Immediately save to upgrade file format
            SaveForPlayer(id, true);
        }

    }
}
