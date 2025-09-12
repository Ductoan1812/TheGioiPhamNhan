using UnityEngine;
using UnityEngine.Events;
using Xianxia.PlayerDataSystem;
using Xianxia.Items;

[DisallowMultipleComponent]
public class PlayerStatsManager : MonoBehaviour
{
    // Level lấy trực tiếp từ PlayerData (không cần field riêng)
    public int Level => PlayerManager.Instance?.Data?.level ?? 1;
    [Header("Base stats (từ PlayerData.stats gốc hoặc level)")]
    [SerializeField] private PlayerStats statsBaseData;
    [Header("Equipment cộng thêm")]
    [SerializeField] private PlayerStats statsEquipData;
    [Header("Current (base + equip, chưa có buff)")]
    [SerializeField] private PlayerStats statsCurrentData;

    public UnityEvent onStatsRecalculated;
    [Header("Level System")]
    [SerializeField] private Xianxia.Progression.LevelSystem levelSystem; // tham chiếu optional
    //get set base stats
    public PlayerStats StatsBaseData { get => statsBaseData; set => statsBaseData = value; }
    public PlayerStats StatsEquipData { get => statsEquipData; set => statsEquipData = value; }
    public PlayerStats StatsCurrentData { get => statsCurrentData; set => statsCurrentData = value; }

    private void OnEnable()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
        }
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
        }
    }

    private void Update()
    {
        // Clamp realtime resources
        if (statsCurrentData != null)
        {
            if (statsCurrentData.hp > statsCurrentData.hpMax) statsCurrentData.hp = statsCurrentData.hpMax;
            if (statsCurrentData.qi > statsCurrentData.qiMax) statsCurrentData.qi = statsCurrentData.qiMax;
        }
    }

    private void OnPlayerDataLoaded(PlayerData data)
    {
        if (data == null || data.stats == null) return;
        // Copy về base
        CopyStats(data.stats, statsBaseData);
        // đảm bảo xpMax hợp lý (nếu cấu hình hệ thống level)
        if (levelSystem != null && data.stats.xpMax <= 0)
        {
            data.stats.xpMax = levelSystem.GetExpRequired(data.realm, data.level);
        }
        // Recalc equipment + current
        RecalculateAll(data);
    }

    public void RecalculateAll(PlayerData data = null)
    {
        if (data == null) data = PlayerManager.Instance?.Data;
        if (data == null) return;

        ClearStats(statsEquipData);
        RecalculateEquipmentStats(data);

        // current = base + equip (chưa buff)
        SumStats(statsBaseData, statsEquipData, statsCurrentData);

        // Giữ nguyên tỷ lệ HP/Qi hiện tại nếu đã có giá trị trước đó
        MaintainResourceRatio(statsCurrentData, data.stats);

        onStatsRecalculated?.Invoke();
    }

    public void RecalculateEquipmentStats(PlayerData data)
    {
        if (data?.equipment == null) return;
        foreach (var slot in data.equipment.Slots)
        {
            var invItem = slot.item;
            if (invItem == null) continue;
            // Ở đây giả định InventoryItem kế thừa ItemData (theo PlayerData.cs) => có baseStats
            var baseStats = invItem.baseStats;
            if (baseStats != null)
            {
                statsEquipData.atk += baseStats.atk;
                statsEquipData.def += baseStats.defense;
                statsEquipData.hpMax += baseStats.hp;
                statsEquipData.qiMax += baseStats.qi;
                statsEquipData.moveSpd += baseStats.moveSpd;
                statsEquipData.critRate += baseStats.critRate;
                statsEquipData.critDmg += baseStats.critDmg;
                statsEquipData.pierce += baseStats.penetration;
                // Một số stat không có mapping trực tiếp (lifesteal, spellPower...), thêm sau nếu cần.
            }
            // Affix cộng thẳng (ví dụ id: atk, hpMax...) – đơn giản minh họa
            if (invItem.affixes != null)
            {
                foreach (var af in invItem.affixes)
                {
                    if (af == null) continue;
                    ApplyAffix(statsEquipData, af.id, af.value);
                }
            }
        }
    }

    private void ApplyAffix(PlayerStats target, string id, float value)
    {
        if (string.IsNullOrWhiteSpace(id) || target == null) return;
        switch (id.ToLowerInvariant())
        {
            case "atk": target.atk += value; break;
            case "def": target.def += value; break;
            case "hp":
            case "hpmax": target.hpMax += value; break;
            case "qi":
            case "qimax": target.qiMax += value; break;
            case "critrate": target.critRate += value; break;
            case "critdmg": target.critDmg += value; break;
            case "movespd": target.moveSpd += value; break;
            case "lifesteal": target.lifesteal += value; break;
            case "spellpower": target.spellPower += value; break;
            case "spellresist": target.spellResist += value; break;
            case "dodge": target.dodge += value; break;
            case "pierce": target.pierce += value; break;
            case "hpregen": target.hpRegen += value; break;
            case "qiregen": target.qiRegen += value; break;
            default: break; // chưa map -> bỏ qua
        }
    }

    private void MaintainResourceRatio(PlayerStats current, PlayerStats prevOriginal)
    {
        if (current == null || prevOriginal == null) return;
        float hpRatio = prevOriginal.hpMax > 0 ? prevOriginal.hp / prevOriginal.hpMax : 1f;
        float qiRatio = prevOriginal.qiMax > 0 ? prevOriginal.qi / prevOriginal.qiMax : 1f;
        current.hp = Mathf.Clamp(hpRatio * current.hpMax, 0, current.hpMax);
        current.qi = Mathf.Clamp(qiRatio * current.qiMax, 0, current.qiMax);
    }

    private void CopyStats(PlayerStats src, PlayerStats dst)
    {
        if (src == null || dst == null) return;
        dst.hpMax = src.hpMax; dst.hp = src.hp;
        dst.qiMax = src.qiMax; dst.qi = src.qi;
        dst.atk = src.atk; dst.def = src.def;
        dst.critRate = src.critRate; dst.critDmg = src.critDmg;
        dst.moveSpd = src.moveSpd; dst.hpRegen = src.hpRegen; dst.qiRegen = src.qiRegen;
        dst.lifesteal = src.lifesteal; dst.spellPower = src.spellPower; dst.spellResist = src.spellResist;
        dst.dodge = src.dodge; dst.pierce = src.pierce; dst.block = src.block; dst.blockRate = src.blockRate; dst.luck = src.luck;
        dst.InventorySize = src.InventorySize;
    }

    private void ClearStats(PlayerStats s)
    {
        if (s == null) return;
        s.hpMax = s.hp = s.qiMax = s.qi = 0;
        s.atk = s.def = s.critRate = s.critDmg = 0;
        s.moveSpd = s.hpRegen = s.qiRegen = 0;
        s.lifesteal = s.spellPower = s.spellResist = 0;
        s.dodge = s.pierce = s.block = s.blockRate = 0;
        s.luck = 0; s.InventorySize = 0;
    }

    private void SumStats(PlayerStats a, PlayerStats b, PlayerStats dst)
    {
        if (dst == null) return;
        ClearStats(dst);
        if (a != null)
        {
            dst.hpMax += a.hpMax; dst.hp += a.hp;
            dst.qiMax += a.qiMax; dst.qi += a.qi;
            dst.atk += a.atk; dst.def += a.def;
            dst.critRate += a.critRate; dst.critDmg += a.critDmg;
            dst.moveSpd += a.moveSpd; dst.hpRegen += a.hpRegen; dst.qiRegen += a.qiRegen;
            dst.lifesteal += a.lifesteal; dst.spellPower += a.spellPower; dst.spellResist += a.spellResist;
            dst.dodge += a.dodge; dst.pierce += a.pierce; dst.block += a.block; dst.blockRate += a.blockRate; dst.luck += a.luck;
            dst.InventorySize += a.InventorySize;
        }
        if (b != null)
        {
            dst.hpMax += b.hpMax; dst.hp += b.hp;
            dst.qiMax += b.qiMax; dst.qi += b.qi;
            dst.atk += b.atk; dst.def += b.def;
            dst.critRate += b.critRate; dst.critDmg += b.critDmg;
            dst.moveSpd += b.moveSpd; dst.hpRegen += b.hpRegen; dst.qiRegen += b.qiRegen;
            dst.lifesteal += b.lifesteal; dst.spellPower += b.spellPower; dst.spellResist += b.spellResist;
            dst.dodge += b.dodge; dst.pierce += b.pierce; dst.block += b.block; dst.blockRate += b.blockRate; dst.luck += b.luck;
            dst.InventorySize += b.InventorySize;
        }
    }

    public void ApplyToPlayerData(bool save = true)
    {
        var data = PlayerManager.Instance?.Data;
        if (data == null) return;
        CopyStats(statsCurrentData, data.stats);
        if (save) PlayerManager.Instance.SavePlayer();
    }

    // Wrapper thêm exp sử dụng LevelSystem nếu có
    public void AddExp(int amount, bool autoLevel = true)
    {
        var data = PlayerManager.Instance?.Data;
        if (data == null || amount <= 0) return;
        if (levelSystem != null)
        {
            levelSystem.AddExp(data, amount, autoLevel);
        }
        else
        {
            data.stats.xp += amount;
            if (data.stats.xpMax <= 0) data.stats.xpMax = 100 * Mathf.Max(1, data.level);
            if (autoLevel && data.stats.xp >= data.stats.xpMax)
            {
                data.stats.xp -= data.stats.xpMax;
                data.level++;
                data.stats.xpMax = 100 * Mathf.Max(1, data.level);
            }
            PlayerManager.Instance?.SavePlayer();
        }
    }

    // Lấy số EXP cần để lên level kế tiếp (đồng bộ với hệ thống level)
    public int ExpRequiredForNextLevel()
    {
        var data = PlayerManager.Instance?.Data;
        if (data == null) return 0;
        if (levelSystem != null)
            return levelSystem.GetExpRequired(data.realm, data.level);
        return 100 * Mathf.Max(1, data.level); // fallback
    }

    // Lấy tiến độ hiện tại (0..1) dựa trên xp / xpMax
    public float ExpProgress01()
    {
        var data = PlayerManager.Instance?.Data;
        if (data == null || data.stats == null) return 0f;
        float need = data.stats.xpMax > 0 ? data.stats.xpMax : ExpRequiredForNextLevel();
        if (need <= 0) return 0f;
        return Mathf.Clamp01(data.stats.xp / need);
    }

    // Exposed getters (ví dụ UI binding)
    public float hp => statsCurrentData?.hp ?? 0;
    public float hpMax => statsCurrentData?.hpMax ?? 0;
    public float atk => statsCurrentData?.atk ?? 0;
    public float def => statsCurrentData?.def ?? 0;
    public float moveSpd => statsCurrentData?.moveSpd ?? 0;
    public float critRate => statsCurrentData?.critRate ?? 0;
    public float critDmg => statsCurrentData?.critDmg ?? 0; // added getter for PlayerAttack
    public float hpRegen => statsCurrentData?.hpRegen ?? 0;
    public float qi => statsCurrentData?.qi ?? 0;
    public float qiMax => statsCurrentData?.qiMax ?? 0;
    public float lifesteal => statsCurrentData?.lifesteal ?? 0;
    public float spellPower => statsCurrentData?.spellPower ?? 0;
    public float spellResist => statsCurrentData?.spellResist ?? 0;
    public float dodge => statsCurrentData?.dodge ?? 0;
    public float pierce => statsCurrentData?.pierce ?? 0;
}

public static class PlayerStatsManagerExtensions
{
    /// <summary>
    /// Basic damage intake for player (temporary). Later move to dedicated combat/health component.
    /// Applies raw damage (no defense yet) and clamps HP. Returns true if HP changed.
    /// </summary>
    public static bool TakeDamage(this PlayerStatsManager mgr, int amount, bool isCrit = false)
    {
        if (mgr == null || amount <= 0) return false;
        var stats = mgr.StatsCurrentData;
        if (stats == null) return false;
        if (stats.hp <= 0) return false; // already dead
        float old = stats.hp;
        stats.hp = Mathf.Clamp(stats.hp - amount, 0, stats.hpMax);
        if (stats.hp != old)
        {
            // Floating combat text
            if (FloatingCombatTextSpawner.InstanceFCT)
            {
                FloatingCombatTextSpawner.InstanceFCT.ShowDamage(mgr.transform.position, amount, isCrit);
            }
            // TODO: raise event / death handling later
            return true;
        }
        return false;
    }
}
