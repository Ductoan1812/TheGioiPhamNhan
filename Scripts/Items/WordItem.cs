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

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (textMeshPro == null)
            textMeshPro = GetComponentInChildren<TextMeshPro>();

    }
    private void Start()
    {
        if (lifetimeSeconds > 0f)
            Destroy(gameObject, lifetimeSeconds);
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        // Kiểm tra tag "Player" trực tiếp
        if (other.CompareTag("Player"))
        {
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
}
