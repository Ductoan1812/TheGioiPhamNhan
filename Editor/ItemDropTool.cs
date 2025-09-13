using UnityEditor;
using UnityEngine;
using Xianxia.Items;
using Xianxia.PlayerDataSystem;

/// Cửa sổ Editor dùng khi đang Play: chọn ID từ ItemDatabaseSO và spawn vật phẩm tại vị trí chỉ định.
public class ItemDropTool : EditorWindow
{
    private ItemDropManager dropManager;
    private ItemDatabaseSO itemDB;
    private int selectedIndex = 0;
    private string[] idOptions = new string[0];

    private int quantity = 1;
    private Vector3 spawnPosition = Vector3.zero;
    private bool spawnNow = false; // nút bool: check để spawn, rồi tự reset

    [MenuItem("Xianxia/Item Drop Tool")]
    public static void Open()
    {
        GetWindow<ItemDropTool>(false, "Item Drop Tool").Show();
    }

    private void OnEnable()
    {
        // Tự tìm manager nếu có trong scene
    if (dropManager == null)
    {
#if UNITY_2023_1_OR_NEWER
        dropManager = Object.FindFirstObjectByType<ItemDropManager>();
#else
        dropManager = Object.FindObjectOfType<ItemDropManager>();
#endif
    }

        // Ưu tiên ItemDatabaseSO.Instance; nếu chưa có, cho phép gán tay
        if (itemDB == null)
            itemDB = ItemDatabaseSO.Instance;

        RefreshIdList();
    }

    private void RefreshIdList()
    {
        if (itemDB != null && itemDB.Items != null)
        {
            var items = itemDB.Items;
            idOptions = new string[items.Count];
            for (int i = 0; i < items.Count; i++)
            {
                idOptions[i] = items[i]?.id ?? string.Empty;
            }
            if (selectedIndex < 0 || selectedIndex >= idOptions.Length) selectedIndex = 0;
        }
        else
        {
            idOptions = new string[0];
            selectedIndex = 0;
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Tool spawn vật phẩm (Play Mode)", EditorStyles.boldLabel);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Hãy chuyển sang Play Mode để sử dụng tool này.", MessageType.Info);
        }

        using (new EditorGUI.DisabledScope(!Application.isPlaying))
        {
            // Tham chiếu
            dropManager = (ItemDropManager)EditorGUILayout.ObjectField("ItemDropManager", dropManager, typeof(ItemDropManager), true);
            itemDB = (ItemDatabaseSO)EditorGUILayout.ObjectField("ItemDatabaseSO", itemDB, typeof(ItemDatabaseSO), false);
            if (GUILayout.Button("Tải DS ID từ DB"))
            {
                if (itemDB == null) itemDB = ItemDatabaseSO.Instance;
                RefreshIdList();
            }

            // Chọn ID từ danh sách (chỉ hiển thị ID)
            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField("Chọn ID vật phẩm");
            if (idOptions.Length == 0)
            {
                EditorGUILayout.HelpBox("Chưa có danh sách ID. Hãy gán ItemDatabaseSO và bấm 'Tải DS ID từ DB'.", MessageType.Warning);
            }
            else
            {
                selectedIndex = EditorGUILayout.Popup("ID", selectedIndex, idOptions);
            }

            quantity = Mathf.Max(1, EditorGUILayout.IntField("Số lượng", quantity));
            spawnPosition = EditorGUILayout.Vector3Field("Vị trí spawn", spawnPosition);
            if (GUILayout.Button("Lấy vị trí từ Object đang chọn"))
            {
                if (Selection.activeTransform != null)
                    spawnPosition = Selection.activeTransform.position;
            }

            // Nút bool: khi check -> spawn rồi auto reset
            bool newSpawn = EditorGUILayout.ToggleLeft("Spawn ngay tại vị trí", spawnNow);
            if (newSpawn && !spawnNow)
            {
                DoSpawn();
                newSpawn = false; // auto reset
            }
            spawnNow = newSpawn;
        }
    }

    private void DoSpawn()
    {
        if (dropManager == null)
        {
            Debug.LogWarning("[ItemDropTool] Chưa gán ItemDropManager trong scene.");
            return;
        }
        if (idOptions.Length == 0 || selectedIndex < 0 || selectedIndex >= idOptions.Length)
        {
            Debug.LogWarning("[ItemDropTool] Chưa chọn ID vật phẩm.");
            return;
        }

        string id = idOptions[selectedIndex];
        if (string.IsNullOrEmpty(id))
        {
            Debug.LogWarning("[ItemDropTool] ID rỗng.");
            return;
        }

        // Tạo InventoryItem từ ID (sao chép dữ liệu gốc nếu có, nhưng UI chỉ cần ID)
        InventoryItem inv = CreateInventoryItem(id, quantity);
        if (inv == null)
        {
            Debug.LogWarning($"[ItemDropTool] Không tạo được InventoryItem cho id={id}");
            return;
        }

        var spawned = dropManager.Spawn(inv, spawnPosition);
        if (spawned != null)
        {
            Debug.Log($"[ItemDropTool] Spawn '{id}' x{quantity} tại {spawnPosition}");
        }
    }

    private InventoryItem CreateInventoryItem(string id, int qty)
    {
        var inv = new InventoryItem();
        inv.id = id;
        inv.quantity = Mathf.Max(1, qty);

        // Nếu có DB, copy thuộc tính để tương thích hệ thống stack, hiển thị...
        var db = itemDB != null ? itemDB : ItemDatabaseSO.Instance;
        var def = db != null ? db.GetById(id) : null;
        if (def != null)
        {
            inv.name = def.name;
            inv.category = def.category;
            inv.rarity = def.rarity;
            inv.element = def.element;
            inv.realmRequirement = def.realmRequirement;
            inv.level = def.level;
            inv.maxStack = def.maxStack;
            inv.baseStats = def.baseStats;
            inv.sockets = def.sockets;
            inv.affixes = def.affixes;
            inv.useEffect = def.useEffect;
            inv.flavor = def.flavor;
            inv.addressIcon = def.addressIcon;
            inv.addressTexture = def.addressTexture;
        }
        return inv;
    }
}
