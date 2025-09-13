using UnityEngine;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using Xianxia.Items;

public class ItemCreator : EditorWindow
{
    // Path tới database JSON chính
    private const string ItemsJsonPath = "Assets/Scripts/Items/Items.json";

    // Trạng thái UI
    private Vector2 leftScroll;
    private Vector2 rightScroll;
    private int selectedIndex = -1;
    private bool createMode = false;
    private string search = "";

    // Dữ liệu DB
    private ItemDTOWrapper db = new ItemDTOWrapper { items = Array.Empty<ItemRecordDTO>() };
    private List<ItemRecordDTO> items = new List<ItemRecordDTO>();
    private ItemRecordDTO newItem = new ItemRecordDTO();

    // Cache Addressables (Sprite/Texture2D)
    private class Addr
    {
        public string address;
        public string guid;
        public string path;
        public Type type; // Sprite hoặc Texture2D
    }
    private List<Addr> allAddr = new List<Addr>();
    private string addrSearchIcon = "";
    private string addrSearchTex = "";

    [MenuItem("Tools/Items Manager")]
    public static void ShowWindow()
    {
        GetWindow<ItemCreator>("Items Manager");
    }

    private void OnEnable()
    {
        RefreshAddressables();
        LoadDb();
    }

    private void LoadDb()
    {
        try
        {
            if (File.Exists(ItemsJsonPath))
            {
                var json = File.ReadAllText(ItemsJsonPath);
                db = JsonUtility.FromJson<ItemDTOWrapper>(json) ?? new ItemDTOWrapper();
                if (db.items == null) db.items = Array.Empty<ItemRecordDTO>();
                items = db.items.ToList();
            }
            else
            {
                db = new ItemDTOWrapper { items = Array.Empty<ItemRecordDTO>() };
                items = new List<ItemRecordDTO>();
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Lỗi đọc Items.json: {ex.Message}");
            db = new ItemDTOWrapper { items = Array.Empty<ItemRecordDTO>() };
            items = new List<ItemRecordDTO>();
        }
    }

    private void SaveDb()
    {
        try
        {
            db.items = items.ToArray();
            var dir = Path.GetDirectoryName(ItemsJsonPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var json = JsonUtility.ToJson(db, true);
            File.WriteAllText(ItemsJsonPath, json);
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("Đã lưu", "Items.json đã được lưu.", "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Lỗi", $"Không thể lưu Items.json: {ex.Message}", "OK");
        }
    }

    private void RefreshAddressables()
    {
        allAddr.Clear();
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null) return;
        foreach (var g in settings.groups)
        {
            if (g == null) continue;
            foreach (var e in g.entries)
            {
                if (e == null) continue;
                var path = AssetDatabase.GUIDToAssetPath(e.guid);
                if (string.IsNullOrEmpty(path)) continue;

                // Thử phân loại Sprite/Texture2D
                Type type = null;
                if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Texture2D))
                {
                    // Có thể là Sprite (Sprite Mode = Single) hoặc Texture2D
                    var sp = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                    type = sp != null ? typeof(Sprite) : typeof(Texture2D);
                }
                else if (AssetDatabase.GetMainAssetTypeAtPath(path) == typeof(Sprite))
                {
                    type = typeof(Sprite);
                }
                else
                {
                    // Bỏ qua các loại khác
                    continue;
                }

                allAddr.Add(new Addr { address = e.address, guid = e.guid, path = path, type = type });
            }
        }
        // Unique theo address
        allAddr = allAddr
            .GroupBy(a => a.address)
            .Select(g => g.First())
            .OrderBy(a => a.address, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void OnGUI()
    {
        DrawToolbar();
        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        DrawListPanel();
        DrawDetailPanel();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
        if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(70)))
        {
            LoadDb();
        }
        if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(60)))
        {
            SaveDb();
        }
    GUILayout.FlexibleSpace();
    search = GUILayout.TextField(search, GUILayout.MinWidth(180));
        if (GUILayout.Button("x", EditorStyles.toolbarButton, GUILayout.Width(20))) search = "";
        if (GUILayout.Button("Refresh Addressables", EditorStyles.toolbarButton, GUILayout.Width(150)))
        {
            RefreshAddressables();
        }
        EditorGUILayout.EndHorizontal();
    }

    private void DrawListPanel()
    {
        EditorGUILayout.BeginVertical(GUILayout.Width(280));

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ New", GUILayout.Height(24)))
        {
            createMode = true;
            selectedIndex = -1;
            newItem = CreateDefaultNewItem();
        }
        GUI.enabled = selectedIndex >= 0 && selectedIndex < items.Count;
        if (GUILayout.Button("Duplicate", GUILayout.Height(24)))
        {
            var src = items[selectedIndex];
            var copy = CloneDTO(src);
            copy.id = MakeNextNumericId(src.id);
            items.Add(copy);
        }
        if (GUILayout.Button("Delete", GUILayout.Height(24)))
        {
            if (EditorUtility.DisplayDialog("Xóa Item", "Bạn có chắc muốn xóa item này?", "Xóa", "Hủy"))
            {
                items.RemoveAt(selectedIndex);
                selectedIndex = -1;
            }
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        leftScroll = EditorGUILayout.BeginScrollView(leftScroll);
        var filtered = string.IsNullOrEmpty(search)
            ? items
            : items.Where(i => (i.id ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || (i.name ?? "").IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0).ToList();

        for (int i = 0; i < filtered.Count; i++)
        {
            var it = filtered[i];
            var label = string.IsNullOrEmpty(it.name) ? it.id : $"{it.name} ({it.id})";
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Toggle(selectedIndex >= 0 && items[selectedIndex] == it, label, "Button"))
            {
                selectedIndex = items.IndexOf(it);
                createMode = false;
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawDetailPanel()
    {
        EditorGUILayout.BeginVertical();
        rightScroll = EditorGUILayout.BeginScrollView(rightScroll);

        if (createMode)
        {
            EditorGUILayout.HelpBox("Chế độ tạo mới: Danh sách Sprite/Texture chỉ hiển thị những Addressable chưa được dùng làm item.", MessageType.Info);
            DrawItemEditor(newItem, isNew: true);
            EditorGUILayout.Space();
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Thêm vào danh sách", GUILayout.Height(28)))
            {
                var err = ValidateDTO(newItem, out var mapped);
                if (err != null)
                {
                    EditorUtility.DisplayDialog("Lỗi dữ liệu", err, "OK");
                }
                else if (items.Any(x => string.Equals(x.id, newItem.id, StringComparison.OrdinalIgnoreCase)))
                {
                    EditorUtility.DisplayDialog("Trùng ID", "ID đã tồn tại trong Items.json", "OK");
                }
                else
                {
                    items.Add(CloneDTO(newItem));
                    createMode = false;
                    selectedIndex = items.Count - 1;
                }
            }
            if (GUILayout.Button("Hủy", GUILayout.Height(28)))
            {
                createMode = false;
            }
            EditorGUILayout.EndHorizontal();
        }
        else if (selectedIndex >= 0 && selectedIndex < items.Count)
        {
            var dto = items[selectedIndex];
            DrawItemEditor(dto, isNew: false);

            // Hiển thị trạng thái mapping
            var err = ValidateDTO(dto, out var mapped);
            if (err == null)
            {
                EditorGUILayout.HelpBox("Hợp lệ theo ItemData.cs", MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox($"Lỗi map: {err}", MessageType.Warning);
            }
        }
        else
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Chọn một item hoặc tạo mới.", EditorStyles.centeredGreyMiniLabel);
            GUILayout.FlexibleSpace();
        }

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private ItemRecordDTO CreateDefaultNewItem()
    {
        return new ItemRecordDTO
        {
            id = MakeUniqueId("new_item"),
            name = "",
            category = ItemCategory.weapon.ToString(),
            rarity = Rarity.pham.ToString(),
            element = Element.none.ToString(),
            realmRequirement = Realm.PhamNhan.ToString(),
            level = 1,
            maxStack = 1,
            baseStats = new BaseStats { res = new Resist() },
            sockets = 0,
            affixes = Array.Empty<AffixEntry>(),
            useEffect = null,
            flavor = "",
            addressIcon = "",
            addressTexture = "",
        };
    }

    private string MakeUniqueId(string baseId)
    {
        string id = baseId;
        int i = 1;
        var set = new HashSet<string>(items.Select(x => x.id), StringComparer.OrdinalIgnoreCase);
        while (set.Contains(id))
        {
            id = baseId + "_" + i++;
        }
        return id;
    }

    // Sinh ID mới bằng cách tăng hậu tố số. Ví dụ:
    // Body, Body2, Body122 => duplicate Body -> Body123
    private string MakeNextNumericId(string originalId)
    {
        if (string.IsNullOrEmpty(originalId)) return MakeUniqueId("new_item");

        // Tách phần chữ (base) và phần số (suffix) ở cuối
        string basePart = originalId;
        int suffixStart = originalId.Length;
        for (int i = originalId.Length - 1; i >= 0; i--)
        {
            if (!char.IsDigit(originalId[i])) { suffixStart = i + 1; break; }
            if (i == 0) suffixStart = 0; // toàn số
        }
        string numberPart = suffixStart < originalId.Length ? originalId.Substring(suffixStart) : string.Empty;
        if (!int.TryParse(numberPart, out var baseNumber)) baseNumber = 0;
        basePart = numberPart.Length > 0 ? originalId.Substring(0, suffixStart) : originalId;

        // Quét tất cả ID có cùng basePart và hậu tố số, lấy max
        int maxNum = baseNumber;
        foreach (var id in items.Select(x => x.id))
        {
            if (id == null) continue;
            if (!id.StartsWith(basePart, StringComparison.Ordinal)) continue;
            var rest = id.Substring(basePart.Length);
            if (rest.Length == 0) { maxNum = Math.Max(maxNum, 0); continue; }
            if (int.TryParse(rest, out var n)) maxNum = Math.Max(maxNum, n);
        }

        int next = maxNum + 1;
        return basePart + next.ToString();
    }

    private ItemRecordDTO CloneDTO(ItemRecordDTO dto)
    {
        // Deep-ish clone bằng JsonUtility
        var json = JsonUtility.ToJson(dto);
        return JsonUtility.FromJson<ItemRecordDTO>(json);
    }

    private string ValidateDTO(ItemRecordDTO dto, out ItemData mapped)
    {
        mapped = null;
        if (dto == null) return "DTO null";
        if (string.IsNullOrWhiteSpace(dto.id)) return "Thiếu id";
        if (string.IsNullOrWhiteSpace(dto.name)) return "Thiếu tên";

        if (!Enum.TryParse(dto.category, true, out ItemCategory _)) return $"Category không hợp lệ: {dto.category}";
        if (!Enum.TryParse(dto.rarity, true, out Rarity _)) return $"Rarity không hợp lệ: {dto.rarity}";
        // Map qua model runtime để chắc chắn đúng
        if (!ItemDTOMapper.TryMap(dto, out mapped, out var err)) return err;
        return null;
    }

    private static T ParseEnum<T>(string s, T def) where T : struct, Enum
    {
        return Enum.TryParse<T>(s, true, out var v) ? v : def;
    }

    private void DrawItemEditor(ItemRecordDTO dto, bool isNew)
    {
        EditorGUILayout.LabelField(isNew ? "Tạo Item mới" : "Chỉnh sửa Item", EditorStyles.boldLabel);
        dto.id = EditorGUILayout.TextField("ID", dto.id);
        dto.name = EditorGUILayout.TextField("Tên", dto.name);

        var cat = ParseEnum(dto.category, ItemCategory.weapon);
        cat = (ItemCategory)EditorGUILayout.EnumPopup("Loại", cat);
        dto.category = cat.ToString();

        var rar = ParseEnum(dto.rarity, Rarity.pham);
        rar = (Rarity)EditorGUILayout.EnumPopup("Phẩm chất", rar);
        dto.rarity = rar.ToString();

        var elem = ParseEnum(dto.element, Element.none);
        elem = (Element)EditorGUILayout.EnumPopup("Ngũ hành", elem);
        dto.element = elem.ToString();

        var realm = ParseEnum(dto.realmRequirement, Realm.PhamNhan);
        realm = (Realm)EditorGUILayout.EnumPopup("Cảnh giới yêu cầu", realm);
        dto.realmRequirement = realm.ToString();


    dto.level = EditorGUILayout.IntField("Cấp", dto.level);
    dto.maxStack = Mathf.Max(1, EditorGUILayout.IntField("Stack tối đa", dto.maxStack));

        EditorGUILayout.Space();
        DrawBaseStats(dto);

        dto.sockets = Mathf.Max(0, EditorGUILayout.IntField("Số socket", dto.sockets));

        EditorGUILayout.Space();
        DrawAffixes(dto);

        EditorGUILayout.Space();
        DrawUseEffect(dto);

        EditorGUILayout.Space();
        dto.flavor = EditorGUILayout.TextField("Mô tả", dto.flavor);

        EditorGUILayout.Space();
        DrawAddressSelectors(dto, isNew);
    }

    private void DrawBaseStats(ItemRecordDTO dto)
    {
        dto.baseStats ??= new BaseStats { res = new Resist() };
        var bs = dto.baseStats;
        EditorGUILayout.LabelField("Chỉ số gốc", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        bs.atk = EditorGUILayout.FloatField("ATK", bs.atk);
        bs.defense = EditorGUILayout.FloatField("DEF", bs.defense);
        bs.hp = EditorGUILayout.FloatField("HP", bs.hp);
        bs.qi = EditorGUILayout.FloatField("Qi", bs.qi);
        bs.moveSpd = EditorGUILayout.FloatField("Move Spd", bs.moveSpd);
        bs.critRate = EditorGUILayout.FloatField("Crit Rate", bs.critRate);
        bs.critDmg = EditorGUILayout.FloatField("Crit Dmg", bs.critDmg);
        bs.penetration = EditorGUILayout.FloatField("Penetration", bs.penetration);
        bs.lifestealQi = EditorGUILayout.FloatField("Lifesteal Qi", bs.lifestealQi);

        bs.res ??= new Resist();
        EditorGUILayout.LabelField("Kháng (res)");
        EditorGUI.indentLevel++;
        bs.res.kim = EditorGUILayout.FloatField("kim", bs.res.kim);
        bs.res.moc = EditorGUILayout.FloatField("moc", bs.res.moc);
        bs.res.thuy = EditorGUILayout.FloatField("thuy", bs.res.thuy);
        bs.res.hoa = EditorGUILayout.FloatField("hoa", bs.res.hoa);
        bs.res.tho = EditorGUILayout.FloatField("tho", bs.res.tho);
        bs.res.loi = EditorGUILayout.FloatField("loi", bs.res.loi);
        bs.res.am = EditorGUILayout.FloatField("am", bs.res.am);
        bs.res.duong = EditorGUILayout.FloatField("duong", bs.res.duong);
        EditorGUI.indentLevel--;
        EditorGUI.indentLevel--;
    }

    private void DrawAffixes(ItemRecordDTO dto)
    {
        EditorGUILayout.LabelField("Affixes", EditorStyles.boldLabel);
        if (dto.affixes == null) dto.affixes = Array.Empty<AffixEntry>();
        var list = dto.affixes.ToList();
        int removeAt = -1;
        for (int i = 0; i < list.Count; i++)
        {
            var a = list[i] ?? new AffixEntry();
            EditorGUILayout.BeginVertical("box");
            a.id = EditorGUILayout.TextField("ID", a.id);
            a.value = EditorGUILayout.FloatField("Giá trị", a.value);
            a.tier = Mathf.Max(1, EditorGUILayout.IntField("Tier", a.tier));
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Xóa", GUILayout.Width(60))) removeAt = i;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            list[i] = a;
        }
        if (removeAt >= 0)
        {
            list.RemoveAt(removeAt);
        }
        if (GUILayout.Button("+ Thêm affix", GUILayout.Width(120)))
        {
            list.Add(new AffixEntry { id = "", value = 0, tier = 1 });
        }
        dto.affixes = list.ToArray();
    }

    private void DrawUseEffect(ItemRecordDTO dto)
    {
        EditorGUILayout.LabelField("Use Effect", EditorStyles.boldLabel);
        bool has = dto.useEffect != null;
        bool newHas = EditorGUILayout.Toggle("Có hiệu ứng?", has);
        if (newHas && dto.useEffect == null) dto.useEffect = new UseEffect();
        if (!newHas) { dto.useEffect = null; return; }
        var u = dto.useEffect;
        u.type = EditorGUILayout.TextField("Type", u.type);
        u.magnitude = EditorGUILayout.FloatField("Magnitude", u.magnitude);
        u.duration = EditorGUILayout.FloatField("Duration", u.duration);
        u.spellId = EditorGUILayout.TextField("Spell Id", u.spellId);
    }

    private void DrawAddressSelectors(ItemRecordDTO dto, bool isNew)
    {
        EditorGUILayout.LabelField("Địa chỉ Addressables", EditorStyles.boldLabel);

        var used = new HashSet<string>(items
            .SelectMany(i => new[] { i.addressIcon, i.addressTexture })
            .Where(a => !string.IsNullOrEmpty(a))
        );

        // Icon selector
        EditorGUILayout.LabelField("Icon (Sprite/Texture2D)", EditorStyles.miniBoldLabel);
        addrSearchIcon = EditorGUILayout.TextField("Tìm kiếm", addrSearchIcon);
        var iconOptions = FilterAddr(allAddr, addrSearchIcon, isNew ? used : null, current: dto.addressIcon);
        int iconIndex = IndexOfAddress(iconOptions, dto.addressIcon);
    var iconLabels = iconOptions.Select(a => string.IsNullOrEmpty(a.address) ? "(none)" : a.address).ToArray();
    int newIconIndex = EditorGUILayout.Popup("Chọn address", Mathf.Max(0, iconIndex), iconLabels);
        if (newIconIndex >= 0 && newIconIndex < iconOptions.Count)
            dto.addressIcon = iconOptions[newIconIndex].address;
        DrawAddressPreview(dto.addressIcon);

        EditorGUILayout.Space();
        // Texture selector
        EditorGUILayout.LabelField("Texture (Sprite/Texture2D)", EditorStyles.miniBoldLabel);
        addrSearchTex = EditorGUILayout.TextField("Tìm kiếm", addrSearchTex);
        var texOptions = FilterAddr(allAddr, addrSearchTex, isNew ? used : null, current: dto.addressTexture);
        int texIndex = IndexOfAddress(texOptions, dto.addressTexture);
    var texLabels = texOptions.Select(a => string.IsNullOrEmpty(a.address) ? "(none)" : a.address).ToArray();
    int newTexIndex = EditorGUILayout.Popup("Chọn address", Mathf.Max(0, texIndex), texLabels);
        if (newTexIndex >= 0 && newTexIndex < texOptions.Count)
            dto.addressTexture = texOptions[newTexIndex].address;
        DrawAddressPreview(dto.addressTexture);
    }

    private List<Addr> FilterAddr(List<Addr> src, string search, HashSet<string> usedIfNew, string current)
    {
        IEnumerable<Addr> q = src;
        if (!string.IsNullOrEmpty(search))
        {
            q = q.Where(a => a.address.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0 || a.path.IndexOf(search, StringComparison.OrdinalIgnoreCase) >= 0);
        }
        if (usedIfNew != null)
        {
            q = q.Where(a => a.address == current || !usedIfNew.Contains(a.address));
        }
        var list = q.ToList();
        // Thêm option rỗng ở đầu để có thể xóa lựa chọn
        if (!list.Any(a => string.IsNullOrEmpty(a.address)))
        {
            list.Insert(0, new Addr { address = string.Empty, path = string.Empty, guid = string.Empty, type = typeof(UnityEngine.Object) });
        }
        return list;
    }

    private int IndexOfAddress(List<Addr> list, string address)
    {
        if (string.IsNullOrEmpty(address)) return 0;
        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].address == address) return i;
        }
        return 0;
    }

    private void DrawAddressPreview(string address)
    {
        if (string.IsNullOrEmpty(address)) return;
        var a = allAddr.FirstOrDefault(x => x.address == address);
        if (a == null) return;
        var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(a.path);
        if (obj != null)
        {
            var preview = AssetPreview.GetAssetPreview(obj) ?? AssetPreview.GetMiniThumbnail(obj);
            if (preview != null)
            {
                var rect = GUILayoutUtility.GetRect(64, 64, GUILayout.MaxWidth(64), GUILayout.MaxHeight(64));
                EditorGUI.DrawPreviewTexture(rect, preview, null, ScaleMode.ScaleToFit);
            }
        }
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"Address: {a.address}");
        if (GUILayout.Button("Ping", GUILayout.Width(60)))
        {
            var obj2 = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(a.path);
            if (obj2 != null) EditorGUIUtility.PingObject(obj2);
        }
        EditorGUILayout.EndHorizontal();
    }
}