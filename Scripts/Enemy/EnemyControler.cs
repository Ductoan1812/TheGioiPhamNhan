using UnityEngine;

/// <summary>
/// EnemyControler1: chỉ xử lý di chuyển (tuần tra trong vòng tròn quanh spawn + đuổi player khi vào phạm vi), không attack / animation.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyControler : MonoBehaviour
{
    [Header("Vùng tuần tra (bán kính quanh điểm spawn)")]
    [SerializeField] private float patrolRadius = 5f;

    [Header("Tốc độ")]
    [SerializeField] private float walkSpeed = 1.2f;
    [SerializeField] private float runSpeed = 3f;

    [Header("Phát hiện & đuổi theo")]
    [SerializeField] private float chaseRadius = 3.5f;      // Phạm vi phát hiện player
    [SerializeField] private float keepDistance = 0.6f;      // Giữ khoảng cách khi đuổi

    [Header("Random hướng khi tuần tra")]
    [SerializeField] private float dirChangeMin = 1f;
    [SerializeField] private float dirChangeMax = 3f;

    [Header("Chu kỳ Đi / Đứng (tuần tra)")]
    [SerializeField] private float moveMin = 2f;   // Thời gian tối thiểu đi
    [SerializeField] private float moveMax = 4f;   // Thời gian tối đa đi
    [SerializeField] private float idleMin = 2f;   // Thời gian tối thiểu đứng yên
    [SerializeField] private float idleMax = 4f;   // Thời gian tối đa đứng yên

    [Header("Điều chỉnh biên")]
    [SerializeField, Tooltip("Tỷ lệ bán kính bắt đầu ưu tiên quay về tâm (0-1)")] private float boundaryReturnFactor = 0.85f;

    [Header("Layer Player")]
    [SerializeField] private LayerMask playerLayer;

    // Runtime
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Vector2 spawnPos;
    private Vector2 moveDir;          // Hướng hiện tại (chuẩn hoá)
    private float dirTimer;           // Thời gian đếm đổi hướng
    private float dirDuration;        // Thời lượng cần giữ hướng hiện tại
    private bool isChasing;
    private bool movingPhase = true;   // Đang ở pha di chuyển hay đứng yên
    private float phaseTimer;          // Đếm thời gian cho pha hiện tại
    private float phaseDuration;       // Thời lượng của pha hiện tại

    // Public read-only accessors (phục vụ các script khác như EnemyAttack)
    public float KeepDistance => keepDistance;
    public float ChaseRadius => chaseRadius;
    public LayerMask PlayerLayer => playerLayer;
    public Vector2 SpawnPosition => spawnPos;
    public float PatrolRadius => patrolRadius; // cố định theo vị trí khi Awake (không tự recenter)

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        spawnPos = rb.position;
        PickNewRandomDirection();
        StartMovePhase();
    }

    // Không recenter spawnPos khi Enable để vùng tuần tra cố định.

    private void FixedUpdate()
    {
        Vector2 pos = rb.position;

        // 1. Tìm player trong phạm vi chase
        Collider2D player = Physics2D.OverlapCircle(pos, chaseRadius, playerLayer);
        if (player != null)
        {
            // Đuổi theo player
            Vector2 toPlayer = (Vector2)player.transform.position - pos;
            float dist = toPlayer.magnitude;
            if (dist > keepDistance)
            {
                moveDir = toPlayer / dist; // normalized
            }
            else
            {
                moveDir = Vector2.zero; // Đã tới gần -> đứng lại giữ khoảng cách
            }
            isChasing = true;
        }
        else
        {
            // Không thấy player -> tuần tra tự do trong vùng
            isChasing = false;
            HandlePatrol(pos);
        }

        // 2. Xử lý chạm biên: nếu sắp vượt bán kính, đổi hướng quay vào tâm
        if (!isChasing)
        {
            Vector2 toCenter = spawnPos - pos;
            float distFromCenter = toCenter.magnitude;
            if (distFromCenter >= patrolRadius)
            {
                // Đã vượt (trường hợp do tốc độ/frame) -> ép quay về
                moveDir = toCenter.normalized;
            }
            else if (distFromCenter > patrolRadius * boundaryReturnFactor)
            {
                // Gần biên và đang đi xa thêm -> phản hướng vào trong
                if (Vector2.Dot(moveDir, (pos - spawnPos)) > 0f)
                {
                    moveDir = Vector2.Reflect(moveDir, (spawnPos - pos).normalized);
                    moveDir.Normalize();
                }
            }
        }

        // 3. Áp vận tốc
        float speed = isChasing ? runSpeed : walkSpeed;
        rb.linearVelocity = moveDir * speed;

        // 4. Lật sprite (nếu có)
        if (sr != null && moveDir.x != 0)
            sr.flipX = moveDir.x < 0;
    }

    private void HandlePatrol(Vector2 pos)
    {
        // Cập nhật pha (chỉ khi không chase)
        phaseTimer += Time.fixedDeltaTime;
        if (phaseTimer >= phaseDuration)
        {
            if (movingPhase)
            {
                StartIdlePhase();
            }
            else
            {
                StartMovePhase();
            }
        }

        if (movingPhase)
        {
            dirTimer += Time.fixedDeltaTime;

            // Nếu đang hướng zero (vì vừa dừng tại keepDistance trước đó hoặc vừa hết pha idle), chọn hướng mới
            if (moveDir == Vector2.zero || dirTimer >= dirDuration)
            {
                PickNewRandomDirection();
            }

            // Nếu đi quá xa vùng -> ưu tiên quay về trung tâm
            float dist = (pos - spawnPos).magnitude;
            if (dist > patrolRadius)
            {
                moveDir = (spawnPos - pos).normalized;
                dirTimer = 0f;
                dirDuration = Random.Range(dirChangeMin, dirChangeMax);
            }
        }
        else
        {
            // Pha đứng yên
            moveDir = Vector2.zero;
        }
    }

    private void PickNewRandomDirection()
    {
        dirTimer = 0f;
        dirDuration = Random.Range(dirChangeMin, dirChangeMax);
        Vector2 random = Random.insideUnitCircle;
        moveDir = random == Vector2.zero ? Vector2.right : random.normalized;
    }

    private void StartMovePhase()
    {
        movingPhase = true;
        phaseTimer = 0f;
        phaseDuration = Random.Range(moveMin, moveMax);
        // Bắt đầu pha di chuyển -> chọn hướng mới ngay nếu đang đứng yên
        if (moveDir == Vector2.zero)
        {
            PickNewRandomDirection();
        }
    }

    private void StartIdlePhase()
    {
        movingPhase = false;
        phaseTimer = 0f;
        phaseDuration = Random.Range(idleMin, idleMax);
        moveDir = Vector2.zero; // Dừng lại
    }

    private void OnDrawGizmosSelected()
    {
        // Vẽ vùng tuần tra & chase & keepDistance
        Gizmos.color = Color.green;
        Vector3 center = Application.isPlaying ? (Vector3)spawnPos : transform.position;
        Gizmos.DrawWireSphere(center, patrolRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, chaseRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(center, keepDistance);
    }
}
