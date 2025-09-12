using UnityEngine;

/// <summary>
/// Simple melee attack handler for the player.
/// Press Q (AttackL from PlayerInput) -> performs an instant radial hit test.
/// Later you can move the ApplyDamage() call to an animation event for better timing.
/// </summary>
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(PlayerStatsManager))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")] 
    [SerializeField] private float attackRadius = 1.2f;          // Hit radius around player
    [SerializeField] private float attackCooldown = 0.5f;         // Minimum time between attacks
    [SerializeField] private LayerMask enemyLayer;                // Layers considered as enemies
    [SerializeField] private bool requireFacing = false;          // If true only damage targets in facing half-space
    [SerializeField, Tooltip("If requireFacing, cosine threshold (e.g. 0 = 180°, 0.2 ~ 78°)")] private float facingDotThreshold = 0f;

    [Header("Debug / FX (optional)")] 
    [SerializeField] private bool showGizmo = true;

    private PlayerInput input;
    private PlayerStatsManager statsMgr;
    private float lastAttackTime = -999f;

    private void Awake()
    {
        input = GetComponent<PlayerInput>();
        statsMgr = GetComponent<PlayerStatsManager>();
    }

    private void Update()
    {
        if (input == null || statsMgr == null) return;
        if (!input.AttackL) return; // Current simple attack key
        if (Time.time - lastAttackTime < attackCooldown) return;

        PerformAttack();
        lastAttackTime = Time.time;
    }

    private void PerformAttack()
    {
        // Collect targets
        var hits = Physics2D.OverlapCircleAll(transform.position, attackRadius, enemyLayer);
        if (hits == null || hits.Length == 0) return;

        // Calculate base damage & crit
        float atk = Mathf.Max(1f, statsMgr.atk);
        float critRate = Mathf.Clamp01(statsMgr.critRate); // assuming 0..1
        float critDmg = statsMgr.critDmg; // assume additive multiplier (e.g. 0.5 = +50%)

        foreach (var h in hits)
        {
            if (h == null) continue;
            // Facing filter (optional)
            if (requireFacing)
            {
                Vector2 dir = (h.transform.position - transform.position).normalized;
                float dot = Vector2.Dot(dir, transform.right * -Mathf.Sign(transform.localScale.x));
                if (dot < facingDotThreshold) continue;
            }

            var damageable = h.GetComponent<IDamageable>();
            if (damageable == null || damageable.IsDead) continue;

            bool isCrit = Random.value < critRate;
            float final = atk * (isCrit ? (1f + critDmg) : 1f);
            int dmgInt = Mathf.Max(1, Mathf.RoundToInt(final));
            var ctx = new DamageContext(gameObject, dmgInt, isCrit);
            damageable.ApplyDamage(ctx);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!showGizmo) return;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}
