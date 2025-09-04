using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class PlayerStats : MonoBehaviour
{
	[Header("Định danh người chơi")]
	[SerializeField] private string playerId = "player_001";
	public string PlayerId => playerId;

	[Header("Máu")]
	[SerializeField] private int maxHealth = 20;
	[SerializeField] private int currentHealth = 20;

	[Header("Mana")]
	[SerializeField] private int maxMana = 50;
	[SerializeField] private int currentMana = 50;

	[Header("Cấp độ & Kinh nghiệm")]
	[SerializeField] private int level = 1; // Lever người dùng yêu cầu
	[SerializeField] private int currentExp = 0;
	[SerializeField] private int expToNextLevel = 100;
	[SerializeField, Tooltip("Hệ số tăng EXP cần cho mỗi cấp")] private float expGrowthMultiplier = 1.25f;
	[SerializeField, Tooltip("Máu cộng thêm mỗi cấp")] private int healthPerLevel = 5;
	[SerializeField, Tooltip("Sát thương cộng thêm mỗi cấp")] private int damagePerLevel = 1;
	
	[Header("Chỉ số")]
	[SerializeField] private int baseDamage = 3;
	[SerializeField] private float baseMoveSpeed = 6f;

	[Header("Sự kiện")] 
	public UnityEvent onDamaged;
	public UnityEvent onHealed;
	public UnityEvent onDeath;
	[Tooltip("Gọi khi mana thay đổi")] public UnityEvent onManaChanged;
	[Tooltip("Gọi khi nhận EXP (kể cả level up)")] public UnityEvent onExpChanged;
	[Tooltip("Gọi mỗi lần lên cấp")] public UnityEvent onLevelUp;

	public int MaxHealth => maxHealth;
	public int CurrentHealth => currentHealth;
	public int Damage => baseDamage;
	public float MoveSpeed => baseMoveSpeed;
	public bool IsDead => currentHealth <= 0;

	public int MaxMana => maxMana;
	public int CurrentMana => currentMana;
	public int Level => level;
	public int CurrentExp => currentExp;
	public int ExpToNextLevel => expToNextLevel;
	public float ExpPercent01 => expToNextLevel > 0 ? (float)currentExp / expToNextLevel : 0f;

	private void Awake()
	{
		// Defaults + clamping
		if (string.IsNullOrWhiteSpace(playerId)) playerId = "player_001";
		if (maxHealth < 1) maxHealth = 1;
		currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
		if (maxMana < 0) maxMana = 0;
		currentMana = Mathf.Clamp(currentMana, 0, maxMana);
		if (level < 1) level = 1;
		if (expToNextLevel < 1) expToNextLevel = 1;

		// Đảm bảo có PlayerIdentity component để các hệ thống khác (vd: ItemPrefab) có thể lấy PlayerId
		EnsureIdentityComponent();
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (string.IsNullOrWhiteSpace(playerId)) playerId = "player_001";
		if (maxHealth < 1) maxHealth = 1;
		currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
		if (maxMana < 0) maxMana = 0;
		currentMana = Mathf.Clamp(currentMana, 0, maxMana);
		if (level < 1) level = 1;
		if (expToNextLevel < 1) expToNextLevel = 1;

		// Đồng bộ PlayerId sang PlayerIdentity khi thay đổi trong Inspector
		var idComp = GetComponent<PlayerIdentity>();
		if (idComp != null) idComp.PlayerId = playerId;
	}
#endif

	private void EnsureIdentityComponent()
	{
		var idComp = GetComponent<PlayerIdentity>();
		if (idComp == null)
		{
			idComp = gameObject.AddComponent<PlayerIdentity>();
		}
		idComp.PlayerId = playerId;
		idComp.Source = this;
	}

	public void SetPlayerId(string newId)
	{
		if (string.IsNullOrWhiteSpace(newId)) return;
		playerId = newId;
		var idComp = GetComponent<PlayerIdentity>();
		if (idComp != null) idComp.PlayerId = playerId;
	}

	public bool TakeDamage(int amount)
	{
		if (amount <= 0 || IsDead) return false;
		int old = currentHealth;
		currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
		if (currentHealth != old)
		{
			onDamaged?.Invoke();
			if (currentHealth <= 0)
			{
				onDeath?.Invoke();
			}
			return true;
		}
		return false;
	}

	public bool Heal(int amount)
	{
		if (amount <= 0 || IsDead) return false;
		int old = currentHealth;
		currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
		if (currentHealth != old)
		{
			onHealed?.Invoke();
			return true;
		}
		return false;
	}

	public void ResetFullHealth()
	{
		currentHealth = maxHealth;
	}

	#region Mana
	public bool SpendMana(int amount)
	{
		if (amount <= 0) return true; // nothing to spend
		if (currentMana < amount) return false;
		currentMana -= amount;
		onManaChanged?.Invoke();
		return true;
	}

	public bool RestoreMana(int amount)
	{
		if (amount <= 0) return false;
		int old = currentMana;
		currentMana = Mathf.Clamp(currentMana + amount, 0, maxMana);
		if (currentMana != old)
		{
			onManaChanged?.Invoke();
			return true;
		}
		return false;
	}

	public void ResetFullMana()
	{
		currentMana = maxMana;
		onManaChanged?.Invoke();
	}
	#endregion

	#region EXP & Level
	public void AddExp(int amount)
	{
		if (amount <= 0) return;
		currentExp += amount;
		bool leveled = false;
		while (currentExp >= expToNextLevel)
		{
			currentExp -= expToNextLevel;
			LevelUpInternal();
			leveled = true;
		}
		onExpChanged?.Invoke();
		if (leveled) onLevelUp?.Invoke();
	}

	private void LevelUpInternal()
	{
		level++;
		// tăng chỉ số
		maxHealth += healthPerLevel;
		currentHealth += healthPerLevel; // giữ tỉ lệ hồi máu khi tăng max
		maxMana += Mathf.Max(0, healthPerLevel / 2); // ví dụ: mana tăng một ít theo health
		currentMana = Mathf.Clamp(currentMana + Mathf.Max(1, healthPerLevel / 2), 0, maxMana);
		baseDamage += damagePerLevel;
		// tính exp cần cho cấp kế (có thể thay bằng công thức riêng)
		int newReq = Mathf.RoundToInt(expToNextLevel * expGrowthMultiplier);
		expToNextLevel = Mathf.Max(newReq, expToNextLevel + 1); // đảm bảo tăng
	}
	#endregion

	#region Upgrades
	public void AddDamage(int amount)
	{
		if (amount <= 0) return;
		baseDamage += amount;
	}

	public void AddMaxHealth(int amount, bool alsoHeal = true)
	{
		if (amount <= 0) return;
		maxHealth = Mathf.Max(1, maxHealth + amount);
		if (alsoHeal)
		{
			int old = currentHealth;
			currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
			if (currentHealth > old) onHealed?.Invoke();
		}
	}
	#endregion
}

/// <summary>
/// Component định danh Player. Được tự động thêm bởi PlayerStats.Awake()
/// để các hệ thống khác có thể GetComponent<PlayerIdentity>() và lấy PlayerId.
/// </summary>
[DisallowMultipleComponent]
public class PlayerIdentity : MonoBehaviour
{
	[SerializeField] private string playerId = "player_001";
	[HideInInspector] public PlayerStats Source; // tham chiếu về PlayerStats sở hữu (nếu có)

	public string PlayerId
	{
		get => playerId;
		set => playerId = string.IsNullOrWhiteSpace(value) ? playerId : value;
	}

#if UNITY_EDITOR
	private void OnValidate()
	{
		if (string.IsNullOrWhiteSpace(playerId)) playerId = "player_001";
	}
#endif
}