using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using Xianxia.Items;
using Xianxia.PlayerDataSystem;
using System.Threading.Tasks;
using System;

/// Slot ô vật phẩm: hiển thị icon/số lượng và phát sinh sự kiện click, double click, kéo/thả.
public class SlotItem : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    [Header("Tham chiếu UI")]
    public Image iconImage;
    public TextMeshProUGUI quantityText;

    [Header("Vị trí Slot")]
    [Tooltip("Chỉ số slot trong túi (do UI gán)")]
    public int slotIndex = -1;

    [Header("Sự kiện (Inspector)")]
    public SlotItemEvent onClick;
    public SlotItemEvent onDoubleClick;
    public SlotItemEvent onBeginDrag;
    public SlotItemDropEvent onDropOnThis; // (source, target)
    public SlotItemEvent onEndDrag;
    public SlotItemEvent onPointerEnter;
    public SlotItemEvent onPointerExit;

    // Sự kiện C# (tuỳ code đăng ký)
    public event Action<SlotItem> Clicked;
    public event Action<SlotItem> DoubleClicked;
    public event Action<SlotItem> BeganDrag;
    public event Action<SlotItem, SlotItem> DroppedOnThis;
    public event Action<SlotItem> EndedDrag;
    public event Action<SlotItem> PointerEntered;
    public event Action<SlotItem> PointerExited;

    private InventoryItem currentItem;
    public InventoryItem CurrentItem => currentItem;

    // Cấu hình double click
    [SerializeField] private float doubleClickThreshold = 0.25f;
    private float _lastClickTime = -999f;

    // Ngữ cảnh kéo/thả chung
    private static SlotItem s_dragSource;
    private static GameObject s_dragIcon;
    private static Canvas s_dragCanvas;

    private void Awake()
    {
        // Mặc định ẩn icon/số lượng nếu chưa có item
        if (iconImage != null) iconImage.enabled = false;
        if (quantityText != null) quantityText.text = string.Empty;
    }

    // API: gán item cho slot và cập nhật hiển thị
    public async void SetItem(InventoryItem item)
    {
        currentItem = item;
    if (item != null) slotIndex = item.Slot;
        await RefreshIconAsync();
    }

    public void Clear()
    {
        currentItem = null;
        if (iconImage != null) iconImage.enabled = false;
        if (quantityText != null) quantityText.text = string.Empty;
    }

    // Dùng cho ô trang bị: lấy icon từ address cụ thể (ví dụ từ ItemDatabase), ẩn số lượng
    public async System.Threading.Tasks.Task SetItemFromAddressAsync(InventoryItem item, string iconAddress, bool showQuantity)
    {
        currentItem = item;
        if (item != null) slotIndex = -1; // equipment slot không dùng chỉ số túi

        if (iconImage == null || quantityText == null)
            return;

        if (item != null)
        {
            Sprite icon = null;
            if (!string.IsNullOrEmpty(iconAddress))
                icon = await ItemAssets.LoadIconSpriteAsync(iconAddress);

            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
                quantityText.text = showQuantity && item.quantity > 1 ? item.quantity.ToString() : string.Empty;
            }
            else
            {
                iconImage.enabled = false;
                quantityText.text = string.Empty;
            }
        }
        else
        {
            iconImage.enabled = false;
            quantityText.text = string.Empty;
        }
    }

    // Cập nhật icon/số lượng (gọi khi thay đổi item)
    public async Task RefreshIconAsync()
    {
        if (iconImage == null || quantityText == null)
            return;

        if (currentItem != null && currentItem.quantity > 0)
        {
            Sprite icon = await ItemAssets.LoadIconSpriteAsync(currentItem.addressIcon);
            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
                quantityText.text = currentItem.quantity > 1 ? currentItem.quantity.ToString() : string.Empty;
            }
            else
            {
                iconImage.enabled = false;
                quantityText.text = string.Empty;
            }
        }
        else
        {
            iconImage.enabled = false;
            quantityText.text = string.Empty;
        }
    }

    //=================== Sự kiện chuột ===================
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left) return;

        float now = Time.unscaledTime;
        if (now - _lastClickTime <= doubleClickThreshold)
        {
            // double click
            _lastClickTime = -999f;
            onDoubleClick?.Invoke(this);
            DoubleClicked?.Invoke(this);
        }
        else
        {
            _lastClickTime = now;
            onClick?.Invoke(this);
            Clicked?.Invoke(this);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        onPointerEnter?.Invoke(this);
        PointerEntered?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        onPointerExit?.Invoke(this);
        PointerExited?.Invoke(this);
    }

    //=================== Kéo/Thả ===================
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentItem == null || currentItem.quantity <= 0) return;

        s_dragSource = this;
        CreateDragIcon();
        UpdateDragIconPosition(eventData);

        onBeginDrag?.Invoke(this);
        BeganDrag?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        UpdateDragIconPosition(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        DestroyDragIcon();
        s_dragSource = null;

        onEndDrag?.Invoke(this);
        EndedDrag?.Invoke(this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (s_dragSource == null || s_dragSource == this) return;
        onDropOnThis?.Invoke(s_dragSource, this);
        DroppedOnThis?.Invoke(s_dragSource, this);
    }

    //=================== Hỗ trợ kéo icon ===================
    private void CreateDragIcon()
    {
        if (iconImage == null || iconImage.sprite == null) return;

        if (s_dragCanvas == null)
        {
            var c = GetComponentInParent<Canvas>();
            s_dragCanvas = c != null ? c.rootCanvas : null;
        }

        s_dragIcon = new GameObject("DraggingIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var img = s_dragIcon.GetComponent<Image>();
        img.raycastTarget = false;
        img.sprite = iconImage.sprite;
        img.color = new Color(1, 1, 1, 0.8f);

        var rt = s_dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = (iconImage.transform as RectTransform)?.sizeDelta ?? new Vector2(64, 64);
        if (s_dragCanvas != null)
            s_dragIcon.transform.SetParent(s_dragCanvas.transform, worldPositionStays: false);
    }

    private void UpdateDragIconPosition(PointerEventData eventData)
    {
        if (s_dragIcon == null) return;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            (s_dragCanvas != null ? s_dragCanvas.transform as RectTransform : null),
            eventData.position,
            eventData.pressEventCamera,
            out var localPos);
        var rt = s_dragIcon.transform as RectTransform;
        if (rt != null) rt.anchoredPosition = localPos;
    }

    private void DestroyDragIcon()
    {
        if (s_dragIcon != null)
        {
            Destroy(s_dragIcon);
            s_dragIcon = null;
        }
    }
}

//=================== UnityEvent helper ===================
[Serializable]
public class SlotItemEvent : UnityEngine.Events.UnityEvent<SlotItem> { }

[Serializable]
public class SlotItemDropEvent : UnityEngine.Events.UnityEvent<SlotItem, SlotItem> { }