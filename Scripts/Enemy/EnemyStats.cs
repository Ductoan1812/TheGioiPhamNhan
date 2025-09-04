using UnityEngine;
using UnityEngine.Events;

// sử dụng các struct / interface combat (cùng assembly nên không cần namespace riêng, nếu có namespace hãy thêm using ở đây)

/// <summary>
/// EnemyStats: Chỉ quản lý dữ liệu máu / sự kiện. Không xử lý UI.
/// </summary>
[DisallowMultipleComponent]
public class EnemyStats : MonoBehaviour
{
    [Header("Máu")] 
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private int currentHealth = 10;
    [SerializeField] private int healerAmount = 3;

    [Header("Tự động hồi máu khi an toàn")] 
    [SerializeField, Tooltip("Bật/tắt cơ chế auto-heal")] private bool enableAutoHeal = true;
    [SerializeField, Tooltip("Thời gian không có player gần (giây) trước khi bắt đầu hồi")]
    private float healDelay = 3f;
    [SerializeField, Tooltip("Khoảng cách kiểm tra player (radius)")] private float playerCheckRadius = 4f;
    [SerializeField, Tooltip("Chu kỳ hồi (giây) mỗi lần cộng healerAmount")]
    private float healInterval = 1f;
    [SerializeField, Tooltip("Layer của Player để phát hiện, để trống sẽ tìm tag 'Player'")] private LayerMask playerLayer;
    [SerializeField, Tooltip("Nếu không dùng layer, tìm theo tag 'Player'")] private bool useTagSearchIfLayerEmpty = true;

    [Header("Sự kiện")] 
    public UnityEvent onDamaged;
    public UnityEvent onHealed;
    public UnityEvent onDeath;

    [Header("Loot khi chết")]
    //[SerializeField] private ItemDrop itemDrop; // Component dùng chung

    public int MaxHealth => maxHealth;
    public int CurrentHealth => currentHealth;
    public bool IsDead => currentHealth <= 0;

    // Runtime
    private float lastDamageTime = -999f; // thời điểm cuối bị đánh
    private float lastHealTickTime = 0f;  // thời điểm cuối đã hồi 1 tick
    private Transform playerCached;

    private void Awake()
    {
        if (maxHealth < 1) maxHealth = 1;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
    CachePlayerIfNeeded();
   // if (itemDrop == null) itemDrop = GetComponent<ItemDrop>();
    }

    private void OnValidate()
    {
        if (maxHealth < 1) maxHealth = 1;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (healDelay < 0f) healDelay = 0f;
        if (playerCheckRadius < 0f) playerCheckRadius = 0f;
        if (healInterval < 0.1f) healInterval = 0.1f;
    }

    private void Update()
    {
        if (!enableAutoHeal || IsDead) return;
        if (currentHealth >= maxHealth) return;

        // Kiểm tra player còn cache không
        if (useTagSearchIfLayerEmpty && (playerCached == null))
        {
            CachePlayerIfNeeded();
        }

        bool playerNear = IsPlayerNearby();
        float timeSinceDamage = Time.time - lastDamageTime;
        if (!playerNear && timeSinceDamage >= healDelay)
        {
            // Hồi theo chu kỳ
            if (Time.time - lastHealTickTime >= healInterval)
            {
                Heal(healerAmount);
                lastHealTickTime = Time.time;
            }
        }
        else
        {
            // Reset nhịp hồi nếu player quay lại hoặc bị đánh
            if (playerNear)
            {
                lastHealTickTime = Time.time; // tránh vừa rời ra lại hồi ngay (đợi healDelay)
            }
        }
    }

    public bool TakeDamage(int amount)
    {
        if (amount <= 0 || IsDead) return false;
        int old = currentHealth;
        currentHealth = Mathf.Clamp(currentHealth - amount, 0, maxHealth);
        if (currentHealth != old)
        {
            onDamaged?.Invoke();
            lastDamageTime = Time.time;
            if (currentHealth <= 0)
            {
                onDeath?.Invoke();
            }
            return true;
        }
        return false;
    }

    // Gọi hàm này từ UnityEvent onDeath để drop item
    public void OnDeath_DropItem()
    {
        //if (itemDrop == null) itemDrop = GetComponent<ItemDrop>();
       // itemDrop?.DropConfigured();
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

    private bool IsPlayerNearby()
    {
        // Nếu có layer -> Physics2D.OverlapCircle
        if (playerLayer.value != 0)
        {
            return Physics2D.OverlapCircle(transform.position, playerCheckRadius, playerLayer) != null;
        }
        // fallback tìm theo tag
        if (useTagSearchIfLayerEmpty)
        {
            if (playerCached != null)
            {
                return Vector2.Distance(playerCached.position, transform.position) <= playerCheckRadius;
            }
        }
        return false;
    }

    private void CachePlayerIfNeeded()
    {
        if (playerCached == null && useTagSearchIfLayerEmpty)
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) playerCached = go.transform;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!enableAutoHeal) return;
        Gizmos.color = new Color(0f, 1f, 0.6f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, playerCheckRadius);
    }
}
