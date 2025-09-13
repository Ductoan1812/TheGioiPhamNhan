using UnityEngine;
using UnityEngine.Events;
using Xianxia.PlayerDataSystem;
using Xianxia.Items;
using Xianxia.Stats;

[DisallowMultipleComponent]
public class PlayerStatsManager : MonoBehaviour
{
    public int Level => PlayerManager.Instance?.Data?.level ?? 1;
    [Header("Level System (tùy chọn)")]
    public UnityEvent onStatsRecalculated;

    private PlayerData cachedData;

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
        // Realtime clamp handled via OnFinalChanged + here ensure current <= max (StatCollection base values for current resources)
        var data = cachedData;
        if (data == null) return;
        ClampCurrent(data, StatId.KhiHuyet, StatId.KhiHuyetMax);
        ClampCurrent(data, StatId.LinhLuc, StatId.LinhLucMax);
        ClampCurrent(data, StatId.ThoNguyen, StatId.ThoNguyenMax);
    }

    private void OnPlayerDataLoaded(PlayerData data)
    {
        cachedData = data;
        if (data == null || data.stats == null) return;
        // Register for final change events (clamp logic)
        data.stats.OnFinalChanged -= OnStatFinalChangedProxy;
        data.stats.OnFinalChanged += OnStatFinalChangedProxy;
        onStatsRecalculated?.Invoke();
    }

    // Legacy recalculation no longer needed because equipment applies modifiers directly via PlayerEquitment.
    public void RecalculateAll(PlayerData data = null)
    {
        if (data == null) data = PlayerManager.Instance?.Data;
        cachedData = data;
        onStatsRecalculated?.Invoke();
    }

    // ===== Resource API (StatCollection) =====
    public bool Spend(StatId current, float amount)
    {
        var data = PlayerManager.Instance?.Data;
        if (data == null || amount <= 0) return true;
        float c = data.stats.GetBase(current);
        if (c < amount) return false;
        data.stats.SetBase(current, c - amount);
        return true;
    }

    public void Heal(StatId current, StatId max, float amount)
    {
        var data = PlayerManager.Instance?.Data;
        if (data == null || amount <= 0) return;
        float c = data.stats.GetBase(current);
        float m = data.stats.GetFinal(max);
        float n = c + amount;
        if (n > m) n = m;
        data.stats.SetBase(current, n);
    }

    public void HealPercent(StatId current, StatId max, float percent)
    {
        if (percent <= 0) return;
        var data = PlayerManager.Instance?.Data;
        if (data == null) return;
        float m = data.stats.GetFinal(max);
        Heal(current, max, m * percent);
    }

    public void FullRestore()
    {
        var data = PlayerManager.Instance?.Data;
        if (data == null) return;
        data.stats.SetBase(StatId.KhiHuyet, data.stats.GetFinal(StatId.KhiHuyetMax));
        data.stats.SetBase(StatId.LinhLuc, data.stats.GetFinal(StatId.LinhLucMax));
        data.stats.SetBase(StatId.ThoNguyen, data.stats.GetFinal(StatId.ThoNguyenMax));
    }

    public void ClampAllResources()
    {
        var data = PlayerManager.Instance?.Data;
        if (data == null) return;
        ClampCurrent(data, StatId.KhiHuyet, StatId.KhiHuyetMax);
        ClampCurrent(data, StatId.LinhLuc, StatId.LinhLucMax);
        ClampCurrent(data, StatId.ThoNguyen, StatId.ThoNguyenMax);
    }

    private void ClampCurrent(PlayerData data, StatId current, StatId max)
    {
        float c = data.stats.GetBase(current);
        float m = data.stats.GetFinal(max);
        if (c > m) data.stats.SetBase(current, m);
        else if (c < 0) data.stats.SetBase(current, 0);
    }

    private void OnStatFinalChangedProxy(StatId id, float val)
    {
        var data = PlayerManager.Instance?.Data;
        if (data == null) return;
        if (id == StatId.KhiHuyetMax) ClampCurrent(data, StatId.KhiHuyet, StatId.KhiHuyetMax);
        else if (id == StatId.LinhLucMax) ClampCurrent(data, StatId.LinhLuc, StatId.LinhLucMax);
    }

    // Obsolete equipment aggregation removed (handled by PlayerEquitment applying StatModifiers to StatCollection).
    public void RecalculateEquipmentStats(PlayerData data) { }

    // (Legacy helper methods removed)

    public void ApplyToPlayerData(bool save = true) { if (save) PlayerManager.Instance?.SavePlayer(); }

    // Thêm tu vi (thay thế AddExp)
    public void AddTuVi(int amount, bool autoBreakthrough = true)
    {
        var data = PlayerManager.Instance?.Data;
        if (data == null || amount <= 0) return;
        float tv = data.stats.GetBase(StatId.TuVi) + amount;
        float need = data.stats.GetBase(StatId.TuViCan);
        if (need <= 0) need = 100 * Mathf.Max(1, data.level);
        while (autoBreakthrough && tv >= need)
        {
            tv -= need;
            data.level++;
            need = 100 * Mathf.Max(1, data.level);
        }
        data.stats.SetBase(StatId.TuVi, tv);
        data.stats.SetBase(StatId.TuViCan, need);
        PlayerManager.Instance?.SavePlayer();
    }

    public float TuVi => PlayerManager.Instance?.Data?.stats?.GetBase(StatId.TuVi) ?? 0f;
    public float TuViCan => PlayerManager.Instance?.Data?.stats?.GetBase(StatId.TuViCan) ?? 0f;
    public float TuViProgress01()
    {
        var data = PlayerManager.Instance?.Data;
        if (data == null || data.stats == null) return 0f;
        float cur = data.stats.GetBase(StatId.TuVi);
        float need = data.stats.GetBase(StatId.TuViCan);
        if (need <= 0) need = 100 * Mathf.Max(1, data.level);
        return Mathf.Clamp01(cur / need);
    }

    // Lấy số EXP cần để lên level kế tiếp (đồng bộ với hệ thống level)
    public int TuViRequiredForNextLevel()
    {
        var data = PlayerManager.Instance?.Data;
        if (data == null) return 0;
        float need = data.stats.GetBase(StatId.TuViCan);
        if (need <= 0) need = 100 * Mathf.Max(1, data.level);
        return Mathf.RoundToInt(need);
    }

    // Lấy tiến độ hiện tại (0..1) dựa trên xp / xpMax
    [System.Obsolete("Dùng TuViProgress01 thay cho ExpProgress01")] public float ExpProgress01() => TuViProgress01();

    // Exposed getters (ví dụ UI binding)
    private StatCollection Stats => PlayerManager.Instance?.Data?.stats;
    public float hp => Stats?.GetBase(StatId.KhiHuyet) ?? 0;
    public float hpMax => Stats?.GetFinal(StatId.KhiHuyetMax) ?? 0;
    public float atk => Stats?[StatId.CongVatLy] ?? 0;
    public float def => Stats?[StatId.PhongVatLy] ?? 0;
    public float moveSpd => Stats?[StatId.TocDo] ?? 0;
    public float critRate => Stats?[StatId.TiLeBaoKich] ?? 0;
    public float critDmg => Stats?[StatId.SatThuongBaoKich] ?? 0;
    public float hpRegen => Stats?[StatId.HoiPhuc] ?? 0;
    public float qi => Stats?.GetBase(StatId.LinhLuc) ?? 0;
    public float qiMax => Stats?.GetFinal(StatId.LinhLucMax) ?? 0;
    public float lifesteal => Stats?[StatId.HutMau] ?? 0;
    public float spellPower => Stats?[StatId.CongPhapThuat] ?? 0;
    public float spellResist => Stats?[StatId.PhongPhapThuat] ?? 0;
    public float dodge => 0; // Not yet mapped in StatId
    public float pierce => Stats?[StatId.XuyenPhong] ?? 0;
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
        var stats = PlayerManager.Instance?.Data?.stats;
        if (stats == null) return false;
        float cur = stats.GetBase(StatId.KhiHuyet);
        if (cur <= 0) return false;
        float max = stats.GetFinal(StatId.KhiHuyetMax);
        float next = Mathf.Clamp(cur - amount, 0, max);
        if (Mathf.Approximately(next, cur)) return false;
        stats.SetBase(StatId.KhiHuyet, next);
        if (FloatingCombatTextSpawner.InstanceFCT)
        {
            FloatingCombatTextSpawner.InstanceFCT.ShowDamage(mgr.transform.position, amount, isCrit);
        }
        return true;
    }
}
