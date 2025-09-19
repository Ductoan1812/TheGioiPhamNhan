using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Foundation.Utils
{
    /// <summary>
    /// Extension methods để code ngắn gọn và dễ đọc hơn.
    /// Tổng hợp các pattern thường dùng trong dự án.
    /// </summary>
    public static class Extensions
    {
        #region Transform Extensions
        
        /// <summary>
        /// Set position X/Y/Z individually
        /// </summary>
        public static void SetPositionX(this Transform transform, float x)
        {
            var pos = transform.position;
            pos.x = x;
            transform.position = pos;
        }
        
        public static void SetPositionY(this Transform transform, float y)
        {
            var pos = transform.position;
            pos.y = y;
            transform.position = pos;
        }
        
        public static void SetPositionZ(this Transform transform, float z)
        {
            var pos = transform.position;
            pos.z = z;
            transform.position = pos;
        }
        
        /// <summary>
        /// Destroy all children
        /// </summary>
        public static void DestroyAllChildren(this Transform transform)
        {
            var children = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                children.Add(transform.GetChild(i));
            }
            
            foreach (var child in children)
            {
                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }
        }
        
        /// <summary>
        /// Get all children transforms
        /// </summary>
        public static Transform[] GetChildren(this Transform transform)
        {
            var children = new Transform[transform.childCount];
            for (int i = 0; i < transform.childCount; i++)
            {
                children[i] = transform.GetChild(i);
            }
            return children;
        }
        
        /// <summary>
        /// Find child by name recursively
        /// </summary>
        public static Transform FindChildRecursive(this Transform transform, string name)
        {
            var result = transform.Find(name);
            if (result != null) return result;
            
            for (int i = 0; i < transform.childCount; i++)
            {
                result = transform.GetChild(i).FindChildRecursive(name);
                if (result != null) return result;
            }
            
            return null;
        }
        
        #endregion
        
        #region GameObject Extensions
        
        /// <summary>
        /// Get or add component
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
                component = gameObject.AddComponent<T>();
            return component;
        }
        
        /// <summary>
        /// Check if GameObject has component
        /// </summary>
        public static bool HasComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() != null;
        }
        
        /// <summary>
        /// Set layer recursively
        /// </summary>
        public static void SetLayerRecursively(this GameObject gameObject, int layer)
        {
            gameObject.layer = layer;
            foreach (Transform child in gameObject.transform)
            {
                child.gameObject.SetLayerRecursively(layer);
            }
        }
        
        /// <summary>
        /// Set active state if different (avoid unnecessary calls)
        /// </summary>
        public static void SetActiveOptimized(this GameObject gameObject, bool active)
        {
            if (gameObject.activeSelf != active)
                gameObject.SetActive(active);
        }
        
        #endregion
        
        #region Vector Extensions
        
        /// <summary>
        /// Set X/Y/Z components individually
        /// </summary>
        public static Vector3 WithX(this Vector3 vector, float x)
        {
            return new Vector3(x, vector.y, vector.z);
        }
        
        public static Vector3 WithY(this Vector3 vector, float y)
        {
            return new Vector3(vector.x, y, vector.z);
        }
        
        public static Vector3 WithZ(this Vector3 vector, float z)
        {
            return new Vector3(vector.x, vector.y, z);
        }
        
        public static Vector2 WithX(this Vector2 vector, float x)
        {
            return new Vector2(x, vector.y);
        }
        
        public static Vector2 WithY(this Vector2 vector, float y)
        {
            return new Vector2(vector.x, y);
        }
        
        /// <summary>
        /// Convert Vector3 to Vector2 (drop Z)
        /// </summary>
        public static Vector2 ToVector2(this Vector3 vector)
        {
            return new Vector2(vector.x, vector.y);
        }
        
        /// <summary>
        /// Convert Vector2 to Vector3 (add Z)
        /// </summary>
        public static Vector3 ToVector3(this Vector2 vector, float z = 0f)
        {
            return new Vector3(vector.x, vector.y, z);
        }
        
        /// <summary>
        /// Get random point in circle
        /// </summary>
        public static Vector2 RandomPointInCircle(this Vector2 center, float radius)
        {
            var randomPoint = Random.insideUnitCircle * radius;
            return center + randomPoint;
        }
        
        /// <summary>
        /// Clamp magnitude
        /// </summary>
        public static Vector3 ClampMagnitude(this Vector3 vector, float maxMagnitude)
        {
            if (vector.sqrMagnitude > maxMagnitude * maxMagnitude)
                return vector.normalized * maxMagnitude;
            return vector;
        }
        
        public static Vector2 ClampMagnitude(this Vector2 vector, float maxMagnitude)
        {
            if (vector.sqrMagnitude > maxMagnitude * maxMagnitude)
                return vector.normalized * maxMagnitude;
            return vector;
        }
        
        #endregion
        
        #region String Extensions
        
        /// <summary>
        /// Check if string is null or empty or whitespace
        /// </summary>
        public static bool IsNullOrEmpty(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }
        
        /// <summary>
        /// Format string with rich text colors
        /// </summary>
        public static string WithColor(this string str, Color color)
        {
            string hex = ColorUtility.ToHtmlStringRGB(color);
            return $"<color=#{hex}>{str}</color>";
        }
        
        public static string WithColor(this string str, string colorName)
        {
            return $"<color={colorName}>{str}</color>";
        }
        
        /// <summary>
        /// Format string with rich text size
        /// </summary>
        public static string WithSize(this string str, int size)
        {
            return $"<size={size}>{str}</size>";
        }
        
        /// <summary>
        /// Format string with rich text bold
        /// </summary>
        public static string Bold(this string str)
        {
            return $"<b>{str}</b>";
        }
        
        /// <summary>
        /// Format string with rich text italic
        /// </summary>
        public static string Italic(this string str)
        {
            return $"<i>{str}</i>";
        }
        
        /// <summary>
        /// Truncate string to max length
        /// </summary>
        public static string Truncate(this string str, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
                return str;
            
            return str.Substring(0, maxLength - suffix.Length) + suffix;
        }
        
        #endregion
        
        #region Collection Extensions
        
        /// <summary>
        /// Check if collection is null or empty
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            return collection == null || !collection.Any();
        }
        
        /// <summary>
        /// Get random element from collection
        /// </summary>
        public static T GetRandom<T>(this IList<T> list)
        {
            if (list.IsNullOrEmpty()) return default;
            return list[Random.Range(0, list.Count)];
        }
        
        public static T GetRandom<T>(this T[] array)
        {
            if (array.IsNullOrEmpty()) return default;
            return array[Random.Range(0, array.Length)];
        }
        
        /// <summary>
        /// Shuffle collection (Fisher-Yates)
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }
        
        /// <summary>
        /// Remove null elements from list
        /// </summary>
        public static void RemoveNulls<T>(this List<T> list) where T : class
        {
            list.RemoveAll(item => item == null);
        }
        
        /// <summary>
        /// Safe get element at index
        /// </summary>
        public static T SafeGet<T>(this IList<T> list, int index, T defaultValue = default)
        {
            if (list == null || index < 0 || index >= list.Count)
                return defaultValue;
            return list[index];
        }
        
        #endregion
        
        #region LayerMask Extensions
        
        /// <summary>
        /// Check if layer is in LayerMask
        /// </summary>
        public static bool Contains(this LayerMask layerMask, int layer)
        {
            return (layerMask.value & (1 << layer)) != 0;
        }
        
        /// <summary>
        /// Check if GameObject layer is in LayerMask
        /// </summary>
        public static bool Contains(this LayerMask layerMask, GameObject gameObject)
        {
            return layerMask.Contains(gameObject.layer);
        }
        
        #endregion
        
        #region Component Extensions
        
        /// <summary>
        /// Try get component without null check
        /// </summary>
        public static bool TryGetComponent<T>(this GameObject gameObject, out T component) where T : Component
        {
            component = gameObject.GetComponent<T>();
            return component != null;
        }
        
        /// <summary>
        /// Get component in parent or children
        /// </summary>
        public static T GetComponentAnywhere<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component != null) return component;
            
            component = gameObject.GetComponentInParent<T>();
            if (component != null) return component;
            
            return gameObject.GetComponentInChildren<T>();
        }
        
        #endregion
        
        #region Math Extensions
        
        /// <summary>
        /// Remap value from one range to another
        /// </summary>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
        }
        
        /// <summary>
        /// Check if approximately equal (using epsilon)
        /// </summary>
        public static bool Approximately(this float a, float b, float epsilon = GameConstants.Math.EPSILON)
        {
            return Mathf.Abs(a - b) < epsilon;
        }
        
        /// <summary>
        /// Wrap angle to 0-360 range
        /// </summary>
        public static float WrapAngle(this float angle)
        {
            while (angle < 0f) angle += 360f;
            while (angle >= 360f) angle -= 360f;
            return angle;
        }
        
        /// <summary>
        /// Get shortest angle between two angles
        /// </summary>
        public static float AngleDifference(this float angle1, float angle2)
        {
            float diff = (angle2 - angle1).WrapAngle();
            if (diff > 180f) diff -= 360f;
            return diff;
        }
        
        #endregion
    }
}