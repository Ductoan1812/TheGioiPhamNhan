using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Xianxia.Items;

namespace Xianxia.UI.Inventory
{
    [DisallowMultipleComponent]
    public class SlotPrefab : MonoBehaviour, IPointerClickHandler
    {
        [Header("Index")]
        [SerializeField] private int slotIndex; // UI index (0-based)

        [Header("View")]
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text quantityText;
        [SerializeField] private Image selectedOutline; // optional

        private InventoryUI _owner;

        private void Reset()
        {
            button = GetComponent<Button>();
            if (iconImage == null) iconImage = GetComponentInChildren<Image>();
            if (quantityText == null) quantityText = GetComponentInChildren<TMP_Text>();
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
            ApplyIcon(null);
            if (quantityText != null) { quantityText.text = ""; quantityText.gameObject.SetActive(false); }
        }

        public async void BindItem(string itemId, int quantity, ItemData def)
        {
            Sprite icon = null;
            if (def != null && !string.IsNullOrEmpty(def.addressIcon))
            {
                icon = await ItemAssets.LoadIconSpriteAsync(def.addressIcon);
            }
            ApplyIcon(icon);

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
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            _owner?.OnSlotClicked(slotIndex);
        }
    }
}