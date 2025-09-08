using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Xianxia.PlayerDataSystem;

namespace Xianxia.PlayerDataSystem
{
    // Extension helpers for EquipmentData to work even if core class lacks APIs at compile time
    public static class EquipmentDataExtensions
    {
    private static FieldInfo fiSlots;
    private static FieldInfo fiSlotId;
    private static FieldInfo fiSlotItem;
    private static Type slotType;

        private static bool EnsureReflection(EquipmentData data)
        {
            if (data == null) return false;
            var t = data.GetType();
            if (fiSlots == null)
            {
                fiSlots = t.GetField("_slots", BindingFlags.Instance | BindingFlags.NonPublic)
                       ?? t.GetField("slots", BindingFlags.Instance | BindingFlags.NonPublic)
                       ?? t.GetField("Slots", BindingFlags.Instance | BindingFlags.NonPublic);

                if (fiSlots == null)
                {
                    // Fallback: scan fields to find a List<Slot>
                    foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        var ft = f.FieldType;
                        if (ft.IsGenericType && typeof(IList).IsAssignableFrom(ft))
                        {
                            var ga = ft.GetGenericArguments()[0];
                            if (ga.Name == "Slot")
                            {
                                fiSlots = f; break;
                            }
                        }
                    }
                }
            }
            if (fiSlots == null)
            {
                Debug.LogWarning("[EquipmentDataExtensions] Cannot find slots list field on EquipmentData");
                return false;
            }

            if (slotType == null)
            {
                // find nested type named "Slot" or infer from list generic type
                foreach (var nt in t.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (nt.Name == "Slot") { slotType = nt; break; }
                }
                if (slotType == null)
                {
                    var ft = fiSlots.FieldType;
                    if (ft.IsGenericType)
                        slotType = ft.GetGenericArguments()[0];
                }
                if (slotType == null)
                {
                    Debug.LogWarning("[EquipmentDataExtensions] Cannot determine Slot type");
                    return false;
                }
                fiSlotId = slotType.GetField("idSlot", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                        ?? slotType.GetField("id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                fiSlotItem = slotType.GetField("item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            }
            if (fiSlotId == null || fiSlotItem == null)
            {
                // Fallback: scan fields by type
                foreach (var f in slotType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (fiSlotId == null && f.FieldType == typeof(string)) fiSlotId = f;
                    if (fiSlotItem == null && f.FieldType == typeof(InventoryItem)) fiSlotItem = f;
                }
            }
            bool ok = fiSlotId != null && fiSlotItem != null;
            if (!ok)
                Debug.LogWarning("[EquipmentDataExtensions] Cannot find fields 'idSlot'/'item' on Slot type");
            return ok;
        }

        public static IEnumerable<(string, InventoryItem)> EnumerateSlots(this EquipmentData data)
        {
            if (!EnsureReflection(data)) yield break;
            var listObj = fiSlots.GetValue(data) as IEnumerable;
            if (listObj == null) yield break;
            foreach (var s in listObj)
            {
                var id = fiSlotId.GetValue(s) as string;
                var item = fiSlotItem.GetValue(s) as InventoryItem;
                yield return (id, item);
            }
        }

        public static bool TryGet(this EquipmentData data, string slotId, out InventoryItem item)
        {
            item = null;
            if (string.IsNullOrEmpty(slotId)) return false;
            foreach (var pair in data.EnumerateSlots())
            {
                if (string.Equals(pair.Item1, slotId, StringComparison.OrdinalIgnoreCase))
                {
                    item = pair.Item2; return true;
                }
            }
            return false;
        }

        public static bool Equip(this EquipmentData data, string slotId, InventoryItem newItem, bool overwrite = true)
        {
            if (!EnsureReflection(data) || string.IsNullOrEmpty(slotId)) return false;
            // Try find existing slot
            IList list = fiSlots.GetValue(data) as IList;
            if (list == null)
            {
                // Try to create the list if it's null
                var listType = typeof(List<>).MakeGenericType(slotType);
                list = (IList)Activator.CreateInstance(listType);
                fiSlots.SetValue(data, list);
            }
            object targetSlot = null;
            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                var id = fiSlotId.GetValue(s) as string;
                if (string.Equals(id, slotId, StringComparison.OrdinalIgnoreCase))
                {
                    targetSlot = s; break;
                }
            }
            if (targetSlot == null)
            {
                targetSlot = Activator.CreateInstance(slotType);
                fiSlotId.SetValue(targetSlot, slotId);
                list.Add(targetSlot);
            }
            var cur = fiSlotItem.GetValue(targetSlot) as InventoryItem;
            if (!overwrite && cur != null) return false;
            fiSlotItem.SetValue(targetSlot, newItem);
            return true;
        }

        public static InventoryItem Unequip(this EquipmentData data, string slotId)
        {
            if (!EnsureReflection(data) || string.IsNullOrEmpty(slotId)) return null;
            IList list = fiSlots.GetValue(data) as IList;
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                var id = fiSlotId.GetValue(s) as string;
                if (string.Equals(id, slotId, StringComparison.OrdinalIgnoreCase))
                {
                    var old = fiSlotItem.GetValue(s) as InventoryItem;
                    fiSlotItem.SetValue(s, null);
                    return old;
                }
            }
            return null;
        }
    }
}
