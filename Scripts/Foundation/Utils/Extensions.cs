using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Foundation.Utils
{
    /// <summary>
    /// Extension methods for common Unity and C# types
    /// </summary>
    public static class Extensions
    {
        #region Vector Extensions
        
        /// <summary>
        /// Get 2D distance ignoring Y axis
        /// </summary>
        public static float Distance2D(this Vector3 from, Vector3 to)
        {
            var fromFlat = new Vector3(from.x, 0, from.z);
            var toFlat = new Vector3(to.x, 0, to.z);
            return Vector3.Distance(fromFlat, toFlat);
        }

        /// <summary>
        /// Get direction to target, normalized
        /// </summary>
        public static Vector3 DirectionTo(this Vector3 from, Vector3 to)
        {
            return (to - from).normalized;
        }

        /// <summary>
        /// Get direction to target in 2D (ignoring Y)
        /// </summary>
        public static Vector3 DirectionTo2D(this Vector3 from, Vector3 to)
        {
            var direction = to - from;
            direction.y = 0;
            return direction.normalized;
        }

        /// <summary>
        /// Check if vector is approximately zero
        /// </summary>
        public static bool IsZero(this Vector3 vector, float threshold = 0.01f)
        {
            return vector.magnitude < threshold;
        }
        
        #endregion

        #region Transform Extensions
        
        /// <summary>
        /// Reset transform to default values
        /// </summary>
        public static void Reset(this Transform transform)
        {
            transform.position = Vector3.zero;
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one;
        }

        /// <summary>
        /// Get all children transforms
        /// </summary>
        public static List<Transform> GetChildren(this Transform transform)
        {
            var children = new List<Transform>();
            for (int i = 0; i < transform.childCount; i++)
            {
                children.Add(transform.GetChild(i));
            }
            return children;
        }

        /// <summary>
        /// Destroy all child objects
        /// </summary>
        public static void DestroyChildren(this Transform transform)
        {
            var children = transform.GetChildren();
            foreach (var child in children)
            {
                if (Application.isPlaying)
                    Object.Destroy(child.gameObject);
                else
                    Object.DestroyImmediate(child.gameObject);
            }
        }
        
        #endregion

        #region GameObject Extensions
        
        /// <summary>
        /// Get component or add if it doesn't exist
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            var component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Check if GameObject has component
        /// </summary>
        public static bool HasComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject.GetComponent<T>() != null;
        }
        
        #endregion

        #region Collection Extensions
        
        /// <summary>
        /// Get random element from collection
        /// </summary>
        public static T GetRandomElement<T>(this IEnumerable<T> collection)
        {
            var list = collection.ToList();
            if (list.Count == 0) return default(T);
            
            var randomIndex = Random.Range(0, list.Count);
            return list[randomIndex];
        }

        /// <summary>
        /// Check if collection is null or empty
        /// </summary>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            return collection == null || !collection.Any();
        }

        /// <summary>
        /// Shuffle collection in place
        /// </summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int randomIndex = Random.Range(0, i + 1);
                (list[i], list[randomIndex]) = (list[randomIndex], list[i]);
            }
        }
        
        #endregion

        #region String Extensions
        
        /// <summary>
        /// Truncate string to specified length
        /// </summary>
        public static string TruncateToLength(this string str, int maxLength, string suffix = "...")
        {
            if (string.IsNullOrEmpty(str) || str.Length <= maxLength)
                return str;
            
            return str.Substring(0, maxLength - suffix.Length) + suffix;
        }

        /// <summary>
        /// Check if string contains any of the specified values
        /// </summary>
        public static bool ContainsAny(this string str, params string[] values)
        {
            return values.Any(value => str.Contains(value));
        }
        
        #endregion

        #region Numeric Extensions
        
        /// <summary>
        /// Remap value from one range to another
        /// </summary>
        public static float Remap(this float value, float fromMin, float fromMax, float toMin, float toMax)
        {
            return toMin + (value - fromMin) * (toMax - toMin) / (fromMax - fromMin);
        }

        /// <summary>
        /// Check if value is between min and max (inclusive)
        /// </summary>
        public static bool IsBetween(this float value, float min, float max)
        {
            return value >= min && value <= max;
        }

        /// <summary>
        /// Clamp value between 0 and 1
        /// </summary>
        public static float Clamp01(this float value)
        {
            return Mathf.Clamp01(value);
        }
        
        #endregion
    }
}
