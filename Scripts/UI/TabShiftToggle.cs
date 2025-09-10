using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(Toggle))]
public class TabShiftToggle : MonoBehaviour
{
    [Tooltip("Để trống sẽ dùng RectTransform của chính đối tượng này (TabVisual).")]
    public RectTransform target;

    [Tooltip("Dịch xuống bao nhiêu pixel khi tab được chọn (dương = xuống).")]
    public float downOffset = 6f;

    [Tooltip("Thời gian animate (giây). 0 = đổi ngay.")]
    public float animTime = 0.08f;

    [Tooltip("Làm tròn vị trí về pixel nguyên.")]
    public bool snapToInt = true;

    Toggle _toggle;
    RectTransform _rt;
    Vector2 _basePos;
    Coroutine _co;

    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _toggle = GetComponent<Toggle>();
        if (target == null) target = _rt;

        // Tự bind ToggleGroup nếu có ở parent (phòng quên gán trong Editor)
        if (_toggle.group == null)
        {
            var group = GetComponentInParent<ToggleGroup>();
            if (group != null) _toggle.group = group;
        }

        _basePos = target.anchoredPosition;
    }

    void OnEnable()
    {
        _toggle.onValueChanged.AddListener(OnToggle);

        // Đồng bộ trạng thái ngay khi bật
        Apply(_toggle.isOn, true);

        // Nếu vô tình có nhiều Toggle đang ON, ToggleGroup sẽ tự xử lý khi bạn click
        // nhưng để chắc ăn, có thể enforce một lần theo Group hiện tại:
        EnforceSingleOnAtStartup();
    }

    void OnDisable()
    {
        _toggle.onValueChanged.RemoveListener(OnToggle);
        if (_co != null) { StopCoroutine(_co); _co = null; }
    }

    void OnToggle(bool isOn)
    {
        Apply(isOn, animTime <= 0f);
    }

    void Apply(bool isOn, bool instant)
    {
        Vector2 to = _basePos + new Vector2(0f, isOn ? -Mathf.Abs(downOffset) : 0f);
        if (snapToInt) to = new Vector2(Mathf.Round(to.x), Mathf.Round(to.y));

        if (instant || animTime <= 0f)
        {
            if (_co != null) StopCoroutine(_co);
            target.anchoredPosition = to;
        }
        else
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(AnimateTo(to, animTime));
        }
    }

    IEnumerator AnimateTo(Vector2 to, float time)
    {
        Vector2 from = target.anchoredPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.0001f, time);
            var p = Vector2.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t));
            if (snapToInt) p = new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
            target.anchoredPosition = p;
            yield return null;
        }
        target.anchoredPosition = to;
        _co = null;
    }

    // Đảm bảo chỉ 1 toggle bật khi vào Play (nếu lỡ có >1 bật sẵn)
    void EnforceSingleOnAtStartup()
    {
        if (_toggle.group == null) return;
        bool foundOn = false;
        foreach (var t in _toggle.group.GetComponentsInChildren<Toggle>(true))
        {
            if (t.isOn)
            {
                if (!foundOn) { foundOn = true; }
                else { t.isOn = false; } // tắt bớt các toggle ON dư
            }
        }
        // Nếu chưa có cái nào ON, bật chính toggle này làm mặc định
        if (!foundOn) _toggle.isOn = true;
    }

    // Gọi hàm này nếu bạn thay đổi layout lúc runtime và muốn set lại mốc
    public void RebindBasePosition()
    {
        _basePos = target.anchoredPosition;
    }
}