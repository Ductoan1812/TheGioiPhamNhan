using UnityEngine;
using System.Collections;
using Xianxia.Items;
using System.Threading.Tasks;
using Xianxia.PlayerDataSystem;
using Unity.AppUI.UI;
using TMPro;

/// Vật phẩm rơi ngoài map: lưu id + số lượng.
/// Chỉ cần 2 chức năng: tự biến mất sau thời gian và chạm Player thì cộng vào túi.
public class WordItem : MonoBehaviour
{
    [Header("Vật phẩm")]
    [Tooltip("ID vật phẩm (trùng ID trong ItemDatabaseSO / PlayerData)")]
    public InventoryItem item;
    [Min(1)] public int quantity = 1;

    [Header("Thời gian")]
    [Tooltip("Tự hủy sau (giây)")]
    public float lifetimeSeconds = 30f;
    [SerializeField] private ItemDatabaseSO itemDB;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TextMeshPro textMeshPro;

    [Header("Nhặt trễ")]
    [Tooltip("Thời gian trễ (giây) trước khi có thể nhặt")] public float pickupDelaySeconds = 3f;
    private float _enablePickupAt = -1f;

    [Header("Hiệu ứng bay ra")]
    [Tooltip("Nếu không có Rigidbody, chạy hiệu ứng bay ra nhẹ nhàng khi spawn")]
    public bool flyOutOnSpawn = true;
    [Tooltip("Quãng đường bay ra (đơn vị)")] public float flyOutDistance = 1.2f;
    [Tooltip("Thời gian bay ra (giây)")] public float flyOutDuration = 0.25f;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (textMeshPro == null)
            textMeshPro = GetComponentInChildren<TextMeshPro>();

    }
    private void Start()
    {
        _enablePickupAt = Time.time + Mathf.Max(0f, pickupDelaySeconds);
        if (lifetimeSeconds > 0f)
            Destroy(gameObject, lifetimeSeconds);

        // Nếu không có Rigidbody 2D/3D -> tự tạo hiệu ứng bay ra
        if (flyOutOnSpawn)
        {
            var hasRb = (GetComponentInChildren<Rigidbody2D>() != null) || (GetComponentInChildren<Rigidbody>() != null);
            if (!hasRb)
            {
                StartCoroutine(FlyOutRoutine());
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra tag "Player" trực tiếp
        if (other.CompareTag("Player"))
        {
            if (Time.time >= _enablePickupAt)
                TryPickup(other);
        }
        
    }
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (Time.time >= _enablePickupAt)
                TryPickup(other);
        }
    }
    public bool TryPickup(Component other)
    {
        Debug.Log($"[WordItem] try pickup {item.id} x{quantity}");
        if (item == null || quantity <= 0) return false;
        if (other == null) return false;
        Transform t = other.transform;
        Transform playerTf = null;
        while (t != null)
        {
            if (t.CompareTag("Player")) { playerTf = t; break; }
            t = t.parent;
        }
        Debug.Log($"[WordItem] OnTriggerEnter2D by {other.name} (tag={other.tag})");
        if (playerTf == null) return false;
        var inv = playerTf.GetComponentInChildren<PlayerInventory>();
        if (inv == null) return false;
        if (inv.AddItem(item))
        {
            Debug.Log($"[WordItem] Player nhặt '{item.id}' x{quantity}");
            Destroy(gameObject);
            return true;
        }
        return false;
    }
    public async Task RenderItem()
    {
        if (itemDB == null)
        {
            Debug.LogWarning($"[WordItem] ItemDatabaseSO Null");
            return;
        }
        ItemData itemData = itemDB.GetById(item.id);
        if (itemData == null) return;
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"[WordItem] SpriteRenderer Null");
            return;
        }
        Texture2D tx = await ItemAssets.LoadTextureAsync(itemData.addressTexture);
        if (textMeshPro != null)
            textMeshPro.text = quantity.ToString();
        if (tx != null)
        {
            spriteRenderer.sprite = Sprite.Create(tx, new Rect(0, 0, tx.width, tx.height), Vector2.zero);
        }
        else
        {
            Debug.LogWarning($"[WordItem] Load texture failed: {itemData.addressTexture}");
        }
    }

    private IEnumerator FlyOutRoutine()
    {
        var start = transform.position;
        // Chọn hướng ngẫu nhiên trên mặt phẳng XY, có thành phần hướng lên
        var dir2 = UnityEngine.Random.insideUnitCircle;
        if (dir2 == Vector2.zero) dir2 = Vector2.right;
        dir2.y = Mathf.Abs(dir2.y) + 0.25f;
        dir2.Normalize();
        var target = start + new Vector3(dir2.x, dir2.y, 0f) * Mathf.Max(0.05f, flyOutDistance);
        float t = 0f;
        while (t < flyOutDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / flyOutDuration);
            // ease-out
            float e = 1f - (1f - k) * (1f - k);
            transform.position = Vector3.LerpUnclamped(start, target, e);
            yield return null;
        }
        transform.position = target;
    }
}
