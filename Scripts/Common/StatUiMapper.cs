using System;
using System.Collections.Generic;
using Xianxia.Stats;
using UnityEngine; // for Mathf

namespace Xianxia.UI
{
    /// <summary>
    /// Central mapping giữa các chuỗi stat UI ("hpMax","atk" ...) và StatId + quy tắc xử lý hiển thị/ phân bổ.
    /// Tránh lặp switch ở nhiều file UI.
    /// </summary>
    public static class StatUiMapper
    {
        private class StatUiDef
        {
            public StatId? Id;               // StatId tương ứng (null nếu là resource pair đặc biệt)
            public bool IsPercent;           // Hiển thị *100
            public float AllocateScale = 1f; // Mỗi điểm cộng bao nhiêu vào base (vd critRate 0.5%)
            public Func<StatCollection, float> CustomGetter; // Dùng cho các cặp hp/hpMax
            public Action<StatCollection, int> CustomAllocator; // Nếu cần xử lý đặc biệt khi cộng điểm
            public Func<StatCollection, string> CompositeFormatter; // Trả về chuỗi đặc biệt (cur/max)
        }

        private static readonly Dictionary<string, StatUiDef> _defs = new(StringComparer.OrdinalIgnoreCase)
        {
            {"hpMax", new StatUiDef{ Id = StatId.KhiHuyetMax, CompositeFormatter = s => $"{Mathf.RoundToInt(s.GetFinal(StatId.KhiHuyet))}/{Mathf.RoundToInt(s.GetFinal(StatId.KhiHuyetMax))}" }},
            {"qiMax", new StatUiDef{ Id = StatId.LinhLucMax, CompositeFormatter = s => $"{Mathf.RoundToInt(s.GetFinal(StatId.LinhLuc))}/{Mathf.RoundToInt(s.GetFinal(StatId.LinhLucMax))}" }},
            {"atk", new StatUiDef{ Id = StatId.CongVatLy }},
            {"def", new StatUiDef{ Id = StatId.PhongVatLy }},
            {"critRate", new StatUiDef{ Id = StatId.TiLeBaoKich, IsPercent = true, AllocateScale = 0.005f }}, // 0.5% mỗi điểm
            {"critDmg", new StatUiDef{ Id = StatId.SatThuongBaoKich, IsPercent = true, AllocateScale = 0.01f }},
            {"moveSpd", new StatUiDef{ Id = StatId.TocDo, AllocateScale = 0.2f }},
            {"hpRegen", new StatUiDef{ Id = StatId.HoiPhuc }},
            {"qiRegen", new StatUiDef{ Id = StatId.HoiPhuc }},
            {"lifesteal", new StatUiDef{ Id = StatId.HutMau, IsPercent = true, AllocateScale = 0.005f }},
            {"spellPower", new StatUiDef{ Id = StatId.CongPhapThuat }},
            {"spellResist", new StatUiDef{ Id = StatId.PhongPhapThuat }},
            {"dodge", new StatUiDef{ Id = StatId.TocDo, AllocateScale = 0.1f }}, // chưa có stat riêng
            {"pierce", new StatUiDef{ Id = StatId.XuyenPhong }},
        };

        public static bool TryGet(string uiId, out StatId id) { id = default; if (_defs.TryGetValue(uiId, out var d) && d.Id.HasValue){ id = d.Id.Value; return true; } return false; }

        public static float GetDisplayValue(StatCollection stats, string uiId)
        {
            if (stats == null) return 0;
            if (_defs.TryGetValue(uiId, out var d))
            {
                if (d.CustomGetter != null) return d.CustomGetter(stats);
                if (d.Id.HasValue)
                {
                    float v = stats.GetFinal(d.Id.Value);
                    if (d.IsPercent) v *= 100f;
                    return v;
                }
            }
            return 0;
        }

        /// <summary>
        /// Lấy chuỗi hiển thị. Nếu có CompositeFormatter thì dùng, nếu stat phần trăm thì format kèm %.
        /// Nếu không có composite: trả về số (int) hoặc xx.xx% tùy IsPercent.
        /// </summary>
        public static string GetDisplayString(StatCollection stats, string uiId)
        {
            if (stats == null) return "0";
            if (_defs.TryGetValue(uiId, out var d))
            {
                if (d.CompositeFormatter != null) return d.CompositeFormatter(stats);
                float val = GetDisplayValue(stats, uiId);
                if (d.IsPercent) return val.ToString("0.0") + "%";
                return Mathf.RoundToInt(val).ToString();
            }
            return "0";
        }

        public static void AllocatePoints(StatCollection stats, string uiId, int points)
        {
            if (stats == null || points <= 0) return;
            if (_defs.TryGetValue(uiId, out var d))
            {
                if (d.CustomAllocator != null) { d.CustomAllocator(stats, points); return; }
                if (d.Id.HasValue)
                {
                    float cur = stats.GetBase(d.Id.Value);
                    stats.SetBase(d.Id.Value, cur + points * d.AllocateScale);
                }
            }
        }
    }
}
