using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Xianxia.PlayerDataSystem;
using Xianxia.Items;

namespace Xianxia.UI.InfoPlayer
{
    // Đơn giản hóa: chỉ hiển thị icon, tên, mô tả item đã trang bị
    public class EquippedItemView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descText;
        [SerializeField] private TMP_Text slotIdText; // tùy chọn: hiển thị tên slot nếu rỗng

        private string _slotId;

        public void SetEmpty(string slotId)
        {
            _slotId = slotId;
            if (iconImage) { iconImage.sprite = null; iconImage.enabled = false; }
            if (nameText) nameText.text = $"[{slotId}] (Trống)";
            if (descText) descText.text = string.Empty;
            if (slotIdText) slotIdText.text = slotId;
        }

        public void SetData(string slotId, Sprite icon, string itemName, string desc)
        {
            _slotId = slotId;
            if (iconImage)
            {
                iconImage.sprite = icon;
                iconImage.enabled = icon != null;
            }
            if (nameText) nameText.text = itemName;
            if (descText) descText.text = desc;
            if (slotIdText) slotIdText.text = slotId;
        }
    }
}
