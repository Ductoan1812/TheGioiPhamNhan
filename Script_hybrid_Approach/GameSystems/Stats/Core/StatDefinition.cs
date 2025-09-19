using System;
using System.Collections.Generic;
using UnityEngine;
using Foundation.Events;
using Foundation.Utils;

namespace GameSystems.Stats.Core
{
    /// <summary>
    /// Stat Definition - cải tiến từ StatId enum với metadata.
    /// Tách riêng definition khỏi data để dễ manage.
    /// </summary>
    public enum StatCategory
    {
        Survival,    // HP, MP, Stamina
        Cultivation, // Tu vi, đạo hạnh, etc.
        Combat,      // Attack, Defense, etc.
        Special,     // Khí vận, nghiệp lực
        Extended     // Custom stats
    }

    [Flags]
    public enum StatFlags
    {
        None = 0,
        ResourceMax = 1 << 0,        // Max resource (HP Max, MP Max)
        ResourceCurrent = 1 << 1,    // Current resource (HP, MP)
        Percentage = 1 << 2,         // Display as percentage
        Derived = 1 << 3,            // Calculated from other stats
        Hidden = 1 << 4              // Don't show in UI
    }

    [AttributeUsage(AttributeTargets.Field)]
    public class StatMetaAttribute : Attribute
    {
        public StatCategory Category { get; }
        public string DisplayName { get; }
        public StatFlags Flags { get; }
        public string Description { get; }
        
        public StatMetaAttribute(StatCategory category, string displayName, StatFlags flags = StatFlags.None, string description = "")
        {
            Category = category;
            DisplayName = displayName;
            Flags = flags;
            Description = description;
        }
    }

    /// <summary>
    /// Stat IDs - giữ nguyên từ code cũ nhưng cải tiến metadata
    /// </summary>
    public enum StatId
    {
        // Survival Stats
        [StatMeta(StatCategory.Survival, "Khí huyết tối đa", StatFlags.ResourceMax, "Lượng máu tối đa")]
        KhiHuyetMax,
        [StatMeta(StatCategory.Survival, "Khí huyết", StatFlags.ResourceCurrent, "Lượng máu hiện tại")]
        KhiHuyet,
        [StatMeta(StatCategory.Survival, "Linh lực tối đa", StatFlags.ResourceMax, "Lượng mana tối đa")]
        LinhLucMax,
        [StatMeta(StatCategory.Survival, "Linh lực", StatFlags.ResourceCurrent, "Lượng mana hiện tại")]
        LinhLuc,
        [StatMeta(StatCategory.Survival, "Thọ nguyên tối đa", StatFlags.ResourceMax, "Lượng stamina tối đa")]
        ThoNguyenMax,
        [StatMeta(StatCategory.Survival, "Thọ nguyên", StatFlags.ResourceCurrent, "Lượng stamina hiện tại")]
        ThoNguyen,

        // Cultivation Stats
        [StatMeta(StatCategory.Cultivation, "Tu vi", StatFlags.None, "Cấp độ tu luyện")]
        TuVi,
        [StatMeta(StatCategory.Cultivation, "Đạo hạnh", StatFlags.None, "Kinh nghiệm tu luyện")]
        DaoHanh,
        [StatMeta(StatCategory.Cultivation, "Đạo tâm", StatFlags.None, "Tâm tính tu luyện")]
        DaoTam,
        [StatMeta(StatCategory.Cultivation, "Ngộ tính", StatFlags.None, "Khả năng lĩnh ngộ")]
        NgoTinh,
        [StatMeta(StatCategory.Cultivation, "Căn cốt", StatFlags.None, "Tư chất bẩm sinh")]
        CanCot,
        [StatMeta(StatCategory.Cultivation, "Tu vi cần", StatFlags.Hidden, "Điểm kinh nghiệm cần cho level tiếp theo")]
        TuViCan,

        // Combat Stats
        [StatMeta(StatCategory.Combat, "Công vật lý", StatFlags.None, "Sát thương vật lý")]
        CongVatLy,
        [StatMeta(StatCategory.Combat, "Công pháp thuật", StatFlags.None, "Sát thương phép thuật")]
        CongPhapThuat,
        [StatMeta(StatCategory.Combat, "Phòng vật lý", StatFlags.None, "Phòng thủ vật lý")]
        PhongVatLy,
        [StatMeta(StatCategory.Combat, "Kháng pháp thuật", StatFlags.None, "Kháng phép thuật")]
        PhongPhapThuat,
        [StatMeta(StatCategory.Combat, "Thần hồn", StatFlags.None, "Sức mạnh tinh thần")]
        ThanHon,
        [StatMeta(StatCategory.Combat, "Tốc độ", StatFlags.None, "Tốc độ di chuyển và tấn công")]
        TocDo,

        // Special Stats
        [StatMeta(StatCategory.Special, "Khí vận", StatFlags.None, "May mắn và cơ duyên")]
        KhiVan,
        [StatMeta(StatCategory.Special, "Nghiệp lực", StatFlags.None, "Nghiệp báo tích lũy")]
        NghiepLuc,
        [StatMeta(StatCategory.Special, "Sức chứa kho", StatFlags.None, "Số slot inventory")]
        InventorySize,

        // Extended Stats
        [StatMeta(StatCategory.Extended, "Tỉ lệ bạo kích", StatFlags.Percentage, "Tỉ lệ đánh critical")]
        TiLeBaoKich,
        [StatMeta(StatCategory.Extended, "Sát thương bạo kích", StatFlags.Percentage, "Sát thương critical")]
        SatThuongBaoKich,
        [StatMeta(StatCategory.Extended, "Hút máu", StatFlags.Percentage, "Hồi máu khi đánh")]
        HutMau,
        [StatMeta(StatCategory.Extended, "Xuyên phòng", StatFlags.None, "Xuyên qua giáp")]
        XuyenPhong,
        [StatMeta(StatCategory.Extended, "Hồi phục", StatFlags.None, "Tốc độ hồi phục")]
        HoiPhuc,
        [StatMeta(StatCategory.Extended, "Điểm chưa phân", StatFlags.Hidden, "Điểm stat có thể phân bổ")]
        Points,
    }

    /// <summary>
    /// Single stat entry - cải tiến từ StatEntry struct
    /// </summary>
    [Serializable]
    public class StatEntry
    {
        [SerializeField] private StatId statId;
        [SerializeField] private float baseValue;
        [SerializeField] private List<StatBonus> bonuses = new List<StatBonus>();
        
        // Cached final value
        private float cachedFinalValue;
        private bool isDirty = true;
        
        // Events
        public event Action<StatId, float> OnValueChanged;
        
        public StatId StatId => statId;
        public float BaseValue => baseValue;
        public IReadOnlyList<StatBonus> Bonuses => bonuses;
        
        public float FinalValue
        {
            get
            {
                if (isDirty)
                {
                    RecalculateFinalValue();
                }
                return cachedFinalValue;
            }
        }
        
        public StatEntry(StatId statId, float baseValue = 0f)
        {
            this.statId = statId;
            this.baseValue = baseValue;
            this.bonuses = new List<StatBonus>();
            MarkDirty();
        }
        
        public void SetBaseValue(float value)
        {
            if (!baseValue.Approximately(value))
            {
                baseValue = value;
                MarkDirty();
                OnValueChanged?.Invoke(statId, FinalValue);
            }
        }
        
        public void AddBonus(StatBonus bonus)
        {
            if (bonus == null || bonus.StatId != statId) return;
            
            bonuses.Add(bonus);
            bonuses.Sort((a, b) => a.Priority.CompareTo(b.Priority));
            MarkDirty();
            OnValueChanged?.Invoke(statId, FinalValue);
            
            DebugUtils.Log($"[StatEntry] Added bonus {bonus.Value} to {statId} from {bonus.SourceId}");
        }
        
        public bool RemoveBonus(StatBonus bonus)
        {
            if (bonuses.Remove(bonus))
            {
                MarkDirty();
                OnValueChanged?.Invoke(statId, FinalValue);
                DebugUtils.Log($"[StatEntry] Removed bonus from {statId}");
                return true;
            }
            return false;
        }
        
        public bool RemoveBonusById(string sourceId)
        {
            int removed = bonuses.RemoveAll(b => b.SourceId == sourceId);
            if (removed > 0)
            {
                MarkDirty();
                OnValueChanged?.Invoke(statId, FinalValue);
                DebugUtils.Log($"[StatEntry] Removed {removed} bonuses from {statId} (source: {sourceId})");
                return true;
            }
            return false;
        }
        
        public void ClearBonuses()
        {
            if (bonuses.Count > 0)
            {
                bonuses.Clear();
                MarkDirty();
                OnValueChanged?.Invoke(statId, FinalValue);
                DebugUtils.Log($"[StatEntry] Cleared all bonuses from {statId}");
            }
        }
        
        private void MarkDirty()
        {
            isDirty = true;
        }
        
        private void RecalculateFinalValue()
        {
            float value = baseValue;
            
            // Apply bonuses in priority order
            foreach (var bonus in bonuses)
            {
                value = bonus.ApplyTo(value);
            }
            
            cachedFinalValue = value;
            isDirty = false;
        }
        
        public override string ToString()
        {
            return $"{statId}: {FinalValue:F2} (Base: {baseValue:F2}, Bonuses: {bonuses.Count})";
        }
    }
}
