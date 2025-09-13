using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Xianxia.PlayerDataSystem;
using Xianxia.Stats;

namespace Xianxia.UI.InfoPlayer
{
    public class StatAllocateRow : MonoBehaviour
    {
        [Header("Refs")]
        public TMP_Text labelText;         // Ví dụ: "Sinh lực"
        public TMP_Text valueText;         // Ví dụ: "4/20" hoặc chỉ số hiện tại
        public TMP_Text deltaText;         // Ví dụ: "+2" (ẩn nếu 0)
        public Button plusButton;
        public Button minusButton;

        [Header("Config")] public string statId; // map logic: hpMax, atk, def, qiMax...

        private int _pending; // điểm phân bổ tạm
        private InfoManager _manager;

        public void Init(InfoManager mgr, string id, string displayName)
        {
            _manager = mgr;
            statId = id;
            if (labelText) labelText.text = displayName;
            ResetPending();
            Hook();
        }

        private void Hook()
        {
            if (plusButton) plusButton.onClick.AddListener(OnPlus);
            if (minusButton) minusButton.onClick.AddListener(OnMinus);
        }

        private void OnDestroy()
        {
            if (plusButton) plusButton.onClick.RemoveListener(OnPlus);
            if (minusButton) minusButton.onClick.RemoveListener(OnMinus);
        }

        private void OnPlus()
        {
            if (_manager == null) return;
            if (_manager.CanSpendPoint())
            {
                _pending++;
                _manager.SpendTempPoint();
                RefreshDelta();
            }
        }

        private void OnMinus()
        {
            if (_manager == null) return;
            if (_pending > 0)
            {
                _pending--;
                _manager.RefundTempPoint();
                RefreshDelta();
            }
        }

        private bool _allocateMode;

        public void SetAllocateMode(bool enabled)
        {
            _allocateMode = enabled;
            if (plusButton) plusButton.gameObject.SetActive(enabled);
            if (minusButton) minusButton.gameObject.SetActive(enabled);
            if (!enabled) ResetPending();
            RefreshDelta();
        }

        public void RefreshValue(StatCollection stats)
        {
            if (stats == null) return;
            EnsureLabelHasColon();
            if (valueText) valueText.text = Xianxia.UI.StatUiMapper.GetDisplayString(stats, statId);
            RefreshDelta();
        }

        private void EnsureLabelHasColon()
        {
            if (labelText == null) return;
            var t = labelText.text.Trim();
            if (!t.EndsWith(":"))
            {
                labelText.text = t + " :"; // khoảng trắng trước sau như mẫu
            }
        }

        // GetStatValue removed, logic unified in StatUiMapper

        private void RefreshDelta()
        {
            if (deltaText)
            {
                bool show = _allocateMode && _pending != 0;
                deltaText.gameObject.SetActive(show);
                if (show) deltaText.text = _pending > 0 ? $"+{_pending}" : _pending.ToString();
            }
            if (minusButton) minusButton.interactable = _allocateMode && _pending > 0;
        }

        public void ResetPending()
        {
            _pending = 0;
            RefreshDelta();
        }

        public int ConsumePending(out string id)
        {
            id = statId;
            int v = _pending;
            _pending = 0;
            RefreshDelta();
            return v;
        }
    }
}
