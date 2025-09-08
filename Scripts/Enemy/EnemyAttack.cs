using UnityEngine;

public class EnemyAttack : MonoBehaviour
{
    [Header("Thiết lập tấn công")]
    [SerializeField] private float attackRadius = 1.2f;        // Phạm vi thực hiện đòn đánh
    [SerializeField] private float attackCooldown = 1f;         // Thời gian chờ giữa 2 đòn
    [SerializeField] private int damage = 1;                    // Sát thương (placeholder)
    [SerializeField] private LayerMask playerLayer;             // Override nếu muốn khác controller
    [SerializeField] private bool useControllerLayer = true;    // Dùng layer từ EnemyControler

    private float timer = 0f;
    private EnemyControler controller;
    private EnemyAnimation enemyAnim;

    private void Awake()
    {
        controller = GetComponent<EnemyControler>();
        enemyAnim = GetComponent<EnemyAnimation>();
        if (controller != null && useControllerLayer)
        {
            playerLayer = controller.PlayerLayer;
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        if (controller == null) return;

        // Không tấn công nếu quá sát (bên trong keep distance) hoặc hết player
        Collider2D player = Physics2D.OverlapCircle(transform.position, attackRadius, playerLayer);
        if (player == null) return;

        if (timer >= attackCooldown)
        {
            PerformAttack(player);
            timer = 0f;
        }
    }

    private void PerformAttack(Collider2D target)
    {
        if (enemyAnim != null)
        {
            enemyAnim.attackAnimation();
        }
        // Áp dụng sát thương lên Player nếu có PlayerStats
        var playerStats = target.GetComponent<PlayerStats>();
        if (playerStats != null)
        {
           // playerStats.TakeDamage(damage);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, attackRadius);
    }
}