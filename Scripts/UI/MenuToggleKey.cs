using UnityEngine;
using UnityEngine.UI;

// Gắn script này vào mỗi Toggle (UGUI) và nhập key.
// Nó sẽ gọi UIManager.Instance.OnToggleSelected(key) khi toggle bật.
[RequireComponent(typeof(Toggle))]
public class MenuToggleKey : MonoBehaviour
{
    public string key;
    private Toggle _toggle;
    private bool _wired;

    private void Awake()
    {
        _toggle = GetComponent<Toggle>();
    }

    private void OnEnable()
    {
        Wire();
        // Nếu toggle khởi động đã bật -> kích hoạt để đồng bộ UI.
        if (_toggle != null && _toggle.isOn && !string.IsNullOrEmpty(key))
        {
            UIManager.Instance?.OnToggleSelected(key);
        }
    }

    private void OnDisable()
    {
        if (_toggle != null)
        {
            _toggle.onValueChanged.RemoveListener(OnToggleChanged);
            _wired = false;
        }
    }

    private void Wire()
    {
        if (_wired || _toggle == null) return;
        _toggle.onValueChanged.AddListener(OnToggleChanged);
        _wired = true;
    }

    private void OnToggleChanged(bool on)
    {
        if (!on) return; // chỉ xử lý khi bật
        if (string.IsNullOrEmpty(key)) return;
        UIManager.Instance?.OnToggleSelected(key);
    }
}
