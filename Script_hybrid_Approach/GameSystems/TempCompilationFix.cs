// GameSystems Compilation Fix Script
// This script temporarily disables problematic code to allow compilation
// TODO: Fix all API calls properly after basic compilation works

using System;
using UnityEngine;

namespace GameSystems.CompilationFix
{
    /// <summary>
    /// Temporary fix class to help compilation
    /// Remove this after proper fixes are implemented
    /// </summary>
    public static class TempCompilationFix
    {
        public static void LogInfo(string message, bool enabled = true)
        {
            if (enabled)
            {
                Debug.Log($"[GameSystems] {message}");
            }
        }
        
        public static void LogWarning(string message, bool enabled = true) 
        {
            if (enabled)
            {
                Debug.LogWarning($"[GameSystems] {message}");
            }
        }
        
        public static void LogError(string message, bool enabled = true)
        {
            if (enabled)
            {
                Debug.LogError($"[GameSystems] {message}");
            }
        }
    }
    
    /// <summary>
    /// TODO: Remove after proper ItemDefinition properties are added
    /// </summary>
    public static class ItemDefinitionExtensions
    {
        public static string GetName(this object item) => "TempName";
        public static string GetIcon(this object item) => "TempIcon"; 
        public static bool GetIsConsumable(this object item) => false;
        public static string GetItemUseEffect(this object item) => "None";
        public static bool IsValidForEquipmentSlot(this object item, object slot) => true;
        public static float GetPowerRating(this object item) => 1.0f;
        public static object[] GetItemStats(this object item) => new object[0];
        public static object[] GetItemResistances(this object item) => new object[0];
    }
}
