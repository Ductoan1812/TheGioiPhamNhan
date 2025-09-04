using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using Xianxia.Items;
using Xianxia.PlayerDataSystem;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider2D))]
public class ItemPrefab : MonoBehaviour
{
    [Header("Item Data")]
    [SerializeField] private string itemId;
    [SerializeField] private int quantity = 1;

    [Header("View")]
    [SerializeField] private SpriteRenderer iconRenderer;
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private bool hideQuantityWhenOne = true;

    [Header("Pickup")]
    [Tooltip("Tự nhặt khi Player (có PlayerStats) chạm vào trigger.")]
    [SerializeField] private bool pickupOnTrigger = true;
    [Tooltip("Nếu không rỗng, chỉ Player có ID này mới nhặt được.")]
    [SerializeField] private string onlyPickupByPlayerId = "";
    [Tooltip("Khóa tạm thời chống double-trigger (giây).")]
    [SerializeField] private float pickupCooldown = 0.2f;

    public string ItemId => itemId;
    public int Quantity => quantity;
    public ItemData Data { get; private set; }

    private CancellationTokenSource _cts;
    private float _lastPickupTime = -999f;

    private void Reset()
    {
        iconRenderer = GetComponent<SpriteRenderer>();
        if (quantityText == null) quantityText = GetComponentInChildren<TMP_Text>();

        var col = GetComponent<Collider2D>();
        if (col == null)
        {
            col = gameObject.AddComponent<CircleCollider2D>();
            ((CircleCollider2D)col).radius = 0.5f;
        }
        col.isTrigger = true;
    }

    private void OnEnable()
    {
        if (!string.IsNullOrWhiteSpace(itemId))
            _ = ApplyAsync();
        else
            UpdateQuantityLabel();
    }

    private void OnDisable() => CancelPending();
    private void OnDestroy() => CancelPending();

    public void Setup(string id, int qty)
    {
        itemId = id;
        quantity = Mathf.Max(1, qty);
        _ = ApplyAsync();
    }

    public void SetQuantity(int qty)
    {
        quantity = Mathf.Max(0, qty);
        UpdateQuantityLabel();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!pickupOnTrigger) return;
        TryPickupFromCollider(other.gameObject);
    }

    private void TryPickupFromCollider(GameObject go)
    {
        if (Time.time - _lastPickupTime < pickupCooldown) return;

        // Tìm PlayerStats trên đối tượng hoặc cha (trường hợp collider nằm ở child)
        var stats = go.GetComponentInParent<PlayerStats>();
        if (stats == null) return;

        var playerId = stats.PlayerId;
        if (!string.IsNullOrEmpty(onlyPickupByPlayerId) && playerId != onlyPickupByPlayerId)
            return;

        if (TryPickup(playerId))
            _lastPickupTime = Time.time;
    }

    /// <summary>
    /// Thử nhặt item vào inventory của playerId. Trả về true nếu nhặt được (kể cả 1 phần).
    /// </summary>
    public bool TryPickup(string playerId)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0) return false;

        var pd = PlayerData.GetForPlayer(playerId, autoCreateIfMissing: true);
        if (pd == null)
        {
            Debug.LogError($"ItemPrefab: Không thể load PlayerData cho playerId={playerId}");
            return false;
        }

        int before = quantity;
        int leftover = pd.AddItem(itemId, quantity, ItemDatabaseSO.Instance);
        int picked = before - leftover;

        if (picked <= 0) return false;

        pd.SaveForPlayer();

        if (leftover > 0)
            SetQuantity(leftover);
        else
            Destroy(gameObject);

        return true;
    }

    private async Task ApplyAsync()
    {
        CancelPending();
        _cts = new CancellationTokenSource();
        var token = _cts.Token;

        if (ItemDatabaseSO.Instance == null)
        {
            Debug.LogError("ItemPrefab: ItemDatabaseSO.Instance is null.");
            ClearView();
            return;
        }

        Data = ItemDatabaseSO.Instance.GetById(itemId);
        if (Data == null)
        {
            Debug.LogError($"ItemPrefab: Item id '{itemId}' không tồn tại trong DB.");
            ClearView();
            return;
        }

        Sprite sprite = null;
        if (!string.IsNullOrEmpty(Data.addressIcon))
        {
            try
            {
                sprite = await ItemAssets.LoadIconSpriteAsync(Data.addressIcon);
                if (token.IsCancellationRequested) return;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ItemPrefab: Lỗi load icon '{Data.addressIcon}' - {e.Message}");
            }
        }

        if (iconRenderer != null)
        {
            iconRenderer.sprite = sprite;
            iconRenderer.enabled = sprite != null;
        }

        UpdateQuantityLabel();
    }

    private void UpdateQuantityLabel()
    {
        if (quantityText == null) return;

        bool show = !(hideQuantityWhenOne && quantity <= 1);
        if (Data != null && Data.maxStack <= 1 && quantity <= 1)
            show = !hideQuantityWhenOne;

        quantityText.gameObject.SetActive(show);
        quantityText.text = quantity > 0 ? quantity.ToString() : "";
    }

    private void ClearView()
    {
        if (iconRenderer != null)
        {
            iconRenderer.sprite = null;
            iconRenderer.enabled = false;
        }
        if (quantityText != null)
        {
            quantityText.text = "";
            quantityText.gameObject.SetActive(false);
        }
    }

    private void CancelPending()
    {
        if (_cts != null)
        {
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (quantity < 0) quantity = 0;
        if (!Application.isPlaying)
        {
            if (quantityText == null) quantityText = GetComponentInChildren<TMP_Text>();
            UpdateQuantityLabel();
        }
    }
#endif
}