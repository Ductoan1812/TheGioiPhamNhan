using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Xianxia.Items
{
    // Quản lý load JSON (Addressables) và populate vào ItemDatabaseSO
    public class ItemManager : MonoBehaviour
    {
        [Header("Addressables")]
        [Tooltip("Địa chỉ (address) của TextAsset JSON trong Addressables")]
        public string jsonAddress = "items_database_json";

        [Header("Target Database")]
        public ItemDatabaseSO targetDatabase;

        [Header("Auto Load On Awake")]
        public bool loadOnAwake = true;

        public bool IsLoaded { get; private set; }

        private async void Awake()
        {
            if (targetDatabase != null)
                targetDatabase.SetAsInstance();

            if (loadOnAwake)
            {
                await LoadAllAsync();
            }
        }

        public async Task<bool> LoadAllAsync()
        {
            if (string.IsNullOrEmpty(jsonAddress))
            {
                Debug.LogError("ItemManager: jsonAddress is empty.");
                return false;
            }

            TextAsset jsonAsset = null;
            try
            {
                var handle = Addressables.LoadAssetAsync<TextAsset>(jsonAddress);
                jsonAsset = await handle.Task;
                Addressables.Release(handle);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ItemManager: Failed to load JSON TextAsset at '{jsonAddress}': {e}");
                return false;
            }

            if (jsonAsset == null || string.IsNullOrEmpty(jsonAsset.text))
            {
                Debug.LogError("ItemManager: JSON TextAsset is null or empty.");
                return false;
            }

            ItemDTOWrapper wrapper = null;
            try
            {
                wrapper = JsonUtility.FromJson<ItemDTOWrapper>(jsonAsset.text);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"ItemManager: JSON parse error: {e}");
                return false;
            }

            if (wrapper?.items == null)
            {
                Debug.LogWarning("ItemManager: No items in JSON.");
                wrapper = new ItemDTOWrapper { items = new ItemRecordDTO[0] };
            }

            var list = new List<ItemData>(wrapper.items.Length);
            var ids = new HashSet<string>();
            int ok = 0, fail = 0;

            foreach (var dto in wrapper.items)
            {
                if (!ItemDTOMapper.TryMap(dto, out var model, out var err))
                {
                    Debug.LogError($"ItemManager: map failed for id '{dto?.id}': {err}");
                    fail++;
                    continue;
                }

                if (!ids.Add(model.id))
                {
                    Debug.LogError($"ItemManager: duplicate id '{model.id}'");
                    fail++;
                    continue;
                }

                list.Add(model);
                ok++;
            }

            if (targetDatabase == null)
            {
                targetDatabase = ScriptableObject.CreateInstance<ItemDatabaseSO>();
                targetDatabase.SetAsInstance();
            }

            targetDatabase.ReplaceAll(list);
            IsLoaded = true;

            Debug.Log($"ItemManager: Loaded {ok} items, {fail} failed.");
            return true;
        }
    }
}