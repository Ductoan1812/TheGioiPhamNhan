using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Xianxia.Items;

namespace Xianxia.UI.Inventory
{
    [DisallowMultipleComponent]
    public class SlotPrefab : MonoBehaviour, IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("Index")]
        [SerializeField] private int slotIndex; // UI index (0-based)

        [Header("View")]
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;               // UI khuyến nghị
        [SerializeField] private SpriteRenderer iconRenderer;   // fallback nếu dùng SpriteRenderer
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private Image selectedOutline;         // optional: outline/khung highlight

        [Header("Drag")]
        [SerializeField] private Canvas topCanvas;             // canvas để hiển thị ghost
        [SerializeField] private CanvasGroup canvasGroup;      // để tắt raycast khi kéo
        [SerializeField] private float dragAlpha = 0.6f;

        private InventoryUI _owner;
        private string _itemId;
        private int _quantity;
        private bool _isEmpty = true;
        private DragGhost _ghost;

        private void Reset()
        {
            button = GetComponent<Button>();
            if (iconImage == null) iconImage = GetComponentInChildren<Image>();
            if (quantityText == null) quantityText = GetComponentInChildren<TMP_Text>();
            if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
            if (topCanvas == null) topCanvas = GetComponentInParent<Canvas>();
        }

        public void SetOwner(InventoryUI owner) => _owner = owner;
        public void SetIndex(int idx) => slotIndex = idx;

        public void SetSelected(bool selected)
        {
            if (selectedOutline != null)
            {
                selectedOutline.enabled = selected;
                var col = selectedOutline.color;
                col.a = selected ? 1f : 0f;
                selectedOutline.color = col;
            }
        }

        public void SetEmpty()
        {
            _itemId = null;
            _quantity = 0;
            _isEmpty = true;
            ApplyIcon(null);
            if (quantityText != null) { quantityText.text = ""; quantityText.gameObject.SetActive(false); }
        }

        public async void BindItem(string itemId, int quantity, ItemData def)
        {
            _itemId = itemId;
            _quantity = quantity;
            _isEmpty = string.IsNullOrEmpty(itemId) || quantity <= 0;

            // Load icon
            Sprite icon = null;
            if (def != null && !string.IsNullOrEmpty(def.addressIcon))
            {
                icon = await ItemAssets.LoadIconSpriteAsync(def.addressIcon);
            }
            ApplyIcon(icon);

            // Quantity text
            if (quantityText != null)
            {
                bool showQty = quantity > 1;
                quantityText.gameObject.SetActive(showQty);
                quantityText.text = showQty ? quantity.ToString() : "";
            }
        }

        private void ApplyIcon(Sprite icon)
        {
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }
            if (iconRenderer != null)
            {
                iconRenderer.sprite = icon;
                iconRenderer.enabled = icon != null;
            }
        }

        // ========== Events ==========
        public void OnPointerClick(PointerEventData eventData)
        {
            _owner?.OnSlotClicked(slotIndex);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_isEmpty) return;

            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = false;
                canvasGroup.alpha = dragAlpha;
            }

            // Tạo ghost
            if (_ghost == null)
            {
                _ghost = DragGhost.Create(topCanvas);
            }
            var currentSprite = iconImage != null ? iconImage.sprite : iconRenderer != null ? iconRenderer.sprite : null;
            _ghost.Show(currentSprite, eventData.position);
        }

        public void OnDrag(PointerEventData eventData)
        {
            _ghost?.Move(eventData.position);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.alpha = 1f;
            }

            bool droppedOnSlot = false;
            SlotPrefab target = null;

            // Raycast UI để tìm SlotPrefab
            var results = DragGhost.RaycastUI(eventData);
            foreach (var r in results)
            {
                target = r.gameObject.GetComponentInParent<SlotPrefab>();
                if (target != null)
                {
                    droppedOnSlot = true;
                    break;
                }
            }

            _ghost?.Hide();

            if (droppedOnSlot && target != null)
            {
                _owner?.OnSlotDroppedOnto(slotIndex, target.slotIndex);
            }
            else
            {
                // Thả ra ngoài grid: drop ra thế giới
                _owner?.OnSlotDraggedOutside(slotIndex);
            }
        }
    }
}