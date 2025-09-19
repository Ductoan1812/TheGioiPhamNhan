using System;
using System.Collections.Generic;
using UnityEngine;

namespace Foundation.Data
{
    /// <summary>
    /// Serialization utilities - cải tiến từ JSON handling hiện tại.
    /// Thêm error handling, versioning, và backup functionality.
    /// </summary>
    public static class SerializationHelper
    {
        private const string BACKUP_SUFFIX = ".backup";
        
        #region JSON Serialization
        
        /// <summary>
        /// Save object to JSON file với backup
        /// </summary>
        public static bool SaveToJson<T>(T obj, string filePath, bool createBackup = true)
        {
            try
            {
                // Create backup if requested
                if (createBackup && System.IO.File.Exists(filePath))
                {
                    CreateBackup(filePath);
                }
                
                string json = JsonUtility.ToJson(obj, true);
                System.IO.File.WriteAllText(filePath, json);
                
                Debug.Log($"[SerializationHelper] Saved {typeof(T).Name} to {filePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SerializationHelper] Failed to save {typeof(T).Name}: {e.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// Load object from JSON file với fallback tới backup
        /// </summary>
        public static bool LoadFromJson<T>(string filePath, out T result) where T : new()
        {
            result = default;
            
            // Try main file first
            if (TryLoadJsonFile(filePath, out result))
            {
                return true;
            }
            
            // Try backup file
            string backupPath = filePath + BACKUP_SUFFIX;
            if (TryLoadJsonFile(backupPath, out result))
            {
                Debug.LogWarning($"[SerializationHelper] Loaded from backup: {backupPath}");
                return true;
            }
            
            // Create default if no file exists
            result = new T();
            Debug.LogWarning($"[SerializationHelper] No file found, created default {typeof(T).Name}");
            return false;
        }
        
        private static bool TryLoadJsonFile<T>(string filePath, out T result)
        {
            result = default;
            
            try
            {
                if (!System.IO.File.Exists(filePath))
                    return false;
                
                string json = System.IO.File.ReadAllText(filePath);
                result = JsonUtility.FromJson<T>(json);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SerializationHelper] Failed to load from {filePath}: {e.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Backup Management
        
        private static void CreateBackup(string originalPath)
        {
            try
            {
                string backupPath = originalPath + BACKUP_SUFFIX;
                System.IO.File.Copy(originalPath, backupPath, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SerializationHelper] Failed to create backup: {e.Message}");
            }
        }
        
        public static bool RestoreFromBackup(string originalPath)
        {
            try
            {
                string backupPath = originalPath + BACKUP_SUFFIX;
                if (!System.IO.File.Exists(backupPath))
                {
                    Debug.LogWarning($"[SerializationHelper] No backup found: {backupPath}");
                    return false;
                }
                
                System.IO.File.Copy(backupPath, originalPath, true);
                Debug.Log($"[SerializationHelper] Restored from backup: {originalPath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SerializationHelper] Failed to restore backup: {e.Message}");
                return false;
            }
        }
        
        #endregion
        
        #region Data Validation
        
        /// <summary>
        /// Validate JSON structure before parsing
        /// </summary>
        public static bool IsValidJson(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return false;
            
            try
            {
                var obj = JsonUtility.FromJson<object>(json);
                return true;
            }
            catch
            {
                return false;
            }
        }
        
        /// <summary>
        /// Get JSON size in bytes
        /// </summary>
        public static long GetJsonSize<T>(T obj)
        {
            try
            {
                string json = JsonUtility.ToJson(obj);
                return System.Text.Encoding.UTF8.GetByteCount(json);
            }
            catch
            {
                return -1;
            }
        }
        
        #endregion
        
        #region Path Utilities
        
        /// <summary>
        /// Get save path in persistent data directory
        /// </summary>
        public static string GetSavePath(string fileName)
        {
            return System.IO.Path.Combine(Application.persistentDataPath, fileName);
        }
        
        /// <summary>
        /// Get save path in streaming assets (read-only)
        /// </summary>
        public static string GetStreamingAssetsPath(string fileName)
        {
            return System.IO.Path.Combine(Application.streamingAssetsPath, fileName);
        }
        
        /// <summary>
        /// Ensure directory exists
        /// </summary>
        public static void EnsureDirectoryExists(string filePath)
        {
            string directory = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// Wrapper cho versioned data - helpful khi cần migrate data structure
    /// </summary>
    [Serializable]
    public class VersionedData<T>
    {
        public int version;
        public T data;
        
        public VersionedData(T data, int version = 1)
        {
            this.data = data;
            this.version = version;
        }
    }
    
    /// <summary>
    /// Simple checksum for data integrity
    /// </summary>
    [Serializable]
    public class ChecksummedData<T>
    {
        public T data;
        public string checksum;
        
        public ChecksummedData(T data)
        {
            this.data = data;
            this.checksum = CalculateChecksum(data);
        }
        
        public bool IsValid()
        {
            return checksum == CalculateChecksum(data);
        }
        
        private string CalculateChecksum(T obj)
        {
            try
            {
                string json = JsonUtility.ToJson(obj);
                return json.GetHashCode().ToString();
            }
            catch
            {
                return "invalid";
            }
        }
    }
}