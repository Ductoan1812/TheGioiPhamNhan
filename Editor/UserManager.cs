using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class UserManager : EditorWindow
{
    // Default folder as requested
    private const string DefaultUserFolder = "C:/Users/baolu_cvd7kdo/AppData/LocalLow/DefaultCompany/demo2";

    private string userFolder = DefaultUserFolder;
    private bool recursive = false;
    private string search = "";

    private Vector2 leftScroll;
    private Vector2 rightScroll;

    private List<string> jsonFiles = new List<string>();
    private int selectedIndex = -1;
    private string selectedPath = null;
    private string jsonText = "";
    private bool dirty = false;

    [MenuItem("Tools/User Manager")]
    public static void ShowWindow()
    {
        GetWindow<UserManager>("User Manager");
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(userFolder)) userFolder = DefaultUserFolder;
        RefreshList();
    }

    private void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        DrawFilesList();
        DrawEditor();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        userFolder = GUILayout.TextField(userFolder, EditorStyles.toolbarTextField, GUILayout.MinWidth(300));
        if (GUILayout.Button("Browse", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            var picked = EditorUtility.OpenFolderPanel("Chọn thư mục người chơi", userFolder, "");
            if (!string.IsNullOrEmpty(picked))
            {
                userFolder = picked.Replace('\\', '/');
                RefreshList();
            }
        }
        if (GUILayout.Button("Open Folder", EditorStyles.toolbarButton, GUILayout.Width(100)))
        {
            if (Directory.Exists(userFolder))
            {
                EditorUtility.RevealInFinder(userFolder);
            }
            else
            {
                EditorUtility.DisplayDialog("Không tìm thấy", "Thư mục không tồn tại.", "OK");
            }
        }
        recursive = GUILayout.Toggle(recursive, "Recursive", EditorStyles.toolbarButton, GUILayout.Width(80));
        if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(80))) RefreshList();
        GUILayout.FlexibleSpace();
        search = GUILayout.TextField(search, EditorStyles.toolbarTextField, GUILayout.MinWidth(180));
        if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(20))) search = "";
        EditorGUILayout.EndHorizontal();
    }

    private void DrawFilesList()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(360));
        leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
        IEnumerable<string> list = jsonFiles;
        if (!string.IsNullOrEmpty(search))
        {
            list = list.Where(p => Path.GetFileName(p).IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        int idx = 0;
        foreach (var p in list)
        {
            var isSel = selectedPath == p;
            if (GUILayout.Toggle(isSel, Path.GetFileName(p), "Button"))
            {
                if (!isSel)
                {
                    TrySwitchToFile(p);
                }
            }
            idx++;
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawEditor()
    {
        EditorGUILayout.BeginVertical();
        if (string.IsNullOrEmpty(selectedPath))
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Chọn một file JSON để chỉnh sửa.", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndVertical();
            return;
        }

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"File: {selectedPath}");
        GUILayout.FlexibleSpace();
        GUI.enabled = true;
        if (GUILayout.Button("Reload", GUILayout.Width(80))) LoadSelected();
        GUI.enabled = !string.IsNullOrEmpty(jsonText);
        if (GUILayout.Button("Pretty", GUILayout.Width(80))) { jsonText = PrettyPrintJson(jsonText); dirty = true; }
        GUI.enabled = dirty && !string.IsNullOrEmpty(jsonText);
        if (GUILayout.Button("Save", GUILayout.Width(80))) SaveSelected();
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        rightScroll = EditorGUILayout.BeginScrollView(rightScroll);
        var style = new GUIStyle(EditorStyles.textArea) { wordWrap = false };
        string newText = EditorGUILayout.TextArea(jsonText, style, GUILayout.ExpandHeight(true));
        if (!ReferenceEquals(newText, jsonText) && newText != jsonText)
        {
            jsonText = newText;
            dirty = true;
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.EndVertical();
    }

    private void RefreshList()
    {
        jsonFiles.Clear();
        try
        {
            if (Directory.Exists(userFolder))
            {
                var opt = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                jsonFiles = Directory.GetFiles(userFolder, "*.json", opt)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else
            {
                // Thư mục chưa tồn tại, giữ danh sách trống
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi đọc thư mục: {ex.Message}");
        }

        // Reset selection nếu file hiện tại không còn
        if (!string.IsNullOrEmpty(selectedPath) && !jsonFiles.Contains(selectedPath))
        {
            selectedPath = null;
            jsonText = string.Empty;
            dirty = false;
        }
    }

    private void TrySwitchToFile(string path)
    {
        if (dirty)
        {
            if (!EditorUtility.DisplayDialog("Chưa lưu", "Bạn có thay đổi chưa lưu. Chuyển file sẽ mất thay đổi. Tiếp tục?", "Tiếp tục", "Hủy"))
            {
                return;
            }
        }
        selectedPath = path;
        LoadSelected();
    }

    private void LoadSelected()
    {
        try
        {
            jsonText = File.ReadAllText(selectedPath);
            dirty = false;
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Lỗi", $"Không thể đọc file: {ex.Message}", "OK");
        }
    }

    private void SaveSelected()
    {
        try
        {
            File.WriteAllText(selectedPath, jsonText);
            dirty = false;
            EditorUtility.DisplayDialog("Đã lưu", "Đã lưu JSON.", "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Lỗi", $"Không thể lưu file: {ex.Message}", "OK");
        }
    }

    // Lightweight pretty-printer for JSON strings (handles quotes/escapes)
    private string PrettyPrintJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;
        var sb = new System.Text.StringBuilder();
        bool inQuotes = false;
        bool escape = false;
        int indent = 0;
        foreach (char ch in json)
        {
            if (escape)
            {
                escape = false;
                sb.Append(ch);
                continue;
            }
            if (ch == '\\')
            {
                escape = true;
                sb.Append(ch);
                continue;
            }
            if (ch == '"')
            {
                inQuotes = !inQuotes;
                sb.Append(ch);
                continue;
            }
            if (inQuotes)
            {
                sb.Append(ch);
                continue;
            }
            switch (ch)
            {
                case '{':
                case '[':
                    sb.Append(ch);
                    sb.Append('\n');
                    indent++;
                    sb.Append(new string(' ', indent * 2));
                    break;
                case '}':
                case ']':
                    sb.Append('\n');
                    indent = Math.Max(0, indent - 1);
                    sb.Append(new string(' ', indent * 2));
                    sb.Append(ch);
                    break;
                case ',':
                    sb.Append(ch);
                    sb.Append('\n');
                    sb.Append(new string(' ', indent * 2));
                    break;
                case ':':
                    sb.Append(": ");
                    break;
                default:
                    if (!char.IsWhiteSpace(ch)) sb.Append(ch);
                    break;
            }
        }
        return sb.ToString();
    }
}
