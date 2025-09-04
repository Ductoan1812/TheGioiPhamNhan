using UnityEngine;

public class EnemyAnimation : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    // Lưu hướng đứng yên cuối cùng để Blend không nhảy lung tung khi idle
    private float lastBlend = 0.5f; // mặc định ngang
    private Vector2 lastMoveDir = Vector2.down;

    void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (animator == null) return;

        // Lấy hướng di chuyển từ vận tốc Rigidbody2D (controller đã đặt velocity)
        Vector2 moveVec = rb != null ? rb.linearVelocity : Vector2.zero;
        Vector2 logicalDir = moveVec.sqrMagnitude > 0.0001f ? moveVec.normalized : Vector2.zero;

        bool isMoving = moveVec.sqrMagnitude > 0.001f;
        animator.SetBool("Moving", isMoving);

        // Speed: ưu tiên lấy moveSpeed nếu có, nếu không lấy magnitude của vận tốc rigidbody
        float speedValue = moveVec.magnitude;
        animator.SetFloat("Speed", speedValue);

        if (isMoving)
        {
            lastMoveDir = logicalDir;
            // Blend 8 hướng gom về 3 giá trị chính: 0 xuống, 0.5 ngang, 1 lên
            float b;
            if (logicalDir.y > 0.25f) b = 1f;            // ưu tiên lên
            else if (logicalDir.y < -0.25f) b = 0f;      // xuống
            else b = 0.5f;                           // ngang / chéo ưu tiên ngang
            lastBlend = b;
            animator.SetFloat("Blend", lastBlend);
        }
        else
        {
            // Giữ blend cũ để idle nhìn theo hướng trước đó
            animator.SetFloat("Blend", lastBlend);
        }

        // Flip sprite dựa vào hướng X (nếu có sprite)
        if (sr != null && Mathf.Abs(lastMoveDir.x) > 0.05f)
        {
            sr.flipX = lastMoveDir.x < 0f;
        }
    }

    public void attackAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Attack");
    }
    public void huntAnimation()
    {
        if (animator != null)
            animator.SetTrigger("Hunt");
    }
}
