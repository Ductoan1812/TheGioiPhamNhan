using UnityEngine;

/// <summary>
/// EnemyUI: Hiển thị thanh máu dựa trên EnemyStats.
/// Cấu trúc:
/// EnemyRoot
///   └─ (có EnemyStats)
///   └─ HealthBar (object chứa Background + Fill)
///       ├─ Background (SpriteRenderer)
///       └─ Fill (SpriteRenderer) -> gán vào fillRenderer.
/// </summary>
public class EnemyUI : MonoBehaviour
{
    [Header("Nguồn dữ liệu")]
    [SerializeField] private EnemyStats stats;  // Nếu để trống sẽ tự tìm trên cha

    [Header("Thanh Máu")] 
    [SerializeField] private SpriteRenderer fillRenderer;
    [SerializeField] private SpriteRenderer HealthBar;
    [SerializeField, Tooltip("Scale X tối đa ứng với full máu")] private float fillMaxScaleX = 0.9375f;
    [SerializeField] private bool hideWhenFull = false;
    [SerializeField] private bool hideWhenDead = true;

    [Header("Theo dõi hướng camera (billboard 2D)")]
    [SerializeField] private bool faceCamera = false; // nếu game 2D thuần có thể bỏ

    private Vector3 fillBaseScale = Vector3.one;
    private Camera mainCam;

    private void Awake()
    {
        if (stats == null)
        {
            stats = GetComponentInParent<EnemyStats>();
        }
        if (fillRenderer != null)
        {
            fillBaseScale = fillRenderer.transform.localScale;
        }
        mainCam = Camera.main;
        RefreshImmediate();
    }

    private void LateUpdate()
    {
        if (stats == null || fillRenderer == null) return;
        UpdateHealthBar();
        if (faceCamera && mainCam != null)
        {
            // Giữ thanh quay về camera (nếu 2.5D). Với 2D thuần thường không cần.
            Vector3 camForward = mainCam.transform.forward;
            transform.forward = camForward; // hoặc LookAt nếu cần xoay phức tạp
        }
    }

    public void RefreshImmediate()
    {
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        float ratio = stats.MaxHealth > 0 ? (float)stats.CurrentHealth / stats.MaxHealth : 0f;
        ratio = Mathf.Clamp01(ratio);
        Vector3 s = fillBaseScale;
        s.x = fillMaxScaleX * ratio;
        fillRenderer.transform.localScale = s;

        if (hideWhenDead && stats.IsDead)
        {
            SetFillActive(false);
        }
        else if (hideWhenFull && ratio >= 0.999f)
        {
            SetFillActive(false);
        }
        else
        {
            SetFillActive(true);
        }
    }

    private void SetFillActive(bool active)
    {
        if (fillRenderer != null && fillRenderer.gameObject.activeSelf != active)
            fillRenderer.gameObject.SetActive(active);
        if (HealthBar != null && HealthBar.gameObject.activeSelf != active)
            HealthBar.gameObject.SetActive(active);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (fillRenderer != null)
        {
            fillBaseScale = fillRenderer.transform.localScale;
        }
    }
#endif
}
