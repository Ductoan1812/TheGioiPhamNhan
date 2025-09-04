using UnityEngine;
using UnityEngine.Rendering;

// Gắn script này vào 1 GameObject (ví dụ: _Bootstrap) trong scene khởi động.
// Nó thiết lập trục sắp xếp trong suốt để sprite có Y thấp (ví dụ y=-2) vẽ "trên" (ở phía trước).
public class TransparencySortBootstrap : MonoBehaviour
{
    [Header("Bật nếu muốn ép Custom Axis thay vì để Unity mặc định.")]
    public bool applyOnAwake = true;

    [Tooltip("Chế độ sắp xếp trong suốt. Dùng CustomAxis để so theo vector bên dưới.")]
    public TransparencySortMode mode = TransparencySortMode.CustomAxis;

    [Tooltip("Trục so sánh. (0,1,0): so theo Y. Dùng (0,-1,0) nếu thấy bị đảo.")]
    public Vector3 axis = new Vector3(0, 1, 0);

    void Awake()
    {
        if (applyOnAwake)
        {
            GraphicsSettings.transparencySortMode  = mode;
            GraphicsSettings.transparencySortAxis  = axis;
            Debug.Log($"[TransparencySortBootstrap] Set Mode={mode} Axis={axis}");
        }
    }
}