using UnityEngine;
using UnityEngine.Events;
using Xianxia.PlayerDataSystem;

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
    [Header("Runtime stats")]
    [SerializeField] private float hpMax;
    [SerializeField] private float hp;
    [SerializeField] private float xpMax;
    [SerializeField] private float xp;
    [SerializeField] private float qiMax;
    [SerializeField] private float qi;
    [SerializeField] private float atk;
    [SerializeField] private float def;
    [SerializeField] private float critRate;
    [SerializeField] private float critDmg;
    [SerializeField] private float moveSpd;
    [SerializeField] private float hpRegen;
    [SerializeField] private float qiRegen;
    [SerializeField] private float lifesteal;
    [SerializeField] private float spellPower;
    [SerializeField] private float spellResist;
    [SerializeField] private float dodge;
    [SerializeField] private float pierce;

    public UnityEvent onStatsLoaded;

    // Accessors
    public float HP { get => hp; set => hp = value; }
    public float Qi { get => qi; set => qi = value; }
    public float Atk { get => atk; set => atk = value; }
    public float Def { get => def; set => def = value; }
    public float CritRate { get => critRate; set => critRate = value; }
    public float MoveSpd { get => moveSpd; set => moveSpd = value; }
    public float HpRegen { get => hpRegen; set => hpRegen = value; }
    public float QiRegen { get => qiRegen; set => qiRegen = value; }
    public float Lifesteal { get => lifesteal; set => lifesteal = value; }
    public float SpellPower { get => spellPower; set => spellPower = value; }
    public float SpellResist { get => spellResist; set => spellResist = value; }
    public float Dodge { get => dodge; set => dodge = value; }
    public float Pierce { get => pierce; set => pierce = value; }
    public float HpMax { get => hpMax; set => hpMax = value; }
    public float QiMax { get => qiMax; set => qiMax = value; }
    public float XpMax { get => xpMax; set => xpMax = value; }
    public float Xp { get => xp; set => xp = value; }
    public float CritDmg { get => critDmg; set => critDmg = value; }


    // set bonus chỉ số tạm thời (từ trang bị, buff, skill...)
    public void SetTemporaryBonus(float hpBonus, float qiBonus, float atkBonus, float defBonus, float critRateBonus, float moveSpdBonus)
    {
        hp += hpBonus;
        qi += qiBonus;
        atk += atkBonus;
        def += defBonus;
        critRate += critRateBonus;
        moveSpd += moveSpdBonus;
        
    }

    private void OnEnable()
	{
        // Subscribe khi PlayerManager load dữ liệu xong
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerDataLoaded += OnPlayerDataLoaded;
            Debug.Log("[PlayerStats] đăng ký sự kiện OnPlayerDataLoaded.");
        }
    }

    private void OnDisable()
    {
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.OnPlayerDataLoaded -= OnPlayerDataLoaded;
            Debug.Log("[PlayerStats] hủy đăng ký sự kiện OnPlayerDataLoaded.");
        }
    }

    /// <summary>
    /// Callback khi PlayerManager load dữ liệu xong.
    /// </summary>
    private void OnPlayerDataLoaded(PlayerData data)
    {
        if (data == null || data.stats == null) return;

        var s = data.stats;
        hp = s.hp;
        qi = s.qi;
        atk = s.atk;
        def = s.def;
        critRate = s.critRate;
        moveSpd = s.moveSpd;
        hpRegen = s.hpRegen;
        qiRegen = s.qiRegen;
        lifesteal = s.lifesteal;
        spellPower = s.spellPower;
        spellResist = s.spellResist;
        dodge = s.dodge;
        pierce = s.pierce;

        onStatsLoaded?.Invoke();
        Debug.Log($"[PlayerStats] Loaded stats for {data.id}");
    }

    /// <summary>
    /// Ghi ngược lại stats hiện tại vào PlayerData và lưu file.
    /// </summary>
    public void ApplyToPlayerData(bool save = true)
	{
		var data = PlayerManager.Instance?.Data;
		if (data == null) return;
		data.stats.hp = hp;
		data.stats.qi = qi;
		data.stats.atk = atk;
		data.stats.def = def;
		data.stats.critRate = critRate;
		data.stats.moveSpd = moveSpd;
        data.stats.hpRegen = hpRegen;
        data.stats.qiRegen = qiRegen;
        data.stats.lifesteal = lifesteal;
        data.stats.spellPower = spellPower;
        data.stats.spellResist = spellResist;
        data.stats.dodge = dodge;
        data.stats.pierce = pierce;

        Debug.Log($"[PlayerStats] Applied stats to PlayerData for {data.id}");

		if (save)
        {
            PlayerManager.Instance.SavePlayer();
        }
	}
}
