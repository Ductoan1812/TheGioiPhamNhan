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
    IPointerExitHandler,
    ICancelHandler
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

    private int _iconVersion = 0; // dùng để hủy kết quả load async cũ

    private void Awake()
    {
        // Mặc định ẩn icon/số lượng nếu chưa có item
        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }
        if (quantityText != null) quantityText.text = string.Empty;
    }

    // API: gán item cho slot và cập nhật hiển thị
    public async void SetItem(InventoryItem item)
    {
        currentItem = item;
        if (item != null) slotIndex = item.Slot;

        if (item == null)
        {
            Clear();
            return;
        }

        int ver = ++_iconVersion;
        await RefreshIconAsyncInternal(ver, item.addressIcon, item.quantity);
    }

    public void Clear()
    {
        currentItem = null;
        _iconVersion++; // vô hiệu hóa mọi load icon đang chờ
        if (iconImage != null)
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
        }
        if (quantityText != null) quantityText.text = string.Empty;
    }

    // Dùng cho ô trang bị: lấy icon từ address cụ thể (ví dụ từ ItemDatabase), ẩn số lượng
    public async System.Threading.Tasks.Task SetItemFromAddressAsync(InventoryItem item, string iconAddress, bool showQuantity)
    {
        currentItem = item;
        if (item != null) slotIndex = -1; // equipment slot không dùng chỉ số túi

        if (iconImage == null || quantityText == null)
            return;

        if (item == null)
        {
            Clear();
            return;
        }

        int ver = ++_iconVersion;
        Sprite icon = null;
        if (!string.IsNullOrEmpty(iconAddress))
            icon = await ItemAssets.LoadIconSpriteAsync(iconAddress);
        if (ver != _iconVersion) return; // bị thay đổi trong lúc chờ -> bỏ kết quả

        if (icon != null)
        {
            iconImage.sprite = icon;
            iconImage.enabled = true;
            quantityText.text = showQuantity && item.quantity > 1 ? item.quantity.ToString() : string.Empty;
        }
        else
        {
            iconImage.enabled = false;
            iconImage.sprite = null;
            quantityText.text = string.Empty;
        }
    }

    // Cập nhật icon/số lượng (gọi khi thay đổi item)
    public async System.Threading.Tasks.Task RefreshIconAsync()
    {
        int ver = ++_iconVersion;
        var itm = currentItem; // chụp lại tham chiếu hiện tại
        string addr = itm != null ? itm.addressIcon : null;
        int qty = itm != null ? itm.quantity : 0;
        await RefreshIconAsyncInternal(ver, addr, qty);
    }

    private async System.Threading.Tasks.Task RefreshIconAsyncInternal(int version, string addressIcon, int quantity)
    {
        if (iconImage == null || quantityText == null)
            return;

        if (currentItem != null && quantity > 0 && !string.IsNullOrEmpty(addressIcon))
        {
            Sprite icon = await ItemAssets.LoadIconSpriteAsync(addressIcon);
            if (version != _iconVersion) return; // đã bị thay đổi trong lúc chờ

            if (icon != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = true;
                quantityText.text = quantity > 1 ? quantity.ToString() : string.Empty;
            }
            else
            {
                iconImage.enabled = false;
                iconImage.sprite = null;
                quantityText.text = string.Empty;
            }
        }
        else
        {
            if (version != _iconVersion) return;
            iconImage.enabled = false;
            iconImage.sprite = null;
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
        // Chỉ huỷ icon nếu chúng ta là nguồn kéo
        if (s_dragSource == this)
        {
            DestroyDragIcon();
            s_dragSource = null;
        }

        onEndDrag?.Invoke(this);
        EndedDrag?.Invoke(this);
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (s_dragSource == null || s_dragSource == this) return;
        onDropOnThis?.Invoke(s_dragSource, this);
        DroppedOnThis?.Invoke(s_dragSource, this);
        // đảm bảo icon kéo được huỷ sau khi drop thành công
        DestroyDragIcon();
        s_dragSource = null;
    }

    public void OnCancel(BaseEventData eventData)
    {
        // Hệ thống huỷ thao tác kéo (mở menu, mất focus, v.v.)
        if (s_dragSource == this)
        {
            DestroyDragIcon();
            s_dragSource = null;
        }
        else
        {
            DestroyDragIcon();
        }
    }

    private void CreateDragIcon()
    {
        if (iconImage == null || iconImage.sprite == null) return;

        if (s_dragCanvas == null)
        {
            var c = GetComponentInParent<Canvas>();
            s_dragCanvas = c != null ? c.rootCanvas : null;
        }
        if (s_dragCanvas == null) return; // không có canvas thì bỏ drag icon

        s_dragIcon = new GameObject("DraggingIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var img = s_dragIcon.GetComponent<Image>();
        img.raycastTarget = false;
        img.sprite = iconImage.sprite;
        img.color = new Color(1, 1, 1, 0.8f);

        var rt = s_dragIcon.GetComponent<RectTransform>();
        rt.sizeDelta = (iconImage.transform as RectTransform)?.sizeDelta ?? new Vector2(64, 64);
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