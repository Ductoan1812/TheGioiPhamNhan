using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Gắn lên Portal (có Collider2D IsTrigger). Khi Player (tag "Player") chạm sẽ load scene khác.
/// </summary>
[DisallowMultipleComponent]
public class ScenePortal : MonoBehaviour
{
    [Header("Scene đích")]
    [Tooltip("Nếu nhập tên hợp lệ, sẽ ưu tiên theo tên scene.")]
    [SerializeField] private string targetSceneName;
    [Tooltip("Dùng build index nếu không dùng tên scene hoặc tên trống.")]
    [SerializeField] private int targetBuildIndex = -1;

    [Header("Tùy chọn")]
    [SerializeField] private bool loadAsync = true;
    [SerializeField, Tooltip("Trì hoãn trước khi load (giây)")] private float delayBeforeLoad = 0f;
    [SerializeField, Tooltip("Vô hiệu hóa portal sau khi sử dụng để tránh load lặp")] private bool disableAfterUse = true;

    private bool isLoading;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isLoading) return;
        if (!other.CompareTag("Player")) return;

        // Basic validation
        bool hasName = !string.IsNullOrWhiteSpace(targetSceneName);
        bool hasIndex = targetBuildIndex >= 0 && targetBuildIndex < SceneManager.sceneCountInBuildSettings;
        if (!hasName && !hasIndex)
        {
            Debug.LogWarning("ScenePortal: Chưa cấu hình scene đích (tên rỗng và build index < 0)");
            return;
        }

        if (disableAfterUse)
        {
            var col = GetComponent<Collider2D>();
            if (col) col.enabled = false;
        }

        isLoading = true;
        if (loadAsync)
        {
            StartCoroutine(LoadRoutine(hasName, hasIndex));
        }
        else
        {
            if (delayBeforeLoad > 0f) StartCoroutine(DelayThenLoad(hasName, hasIndex));
            else LoadImmediate(hasName, hasIndex);
        }
    }

    private IEnumerator LoadRoutine(bool hasName, bool hasIndex)
    {
        if (delayBeforeLoad > 0f) yield return new WaitForSeconds(delayBeforeLoad);
        AsyncOperation op = hasName
            ? SceneManager.LoadSceneAsync(targetSceneName, LoadSceneMode.Single)
            : SceneManager.LoadSceneAsync(targetBuildIndex, LoadSceneMode.Single);
        if (op != null)
        {
            op.allowSceneActivation = true; // có thể tuỳ biến nếu cần màn hình loading
            while (!op.isDone) yield return null;
        }
    }

    private IEnumerator DelayThenLoad(bool hasName, bool hasIndex)
    {
        yield return new WaitForSeconds(delayBeforeLoad);
        LoadImmediate(hasName, hasIndex);
    }

    private void LoadImmediate(bool hasName, bool hasIndex)
    {
        if (hasName) SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
        else if (hasIndex) SceneManager.LoadScene(targetBuildIndex, LoadSceneMode.Single);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.7f);
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 1f, 0f));
        var label = !string.IsNullOrWhiteSpace(targetSceneName) ? targetSceneName : (targetBuildIndex >= 0 ? $"BuildIndex {targetBuildIndex}" : "<chưa set>");
        UnityEditor.Handles.color = Gizmos.color;
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.6f, $"Portal → {label}");
    }
#endif
}
