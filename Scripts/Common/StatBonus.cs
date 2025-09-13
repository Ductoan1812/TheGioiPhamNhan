using System;
using System.Collections.Generic;
using UnityEngine;
using Xianxia.Stats;

namespace Xianxia.Items
{
    [Serializable]
    public struct StatBonus
    {
        public StatId id;      // Chỉ số áp dụng
        public float add;      // Giá trị cộng thẳng
        public float pct;      // Phần trăm (0.10 = +10%)
    }

    // Tiện ích: áp bonus vào StatCollection
    public static class StatBonusExtensions
    {
        public static void ApplyBonuses(this IEnumerable<StatBonus> bonuses, StatCollection stats, object source)
        {
            if (bonuses == null) return;
            foreach (var b in bonuses)
            {
                if (b.id >= 0) stats.AddModifier(b.id, b.add, b.pct, source);
            }
        }
    }
}
