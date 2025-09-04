#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Xianxia.Items;

[CustomEditor(typeof(ItemSpawner))]
public class ItemSpawnerEditor : Editor
{
    private ReorderableList _list;
    private SerializedProperty _databaseProp;
    private SerializedProperty _itemPrefabProp;
    private SerializedProperty _initialSpawnsProp;

    // Cache combobox
    private string[] _idOptions = Array.Empty<string>();
    private GUIContent _spawnHeader = new GUIContent("Initial Spawns");

    // Foldout trạng thái theo phần tử
    private readonly Dictionary<string, bool> _foldoutStates = new Dictionary<string, bool>();

    private void OnEnable()
    {
        _databaseProp = serializedObject.FindProperty("database");
        _itemPrefabProp = serializedObject.FindProperty("itemPrefab");
        _initialSpawnsProp = serializedObject.FindProperty("initialSpawns");

        BuildIdOptions();

        _list = new ReorderableList(serializedObject, _initialSpawnsProp, true, true, true, true);
        _list.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, _spawnHeader);
        };

        _list.elementHeightCallback = index =>
        {
            var element = _initialSpawnsProp.GetArrayElementAtIndex(index);
            float h = 0f;
            h += EditorGUIUtility.singleLineHeight + 6; // itemId popup
            h += EditorGUIUtility.singleLineHeight + 2; // quantity
            h += EditorGUIUtility.singleLineHeight + 6; // position

            // Foldout
            var key = element.propertyPath + "_fold";
            bool expanded = _foldoutStates.TryGetValue(key, out var state) && state;
            h += EditorGUIUtility.singleLineHeight + 4;
            if (expanded)
            {
                // level, rarity, element, quality, durability
                h += (EditorGUIUtility.singleLineHeight + 2) * 5;

                // baseStats, affixes, tags, custom
                h += EditorGUI.GetPropertyHeight(element.FindPropertyRelative("baseStats"), true) + 6;
                h += EditorGUI.GetPropertyHeight(element.FindPropertyRelative("affixes"), true) + 6;
                h += EditorGUI.GetPropertyHeight(element.FindPropertyRelative("tags"), true) + 6;
                h += EditorGUI.GetPropertyHeight(element.FindPropertyRelative("custom"), true) + 6;
            }
            return h + 6;
        };

        _list.drawElementCallback = (rect, index, active, focused) =>
        {
            var element = _initialSpawnsProp.GetArrayElementAtIndex(index);
            var r = new Rect(rect.x, rect.y + 2, rect.width, EditorGUIUtility.singleLineHeight);

            // 1) Item combobox
            var idProp = element.FindPropertyRelative("itemId");
            int curIndex = Mathf.Max(0, Array.IndexOf(_idOptions, string.IsNullOrEmpty(idProp.stringValue) ? "" : idProp.stringValue));
            int newIndex = EditorGUI.Popup(r, "Item", curIndex, _idOptions);
            if (newIndex >= 0 && newIndex < _idOptions.Length)
                idProp.stringValue = _idOptions[newIndex];

            // 2) Quantity
            r.y += r.height + 2;
            var qtyProp = element.FindPropertyRelative("quantity");
            qtyProp.intValue = Mathf.Max(1, EditorGUI.IntField(r, "Quantity", qtyProp.intValue));

            // 3) Position
            r.y += r.height + 6;
            var posProp = element.FindPropertyRelative("position");
            posProp.vector3Value = EditorGUI.Vector3Field(r, "Position", posProp.vector3Value);

            // 4) Foldout Variants
            r.y += r.height + 4;
            var key = element.propertyPath + "_fold";
            bool expanded = _foldoutStates.TryGetValue(key, out var state) && state;
            expanded = EditorGUI.Foldout(r, expanded, "Variant", true);
            _foldoutStates[key] = expanded;

            if (expanded)
            {
                // Simple fields
                r.y += r.height + 2;
                var levelProp = element.FindPropertyRelative("level");
                levelProp.intValue = EditorGUI.IntField(r, "Level", levelProp.intValue);

                r.y += r.height + 2;
                var rarityProp = element.FindPropertyRelative("rarity");
                rarityProp.stringValue = EditorGUI.TextField(r, "Rarity", rarityProp.stringValue);

                r.y += r.height + 2;
                var elementProp = element.FindPropertyRelative("element");
                elementProp.stringValue = EditorGUI.TextField(r, "Element", elementProp.stringValue);

                r.y += r.height + 2;
                var qualityProp = element.FindPropertyRelative("quality");
                qualityProp.intValue = EditorGUI.IntSlider(r, new GUIContent("Quality"), qualityProp.intValue, 0, 100);

                r.y += r.height + 2;
                var duraProp = element.FindPropertyRelative("durability");
                duraProp.floatValue = EditorGUI.FloatField(r, "Durability", duraProp.floatValue);

                // Arrays/complex
                r.y += r.height + 6;
                var baseStatsProp = element.FindPropertyRelative("baseStats");
                float h1 = EditorGUI.GetPropertyHeight(baseStatsProp, true);
                EditorGUI.PropertyField(new Rect(r.x, r.y, r.width, h1), baseStatsProp, new GUIContent("Base Stats"), true);
                r.y += h1 + 6;

                var affixesProp = element.FindPropertyRelative("affixes");
                float h2 = EditorGUI.GetPropertyHeight(affixesProp, true);
                EditorGUI.PropertyField(new Rect(r.x, r.y, r.width, h2), affixesProp, new GUIContent("Affixes"), true);
                r.y += h2 + 6;

                var tagsProp = element.FindPropertyRelative("tags");
                float h3 = EditorGUI.GetPropertyHeight(tagsProp, true);
                EditorGUI.PropertyField(new Rect(r.x, r.y, r.width, h3), tagsProp, new GUIContent("Tags"), true);
                r.y += h3 + 6;

                var customProp = element.FindPropertyRelative("custom");
                float h4 = EditorGUI.GetPropertyHeight(customProp, true);
                EditorGUI.PropertyField(new Rect(r.x, r.y, r.width, h4), customProp, new GUIContent("Custom Props"), true);
            }
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_itemPrefabProp);
        EditorGUILayout.PropertyField(_databaseProp);

        EditorGUILayout.Space(6);
        if (GUILayout.Button("Refresh Item Ids (from Database)"))
        {
            BuildIdOptions();
        }

        EditorGUILayout.Space(6);
        _list.DoLayoutList();

        serializedObject.ApplyModifiedProperties();
    }

    private void BuildIdOptions()
    {
        var spawner = (ItemSpawner)target;
        ItemDatabaseSO db = spawner != null ? spawner.DatabaseOrInstance : null;

        // Mặc định 1 option rỗng để khỏi crash Dropdown
        _idOptions = new[] { "" };

        if (db == null)
            return;

        // Cách 1: Đọc trực tiếp mảng "items" (Serialized) nếu có
        try
        {
            var so = new SerializedObject(db);
            var itemsProp = so.FindProperty("items");
            if (itemsProp != null && itemsProp.isArray && itemsProp.arraySize >= 0)
            {
                var list = new List<string>();
                for (int i = 0; i < itemsProp.arraySize; i++)
                {
                    var elem = itemsProp.GetArrayElementAtIndex(i);
                    var idProp = elem.FindPropertyRelative("id");
                    if (idProp != null)
                    {
                        var id = idProp.stringValue ?? "";
                        if (!string.IsNullOrEmpty(id))
                            list.Add(id);
                    }
                }
                list = list.Distinct().OrderBy(s => s, StringComparer.Ordinal).ToList();
                if (list.Count > 0) { _idOptions = list.ToArray(); return; }
            }
        }
        catch { /* ignore and fallback */ }

        // Cách 2: Reflection mềm dẻo — không cần method tồn tại ở compile-time
        try
        {
            var ids = TryGetIdsViaReflection(db);
            if (ids != null && ids.Length > 0)
            {
                _idOptions = ids;
                return;
            }
        }
        catch { /* ignore */ }
    }

    private static string[] TryGetIdsViaReflection(ItemDatabaseSO db)
    {
        var t = db.GetType();
        var ids = new List<string>();

        // 2.1. Tìm method trả về string[] hoặc IEnumerable<string>
        string[] methodCandidates = { "GetAllIds", "AllIds", "Ids", "ListIds" };
        foreach (var name in methodCandidates)
        {
            var m = t.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (m == null) continue;

            object result = m.IsStatic ? m.Invoke(null, null) : m.Invoke(db, null);
            if (result is string[] sa && sa.Length > 0)
                return sa.OrderBy(s => s, StringComparer.Ordinal).ToArray();
            if (result is IEnumerable<string> se && se.Any())
                return se.OrderBy(s => s, StringComparer.Ordinal).ToArray();
        }

        // 2.2. Tìm method trả về IEnumerable<ItemData> / IEnumerable<any có field 'id'>
        string[] listCandidates = { "GetAll", "All", "GetItems", "List", "GetAllItems" };
        foreach (var name in listCandidates)
        {
            var m = t.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (m == null) continue;

            object result = m.IsStatic ? m.Invoke(null, null) : m.Invoke(db, null);
            if (result is IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    var id = ReadIdByReflection(item);
                    if (!string.IsNullOrEmpty(id)) ids.Add(id);
                }
                if (ids.Count > 0) return ids.Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();
            }
        }

        // 2.3. Field/Property "items" (runtime — không qua SerializedObject)
        var itemsField = t.GetField("items", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        if (itemsField != null)
        {
            var value = itemsField.GetValue(db);
            CollectIdsFromEnumerable(value, ids);
            if (ids.Count > 0) return ids.Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();
        }

        var itemsProp = t.GetProperty("items", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        if (itemsProp != null)
        {
            var value = itemsProp.GetValue(db);
            CollectIdsFromEnumerable(value, ids);
            if (ids.Count > 0) return ids.Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();
        }

        // 2.4. Dictionary<string, ItemData> kiểu 'dict' hoặc 'map'
        foreach (var name in new[] { "dict", "map", "lookup", "byId" })
        {
            var f = t.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (f != null)
            {
                var value = f.GetValue(db);
                CollectIdsFromDictionary(value, ids);
            }

            var p = t.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            if (p != null)
            {
                var value = p.GetValue(db);
                CollectIdsFromDictionary(value, ids);
            }
        }
        if (ids.Count > 0) return ids.Distinct().OrderBy(s => s, StringComparer.Ordinal).ToArray();

        return Array.Empty<string>();
    }

    private static void CollectIdsFromEnumerable(object value, List<string> ids)
    {
        if (value is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                var id = ReadIdByReflection(item);
                if (!string.IsNullOrEmpty(id)) ids.Add(id);
            }
        }
    }

    private static void CollectIdsFromDictionary(object value, List<string> ids)
    {
        if (value is IDictionary dict)
        {
            foreach (DictionaryEntry kv in dict)
            {
                if (kv.Key is string s) ids.Add(s);
                else
                {
                    var id = ReadIdByReflection(kv.Value);
                    if (!string.IsNullOrEmpty(id)) ids.Add(id);
                }
            }
        }
    }

    private static string ReadIdByReflection(object item)
    {
        if (item == null) return null;

        // Nếu là ItemData, đọc trực tiếp property/field 'id'
        var t = item.GetType();
        var pf = t.GetField("id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (pf != null && pf.FieldType == typeof(string))
            return (string)pf.GetValue(item);

        var pp = t.GetProperty("id", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (pp != null && pp.PropertyType == typeof(string))
            return (string)pp.GetValue(item);

        return null;
    }
}
#endif