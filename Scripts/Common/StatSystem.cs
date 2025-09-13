using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Xianxia.Stats
{
    public enum StatCategory
    {
        Survival,  // chỉ số sinh tồn (HP, MP, Stamina, ...)
        Cultivation,  // chỉ số tu luyện (Kinh nghiệm, Tiến độ, ...)
        Combat,  // chỉ số chiến đấu (Sát thương, Phòng thủ, ...)
        Special,  // chỉ số đặc biệt (Kỹ năng, Phép thuật, ...)
        Extended  // chỉ số mở rộng (Tùy chỉnh, Nâng cấp, ...)
    }

    [Flags]
    public enum StatFlags // 
    {
        None = 0,
        ResourceMax = 1 << 0,
        ResourceCurrent = 1 << 1,
        Percentage = 1 << 2,
        Derived = 1 << 3
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class StatMetaAttribute : Attribute
    {
        public StatCategory Category;
        public string DisplayName;
        public StatFlags Flags;
        public StatMetaAttribute(StatCategory category, string displayName, StatFlags flags = StatFlags.None)
        {
            Category = category;
            DisplayName = displayName;
            Flags = flags;
        }
    }

    public enum StatId
    {
        [StatMeta(StatCategory.Survival, "Khí huyết tối đa", StatFlags.ResourceMax)]
        KhiHuyetMax,
        [StatMeta(StatCategory.Survival, "Khí huyết", StatFlags.ResourceCurrent)]
        KhiHuyet,
        [StatMeta(StatCategory.Survival, "Linh lực tối đa", StatFlags.ResourceMax)]
        LinhLucMax,
        [StatMeta(StatCategory.Survival, "Linh lực", StatFlags.ResourceCurrent)]
        LinhLuc,
        [StatMeta(StatCategory.Survival, "Thọ nguyên tối đa", StatFlags.ResourceMax)]
        ThoNguyenMax,
        [StatMeta(StatCategory.Survival, "Thọ nguyên", StatFlags.ResourceCurrent)]
        ThoNguyen,
        [StatMeta(StatCategory.Cultivation, "Tu vi")]
        TuVi,
        [StatMeta(StatCategory.Cultivation, "Đạo hạnh")]
        DaoHanh,
        [StatMeta(StatCategory.Cultivation, "Đạo tâm")]
        DaoTam,
        [StatMeta(StatCategory.Cultivation, "Ngộ tính")]
        NgoTinh,
        [StatMeta(StatCategory.Cultivation, "Căn cốt")]
        CanCot,
        [StatMeta(StatCategory.Combat, "Công vật lý")]
        CongVatLy,
        [StatMeta(StatCategory.Combat, "Công pháp thuật")]
        CongPhapThuat,
        [StatMeta(StatCategory.Combat, "Phòng vật lý")]
        PhongVatLy,
        [StatMeta(StatCategory.Combat, "Kháng pháp thuật")]
        PhongPhapThuat,
        [StatMeta(StatCategory.Combat, "Thần hồn")]
        ThanHon,
        [StatMeta(StatCategory.Combat, "Tốc độ")]
        TocDo,
        [StatMeta(StatCategory.Special, "Khí vận")]
        KhiVan,
        [StatMeta(StatCategory.Special, "Nghiệp lực")]
        NghiepLuc,
        [StatMeta(StatCategory.Special, "Sức chứa kho")]
        InventorySize,
        [StatMeta(StatCategory.Extended, "Tỉ lệ bạo kích", StatFlags.Percentage)]
        TiLeBaoKich,
        [StatMeta(StatCategory.Extended, "Sát thương bạo kích", StatFlags.Percentage)]
        SatThuongBaoKich,
        [StatMeta(StatCategory.Extended, "Hút máu", StatFlags.Percentage)]
        HutMau,
        [StatMeta(StatCategory.Extended, "Xuyên phòng")]
        XuyenPhong,
        [StatMeta(StatCategory.Extended, "Hồi phục")]
        HoiPhuc,
        [StatMeta(StatCategory.Cultivation, "Tu vi cần")]
        TuViCan,
    [StatMeta(StatCategory.Extended, "Điểm chưa phân")]
    Points,
    }

    [Serializable]
    public struct StatEntry
    {
        public StatId id;
        public float baseValue;
    }

    public class StatModifier
    {
        public StatId id;
        public float add;
        public float pct;
        public object source;
    }

    [Serializable]
    public class StatCollection : ISerializationCallbackReceiver
    {
        [SerializeField] private List<StatEntry> _serialized = new();
        private Dictionary<StatId, float> _base;
        private Dictionary<StatId, List<StatModifier>> _modifiers;

        public event Action<StatId, float> OnBaseChanged;
        public event Action<StatId, float> OnFinalChanged;

        private static Dictionary<StatId, StatMetaAttribute> _metaCache;
        public static StatMetaAttribute GetMeta(StatId id)
        {
            if (_metaCache == null)
            {
                _metaCache = new();
                foreach (var f in typeof(StatId).GetFields(BindingFlags.Public | BindingFlags.Static))
                {
                    if (f.GetCustomAttribute(typeof(StatMetaAttribute)) is StatMetaAttribute attr)
                        _metaCache[(StatId)f.GetValue(null)] = attr;
                }
            }
            return _metaCache.TryGetValue(id, out var m) ? m : null;
        }

        // Danh sách toàn bộ StatId hiện có trong collection (base đã khởi tạo)
        public IEnumerable<StatId> AllIds => _base.Keys;

        /// <summary>
        /// Lấy giá trị base (chưa cộng modifier). Nếu stat chưa tồn tại sẽ trả 0.
        /// Dùng khi cần xử lý logic so với giá trị gốc (ví dụ so sánh trước / sau buff).
        /// </summary>
        public float GetBase(StatId id) => _base.TryGetValue(id, out var v) ? v : 0f;

        /// <summary>
        /// Gán giá trị base cho một stat. Chỉ bắn sự kiện khi giá trị thay đổi thực sự.
        /// Sau khi gán sẽ tự động thông báo OnBaseChanged và OnFinalChanged.
        /// </summary>
        public void SetBase(StatId id, float value)
        {
            if (!_base.ContainsKey(id) || !Mathf.Approximately(_base[id], value))
            {
                _base[id] = value;
                OnBaseChanged?.Invoke(id, value);
                RaiseFinalChanged(id);
            }
        }

        /// <summary>
        /// Trả về giá trị cuối (final) = (Base + tổng Add) * (1 + tổng Percent).
        /// Nếu không có modifier nào -> trả base.
        /// Quy tắc: cộng dồn toàn bộ modifier cộng thẳng (add) trước rồi nhân một lần với tổng phần trăm.
        /// Nếu muốn phức tạp hơn (ví dụ nhiều tầng % khác nhau) có thể mở rộng sau.
        /// </summary>
        public float GetFinal(StatId id)
        {
            float val = GetBase(id);
            if (_modifiers.TryGetValue(id, out var list) && list.Count > 0)
            {
                float add = 0f;
                float pct = 0f;
                foreach (var m in list)
                {
                    add += m.add;
                    pct += m.pct;
                }
                val = (val + add) * (1f + pct);
            }
            return val;
        }

        /// <summary>
        /// Thêm một modifier cho stat:
        ///  - add: giá trị cộng thẳng (ví dụ +15 ATK)
        ///  - pct: phần trăm cộng thêm (0.10 = +10%) áp dụng sau khi cộng add
        ///  - source: đối tượng nguồn (item, buff, skill) để có thể gỡ đồng loạt sau này.
        /// Trả về đối tượng StatModifier nếu muốn giữ tham chiếu để remove riêng lẻ sau.
        /// </summary>
        public StatModifier AddModifier(StatId id, float add = 0f, float pct = 0f, object source = null)
        {
            var mod = new StatModifier { id = id, add = add, pct = pct, source = source };
            if (!_modifiers.TryGetValue(id, out var list))
            {
                list = new List<StatModifier>();
                _modifiers[id] = list;
            }
            list.Add(mod);
            RaiseFinalChanged(id);
            return mod;
        }

        /// <summary>
        /// Gỡ toàn bộ modifier có cùng source (ví dụ: một trang bị bị tháo ra, buff hết thời gian).
        /// Nếu nhiều stat bị ảnh hưởng sẽ bắn sự kiện thay đổi final cho từng stat.
        /// </summary>
        public void RemoveModifiersBySource(object source)
        {
            if (source == null) return;
            var changed = new HashSet<StatId>();
            foreach (var kv in _modifiers)
            {
                int removed = kv.Value.RemoveAll(m =>
                    m.source == source ||
                    (source is string s && m.source is string ms && string.Equals(s, ms, StringComparison.OrdinalIgnoreCase))
                );
                if (removed > 0) changed.Add(kv.Key);
            }
            foreach (var id in changed) RaiseFinalChanged(id);
        }

        /// <summary>
        /// Kích hoạt sự kiện OnFinalChanged với giá trị final hiện tại.
        /// Gọi mỗi khi base hoặc modifier thay đổi.
        /// </summary>
        private void RaiseFinalChanged(StatId id)
        {
            OnFinalChanged?.Invoke(id, GetFinal(id));
        }

        /// <summary>
        /// Kiểm tra stat có tồn tại trong dictionary base hay chưa.
        /// (Thông thường luôn tồn tại vì EnsureAllEnumStats được gọi trong constructor.)
        /// </summary>
        public bool HasStat(StatId id) => _base.ContainsKey(id);

        /// <summary>
        /// Đảm bảo tất cả enum StatId đều có entry base (tránh lỗi khi thêm enum mới).
        /// defaultValue: giá trị gán cho stat mới nếu trước đó chưa tồn tại.
        /// Gọi lại hàm này sau khi thêm enum mới để cập nhật các save cũ.
        /// </summary>
        public void EnsureAllEnumStats(float defaultValue = 0f)
        {
            foreach (StatId id in Enum.GetValues(typeof(StatId)))
            {
                if (!_base.ContainsKey(id))
                    _base[id] = defaultValue;
            }
        }

        /// <summary>
        /// Indexer: đọc trả Final, ghi thiết lập Base.
        /// Ví dụ: stats[StatId.CongVatLy] = 100;  (set base)
        /// float atk = stats[StatId.CongVatLy];  (get final)
        /// </summary>
        public float this[StatId id]
        {
            get => GetFinal(id);
            set => SetBase(id, value);
        }

        public StatCollection()
        {
            _base = new Dictionary<StatId, float>();
            _modifiers = new Dictionary<StatId, List<StatModifier>>();
            EnsureAllEnumStats();
        }

        /// <summary>
        /// Trước khi serialize: chuyển dictionary base sang list `_serialized` để Unity/JsonUtility lưu được.
        /// Bỏ qua các stat đánh dấu Derived (không cần lưu base của chúng).
        /// </summary>
        public void OnBeforeSerialize() 
        {
            _serialized.Clear();
            foreach (var kv in _base)
            {
                var meta = GetMeta(kv.Key);
                if (meta != null && (meta.Flags & StatFlags.Derived) != 0) continue;
                _serialized.Add(new StatEntry { id = kv.Key, baseValue = kv.Value });
            }
        }

        /// <summary>
        /// Sau khi deserialize: build lại dictionary base và modifiers (rỗng) rồi đảm bảo đủ enum.
        /// </summary>
        public void OnAfterDeserialize()
        {
            _base = new Dictionary<StatId, float>();
            _modifiers = new Dictionary<StatId, List<StatModifier>>();
            foreach (var e in _serialized)
                _base[e.id] = e.baseValue;
            EnsureAllEnumStats();
        }
    }
}
