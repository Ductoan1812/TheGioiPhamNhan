using UnityEngine;

namespace Foundation.Utils
{
    /// <summary>
    /// Debug utilities với conditional compilation.
    /// Chỉ hoạt động trong Development builds.
    /// </summary>
    public static class DebugUtils
    {
        private static bool isDebugEnabled = Debug.isDebugBuild;
        
        #region Conditional Logging
        
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void Log(object message, Object context = null)
        {
            if (!isDebugEnabled) return;
            Debug.Log($"[DEBUG] {message}", context);
        }
        
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogWarning(object message, Object context = null)
        {
            if (!isDebugEnabled) return;
            Debug.LogWarning($"[DEBUG] {message}", context);
        }
        
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogError(object message, Object context = null)
        {
            Debug.LogError($"[DEBUG] {message}", context);
        }
        
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        public static void LogFormat(string format, params object[] args)
        {
            if (!isDebugEnabled) return;
            Debug.LogFormat($"[DEBUG] {format}", args);
        }
        
        #endregion
        
        #region Gizmo Helpers
        
        public static void DrawWireSphere(Vector3 center, float radius, Color color, float duration = 0f)
        {
            #if UNITY_EDITOR
            var oldColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawWireSphere(center, radius);
            Gizmos.color = oldColor;
            #endif
        }
        
        public static void DrawWireCube(Vector3 center, Vector3 size, Color color)
        {
            #if UNITY_EDITOR
            var oldColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawWireCube(center, size);
            Gizmos.color = oldColor;
            #endif
        }
        
        public static void DrawLine(Vector3 start, Vector3 end, Color color, float duration = 0f)
        {
            #if UNITY_EDITOR
            if (Application.isPlaying)
                Debug.DrawLine(start, end, color, duration);
            else
            {
                var oldColor = Gizmos.color;
                Gizmos.color = color;
                Gizmos.DrawLine(start, end);
                Gizmos.color = oldColor;
            }
            #endif
        }
        
        public static void DrawCircle(Vector3 center, float radius, Color color, int segments = 32)
        {
            #if UNITY_EDITOR
            var oldColor = Gizmos.color;
            Gizmos.color = color;
            
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + Vector3.right * radius;
            
            for (int i = 1; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                Vector3 newPoint = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
                Gizmos.DrawLine(prevPoint, newPoint);
                prevPoint = newPoint;
            }
            
            Gizmos.color = oldColor;
            #endif
        }
        
        #endregion
        
        #region Performance Monitoring
        
        public static void LogPerformance(string operation, System.Action action)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            action?.Invoke();
            stopwatch.Stop();
            Log($"Performance [{operation}]: {stopwatch.ElapsedMilliseconds}ms");
            #else
            action?.Invoke();
            #endif
        }
        
        public static T LogPerformance<T>(string operation, System.Func<T> func)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            T result = func != null ? func() : default;
            stopwatch.Stop();
            Log($"Performance [{operation}]: {stopwatch.ElapsedMilliseconds}ms");
            return result;
            #else
            return func != null ? func() : default;
            #endif
        }
        
        #endregion
        
        #region Memory Monitoring
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void LogMemoryUsage(string context = "")
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            long totalMemory = System.GC.GetTotalMemory(false);
            float memoryMB = totalMemory / (1024f * 1024f);
            Log($"Memory Usage {context}: {memoryMB:F2} MB");
            #endif
        }
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void ForceGarbageCollection(string context = "")
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            var beforeMemory = System.GC.GetTotalMemory(false);
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            System.GC.Collect();
            var afterMemory = System.GC.GetTotalMemory(false);
            
            float freedMB = (beforeMemory - afterMemory) / (1024f * 1024f);
            Log($"GC {context}: Freed {freedMB:F2} MB");
            #endif
        }
        
        #endregion
        
        #region Assertion Helpers
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void Assert(bool condition, string message, Object context = null)
        {
            if (!condition)
            {
                LogError($"Assertion Failed: {message}", context);
                #if UNITY_EDITOR
                Debug.Break();
                #endif
            }
        }
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void AssertNotNull(Object obj, string name, Object context = null)
        {
            Assert(obj != null, $"{name} is null", context);
        }
        
        [System.Diagnostics.Conditional("UNITY_EDITOR")]
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        public static void AssertNotNullOrEmpty(string str, string name, Object context = null)
        {
            Assert(!string.IsNullOrEmpty(str), $"{name} is null or empty", context);
        }
        
        #endregion
        
        #region Debug UI
        
        private static GUIStyle debugStyle;
        
        public static void DrawDebugGUI(System.Action drawAction)
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!isDebugEnabled) return;
            
            if (debugStyle == null)
            {
                debugStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 12,
                    normal = { textColor = Color.white }
                };
            }
            
            GUI.color = Color.white;
            drawAction?.Invoke();
            #endif
        }
        
        public static void DrawFPS()
        {
            #if UNITY_EDITOR || DEVELOPMENT_BUILD
            DrawDebugGUI(() =>
            {
                float fps = 1f / Time.unscaledDeltaTime;
                Color color = fps > 45f ? Color.green : fps > 30f ? Color.yellow : Color.red;
                
                GUI.color = color;
                GUI.Label(new Rect(10, 10, 200, 30), $"FPS: {fps:F1}", debugStyle);
            });
            #endif
        }
        
        #endregion
        
        #region Settings
        
        public static void SetDebugEnabled(bool enabled)
        {
            isDebugEnabled = enabled && Debug.isDebugBuild;
        }
        
        public static bool IsDebugEnabled => isDebugEnabled;
        
        #endregion
    }
}