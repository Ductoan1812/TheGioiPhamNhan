using UnityEngine;

/// <summary>
/// CameraFollow: đơn giản cho game 2D – camera bám theo Player mượt, có offset, dead zone và giới hạn biên.
/// Gắn script này vào Main Camera.
/// </summary>
[ExecuteAlways]
public class CameraFollow : MonoBehaviour
{
    [Header("Target (Player)")]
    [SerializeField] private Transform target;             // Player
    [Tooltip("Tự động tìm GameObject có tag 'Player' nếu để trống")]
    [SerializeField] private bool autoFindPlayer = true;

    [Header("Offset & Độ mượt")]
    [SerializeField] private Vector3 offset = new Vector3(0, 0, -10f);
    [SerializeField, Tooltip("Thời gian trễ (giây) cho SmoothDamp")] private float smoothTime = 0.15f;

    [Header("Dead Zone (vùng an toàn không di chuyển)")]
    [SerializeField, Tooltip("Nửa chiều rộng dead zone")]
    private float deadZoneX = 0.2f;
    [SerializeField, Tooltip("Nửa chiều cao dead zone")]
    private float deadZoneY = 0.15f;

    [Header("Giới hạn biên (World Bounds)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 minBounds = new Vector2(-100, -100);
    [SerializeField] private Vector2 maxBounds = new Vector2(100, 100);

    [Header("Chế độ theo dõi")]
    [Tooltip("Nếu bật: chỉ áp dụng dead zone theo trục X")]
    [SerializeField] private bool horizontalOnly = false;

    private Vector3 velocity; // cho SmoothDamp

    private void Awake()
    {
        TryAutoAssign();
    }

    private void OnEnable()
    {
        TryAutoAssign();
    }

    private void TryAutoAssign()
    {
        if (target == null && autoFindPlayer)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null) target = playerObj.transform;
        }
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 currentPos = transform.position;
        Vector3 targetPos = target.position + offset;

        // Giữ nguyên Z theo offset nếu người chơi ở Z khác
        if (!Mathf.Approximately(offset.z, 0f))
        {
            targetPos.z = target.position.z + offset.z;
        }
        else
        {
            targetPos.z = currentPos.z; // không thay đổi z
        }

        // DEAD ZONE xử lý theo không gian camera (giản lược trong thế giới 2D)
        Vector3 diff = targetPos - currentPos;
        Vector3 adjustedTarget = currentPos;

        if (horizontalOnly)
        {
            float dx = diff.x;
            if (Mathf.Abs(dx) > deadZoneX)
                adjustedTarget.x = targetPos.x - Mathf.Sign(dx) * deadZoneX;
            adjustedTarget.y = currentPos.y; // không đổi Y
            adjustedTarget.z = targetPos.z;
        }
        else
        {
            float dx = diff.x;
            float dy = diff.y;
            if (Mathf.Abs(dx) > deadZoneX)
                adjustedTarget.x = targetPos.x - Mathf.Sign(dx) * deadZoneX;
            if (Mathf.Abs(dy) > deadZoneY)
                adjustedTarget.y = targetPos.y - Mathf.Sign(dy) * deadZoneY;
            adjustedTarget.z = targetPos.z;
        }

        // Smooth
        Vector3 smoothed = Vector3.SmoothDamp(currentPos, adjustedTarget, ref velocity, smoothTime);

        // Clamp theo bounds nếu bật
        if (useBounds)
        {
            smoothed.x = Mathf.Clamp(smoothed.x, minBounds.x, maxBounds.x);
            smoothed.y = Mathf.Clamp(smoothed.y, minBounds.y, maxBounds.y);
        }

        transform.position = smoothed;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying && autoFindPlayer && target == null)
        {
            TryAutoAssign();
        }

        if (target != null)
        {
            // Vẽ dead zone
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
            Vector3 center = new Vector3(transform.position.x, transform.position.y, target.position.z);
            Gizmos.DrawCube(center, new Vector3(deadZoneX * 2f, deadZoneY * 2f, 0.1f));
        }

        if (useBounds)
        {
            Gizmos.color = Color.cyan;
            Vector3 c = new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, 0f);
            Vector3 size = new Vector3((maxBounds.x - minBounds.x), (maxBounds.y - minBounds.y), 0.1f);
            Gizmos.DrawWireCube(c, size);
        }
    }
#endif
}
