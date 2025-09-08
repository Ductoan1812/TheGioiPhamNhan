using UnityEngine;
using Xianxia.PlayerDataSystem;

/// Quản lý spawn vật phẩm rơi (prefab có component WordItem).
/// Nhận InventoryItem và gán thẳng vào WordItem.
public class ItemDropManager : MonoBehaviour
{
    [Header("Prefab Vật phẩm")]
    [Tooltip("Prefab phải có component WordItem")]
    public WordItem wordItemPrefab;

    [Header("Hiệu ứng nẩy khi spawn")]
    [Tooltip("Nếu bật, vật phẩm sẽ nẩy ra nhẹ khi xuất hiện")]
    public bool bounceOnSpawn = true;
    [Tooltip("Lực nẩy tối thiểu (2D)")]
    public float bounceForceMin2D = 2f;
    [Tooltip("Lực nẩy tối đa (2D)")]
    public float bounceForceMax2D = 4f;
    [Tooltip("Momen xoắn ngẫu nhiên (2D)")]
    public float torqueMin2D = -20f, torqueMax2D = 20f;
    [Tooltip("Lực nẩy tối thiểu (3D)")]
    public float bounceForceMin3D = 2f;
    [Tooltip("Lực nẩy tối đa (3D)")]
    public float bounceForceMax3D = 4f;
    [Tooltip("Momen xoắn ngẫu nhiên (3D)")]
    public float torqueMin3D = -10f, torqueMax3D = 10f;

    /// Spawn đơn giản tại vị trí, quay mặc định (Quaternion.identity), không parent.
    public WordItem Spawn(InventoryItem invItem, Vector3 position)
    {
        return Spawn(invItem, position, Quaternion.identity, null);
    }

    /// Spawn với đầy đủ tham số: vị trí, quay, parent. lifetimeSeconds < 0: giữ theo prefab.
    public WordItem Spawn(InventoryItem invItem, Vector3 position, Quaternion rotation, Transform parent, float lifetimeSeconds = -1f)
    {
        if (wordItemPrefab == null)
        {
            Debug.LogError("[ItemDropManager] Chưa gán prefab WordItem");
            return null;
        }
        if (invItem == null)
        {
            Debug.LogWarning("[ItemDropManager] invItem null, bỏ qua spawn");
            return null;
        }

        var instance = Instantiate(wordItemPrefab, position, rotation, parent);
        instance.item = invItem;
        instance.quantity = Mathf.Max(1, invItem.quantity); // phục vụ hiển thị số lượng nếu có
        if (lifetimeSeconds >= 0f)
            instance.lifetimeSeconds = lifetimeSeconds;

        // Gợi ý: cập nhật hiển thị ngay nếu prefab hỗ trợ
        _ = instance.RenderItem();

        if (bounceOnSpawn)
            ApplyBounce(instance);

        return instance;
    }

    private void ApplyBounce(WordItem instance)
    {
        if (instance == null) return;
        var go = instance.gameObject;

        // Ưu tiên 2D nếu có Rigidbody2D
        var rb2d = go.GetComponentInChildren<Rigidbody2D>();
        if (rb2d != null)
        {
            var dir = Random.insideUnitCircle;
            if (dir == Vector2.zero) dir = Vector2.right;
            dir.y = Mathf.Abs(dir.y) + 0.5f; // luôn có thành phần hướng lên
            dir.Normalize();
            float force = Random.Range(bounceForceMin2D, bounceForceMax2D);
            rb2d.AddForce(dir * force, ForceMode2D.Impulse);
            rb2d.AddTorque(Random.Range(torqueMin2D, torqueMax2D), ForceMode2D.Impulse);
            return;
        }

        // Nếu không có 2D, thử 3D
        var rb = go.GetComponentInChildren<Rigidbody>();
        if (rb != null)
        {
            var dir3 = new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f));
            if (dir3.sqrMagnitude < 0.0001f) dir3 = Vector3.right;
            dir3 = (dir3 + Vector3.up * 0.75f).normalized; // hướng chếch lên
            float force = Random.Range(bounceForceMin3D, bounceForceMax3D);
            rb.AddForce(dir3 * force, ForceMode.Impulse);
            var torque = new Vector3(
                Random.Range(torqueMin3D, torqueMax3D),
                Random.Range(torqueMin3D, torqueMax3D),
                Random.Range(torqueMin3D, torqueMax3D));
            rb.AddTorque(torque, ForceMode.Impulse);
        }
    }
}
