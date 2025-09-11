using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Xianxia.PlayerDataSystem;

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

        public void RefreshValue(PlayerStats stats)
        {
            if (stats == null) return;
            switch (statId)
            {
                case "hpMax":
                    EnsureLabelHasColon();
                    if (valueText) valueText.text = $"{Mathf.RoundToInt(stats.hp)}/{Mathf.RoundToInt(stats.hpMax)}"; break;
                case "qiMax":
                    EnsureLabelHasColon();
                    if (valueText) valueText.text = $"{Mathf.RoundToInt(stats.qi)}/{Mathf.RoundToInt(stats.qiMax)}"; break;
                default:
                    float v = GetStatValue(stats, statId);
                    EnsureLabelHasColon();
                    if (valueText) valueText.text = v.ToString("0");
                    break;
            }
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

        private float GetStatValue(PlayerStats s, string id)
        {
            return id switch
            {
                "atk" => s.atk,
                "def" => s.def,
                "hpMax" => s.hpMax,
                "qiMax" => s.qiMax,
                "critRate" => s.critRate,
                "critDmg" => s.critDmg,
                "moveSpd" => s.moveSpd,
                "hpRegen" => s.hpRegen,
                "qiRegen" => s.qiRegen,
                "lifesteal" => s.lifesteal,
                "spellPower" => s.spellPower,
                "spellResist" => s.spellResist,
                "dodge" => s.dodge,
                "pierce" => s.pierce,
                _ => 0f
            };
        }

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
