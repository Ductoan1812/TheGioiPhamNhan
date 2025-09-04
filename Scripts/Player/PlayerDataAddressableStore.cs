using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Xianxia.PlayerDataSystem;

namespace Xianxia.Persistence
{
    public class PlayerDataAddressableStore
    {
        public string address;
        public string persistentFileName = "PlayerData.json";
        public bool writeBackSeedToPersistent = true;
        public bool prettyPrint = true;

        public PlayerDataAddressableStore(string address, string persistentFileName = "PlayerData.json")
        {
            this.address = address;
            this.persistentFileName = persistentFileName;
        }

        public static string GetPersistentPath(string fileName)
        {
            return Path.Combine(Application.persistentDataPath, fileName);
        }

        public async Task<PlayerData> LoadAsync()
        {
            string path = GetPersistentPath(persistentFileName);

            if (File.Exists(path))
            {
                return LoadFromFile(path);
            }

            if (string.IsNullOrWhiteSpace(address))
            {
                Debug.LogWarning("PlayerDataStore: address rỗng, trả về PlayerData rỗng.");
                return NewEmpty();
            }

            TextAsset jsonAsset = null;
            try
            {
                var handle = Addressables.LoadAssetAsync<TextAsset>(address);
                jsonAsset = await handle.Task;
                Addressables.Release(handle);
            }
            catch (Exception e)
            {
                Debug.LogError($"PlayerDataStore: Lỗi load seed từ Addressables '{address}': {e.Message}");
                return NewEmpty();
            }

            if (jsonAsset == null || string.IsNullOrEmpty(jsonAsset.text))
            {
                Debug.LogWarning($"PlayerDataStore: Seed '{address}' null/empty. Trả về rỗng.");
                return NewEmpty();
            }

            var data = Parse(jsonAsset.text);
            EnsureDefaults(data);

            if (writeBackSeedToPersistent)
            {
                SaveToFile(path, data);
                Debug.Log($"PlayerDataStore: Seed đã ghi về persistent: {path}");
            }

            return data;
        }

        public bool Save(PlayerData data)
        {
            if (data == null) { Debug.LogError("PlayerDataStore: Save null data."); return false; }
            string path = GetPersistentPath(persistentFileName);
            EnsureDefaults(data);
            return SaveToFile(path, data);
        }

        // Helpers
        private static PlayerData LoadFromFile(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                if (string.IsNullOrWhiteSpace(json))
                {
                    Debug.LogWarning($"PlayerDataStore: File rỗng: {path}, tạo mới.");
                    return NewEmpty();
                }
                var data = Parse(json);
                EnsureDefaults(data);
                return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"PlayerDataStore: Lỗi đọc file '{path}': {e.Message}");
                return NewEmpty();
            }
        }

        private bool SaveToFile(string path, PlayerData data)
        {
            try
            {
                var dir = Path.GetDirectoryName(path);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonUtility.ToJson(data, prettyPrint);
                File.WriteAllText(path, json);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"PlayerDataStore: Lỗi ghi file '{path}': {e.Message}");
                return false;
            }
        }

        private static PlayerData Parse(string json)
        {
            try
            {
                var data = JsonUtility.FromJson<PlayerData>(json);
                return data ?? NewEmpty();
            }
            catch (Exception e)
            {
                Debug.LogError($"PlayerDataStore: Parse JSON lỗi: {e.Message}");
                return NewEmpty();
            }
        }

        private static void EnsureDefaults(PlayerData d)
        {
            if (d == null) return;
            d.inventory ??= new List<PlayerInventoryItem>();
            if (d.InventorySize <= 0) d.InventorySize = 30;
        }

        private static PlayerData NewEmpty()
        {
            return new PlayerData
            {
                InventorySize = 30,
                inventory = new List<PlayerInventoryItem>()
            };
        }
    }
}