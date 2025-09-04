using System.Collections.Generic;
using UnityEngine;

namespace Xianxia.Items
{
    [CreateAssetMenu(fileName = "ItemDatabaseSO", menuName = "Xianxia/Item Database SO")]
    public class ItemDatabaseSO : ScriptableObject
    {
        [SerializeField] private List<ItemData> items = new List<ItemData>();
        private Dictionary<string, ItemData> byId = new Dictionary<string, ItemData>();

        private static ItemDatabaseSO _instance;
        public static ItemDatabaseSO Instance => _instance;

        public void SetAsInstance()
        {
            _instance = this;
            BuildIndex();
        }

        public IReadOnlyList<ItemData> Items => items;

        public void ReplaceAll(IEnumerable<ItemData> newItems)
        {
            items.Clear();
            if (newItems != null) items.AddRange(newItems);
            BuildIndex();
        }

        public void BuildIndex()
        {
            byId.Clear();
            foreach (var it in items)
            {
                if (it != null && !string.IsNullOrEmpty(it.id))
                {
                    byId[it.id] = it;
                }
            }
        }

        public ItemData GetById(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            return byId.TryGetValue(id, out var d) ? d : null;
        }
    }
}