using UnityEngine;
using Xianxia.Items;
using Xianxia.PlayerDataSystem;

[DisallowMultipleComponent]
public class ItemSpawner : MonoBehaviour
{
    [System.Serializable]
    public struct SpawnEntry
    {
        [Tooltip("Chọn từ combobox (Editor) — giá trị thực lưu là itemId")]
        public string itemId;

        [Min(1)] public int quantity;
        public Vector3 position;

        // Variant
        [Header("Variant")]
        public int level;
        public string rarity;
        public string element;
        [Range(0, 100)] public int quality;
        [Tooltip("0..1 hoặc tuỳ hệ thống")] public float durability;

        public ItemStat[] baseStats;
        public PlayerInventoryAffix[] affixes;
        public string[] tags;
        public ItemProp[] custom;
    }

    [Header("Database (để combobox hiển thị danh sách id)")]
    [SerializeField] private ItemDatabaseSO database;

    [Header("Prefab có ItemPrefab")]
    public GameObject itemPrefab;

    [Header("Spawn sẵn khi Start (tùy chọn)")]
    public SpawnEntry[] initialSpawns;

    public ItemDatabaseSO DatabaseOrInstance => database != null ? database : ItemDatabaseSO.Instance;

    private void Start()
    {
        if (initialSpawns != null && initialSpawns.Length > 0)
        {
            foreach (var e in initialSpawns)
            {
                Spawn(e);
            }
        }
    }

    public GameObject Spawn(SpawnEntry e)
    {
        return Spawn(
            e.itemId, Mathf.Max(1, e.quantity), e.position,
            e.level, e.rarity, e.element, e.quality, e.durability,
            e.baseStats, e.affixes, e.tags, e.custom
        );
    }

    public GameObject Spawn(
        string itemId, int quantity, Vector3 position,
        int level = 0, string rarity = "", string element = "",
        int quality = 0, float durability = 1f,
        ItemStat[] baseStats = null, PlayerInventoryAffix[] affixes = null,
        string[] tags = null, ItemProp[] custom = null)
    {
        if (itemPrefab == null)
        {
            Debug.LogError("ItemSpawner: itemPrefab chưa gán.");
            return null;
        }

        var go = Instantiate(itemPrefab, position, Quaternion.identity);
        var view = go.GetComponent<ItemPrefab>();
        if (view == null)
        {
            Debug.LogError("ItemSpawner: Prefab không có ItemPrefab component.");
            return go;
        }

        view.Setup(
            itemId, Mathf.Max(1, quantity),
            level, rarity, element,
            Mathf.Clamp(quality, 0, 100), durability,
            baseStats, affixes, tags, custom
        );
        return go;
    }

    // Tiện lợi: Spawn theo ItemData (nếu bạn có tham chiếu trực tiếp)
    public GameObject Spawn(ItemData data, int quantity, Vector3 position)
    {
        if (data == null) { Debug.LogError("ItemSpawner: ItemData null."); return null; }
        return Spawn(data.id, quantity, position);
    }
}