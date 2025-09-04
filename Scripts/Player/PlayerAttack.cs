using UnityEngine;
using UnityEngine.Events;
// dùng FloatingCombatTextSpawner (cùng assembly, không namespace)

[RequireComponent(typeof(PlayerInput))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private float attackRadius = 1.2f;
    [SerializeField] private LayerMask enemyLayer;
    [SerializeField] private bool directionalOffset = true;
    [SerializeField] private Vector2 offset = new Vector2(0.6f, 0f);

    [Header("Events")] 
    public UnityEvent onAttack; 
    public UnityEvent<int> onAttackWithDamage;

    private PlayerInput input; 
    private PlayerStats stats; 
    private Animator animator; 
    private float timer;

    private void Awake()
    {
        input = GetComponent<PlayerInput>();
        stats = GetComponent<PlayerStats>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        timer += Time.deltaTime;
        if (input == null || timer < attackCooldown) return;
        if (input.AttackL) DoAttack(true);
        else if (input.AttackR) DoAttack(false);
    }

    private void DoAttack(bool left)
    {
        timer = 0f;
        int damage = stats ? stats.Damage : 1;

        if (animator)
        {
            animator.ResetTrigger("2_Attack");
            animator.SetTrigger(left ? "2_Attack" : "2_Attack");
        }

        Vector2 center = transform.position;
        if (directionalOffset)
        {
            Vector2 dir = left ? Vector2.left : Vector2.right;
            center += new Vector2(dir.x * offset.x, offset.y);
        }

        if (attackRadius > 0.01f)
        {
            var hits = Physics2D.OverlapCircleAll(center, attackRadius, enemyLayer);
            for (int i = 0; i < hits.Length; i++)
            {
                var go = hits[i].gameObject;
                bool didDamage = false;
                if (go.TryGetComponent<IDamageable>(out var dmgTarget))
                {
                    didDamage = dmgTarget.ApplyDamage(new DamageContext(gameObject, damage));
                }
                else if (go.TryGetComponent<EnemyStats>(out var es)) 
                {
                    didDamage = es.TakeDamage(damage);
                }
                if (didDamage && FloatingCombatTextSpawner.InstanceFCT)
                {
                    FloatingCombatTextSpawner.InstanceFCT.ShowDamage(go.transform.position + Vector3.up * 0.6f, damage);
                }
            }
        }

        onAttack?.Invoke();
        onAttackWithDamage?.Invoke(damage);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector2 c = transform.position;
        Gizmos.DrawWireSphere(c + Vector2.right * offset.x, attackRadius);
        Gizmos.DrawWireSphere(c + Vector2.left * offset.x, attackRadius);
    }
}
