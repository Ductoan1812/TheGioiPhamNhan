using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Linq;

namespace Xianxia.Items
{
    public static class ItemAssets
    {
        private static readonly Dictionary<string, (AsyncOperationHandle<Sprite> handle, Sprite asset)> _icons
            = new Dictionary<string, (AsyncOperationHandle<Sprite>, Sprite)>();
        private static readonly Dictionary<string, (AsyncOperationHandle<Texture2D> handle, Texture2D asset)> _textures
            = new Dictionary<string, (AsyncOperationHandle<Texture2D>, Texture2D)>();
        private static readonly Dictionary<string, (AsyncOperationHandle<IList<Sprite>> handle, Sprite[] assets)> _spriteLists
            = new Dictionary<string, (AsyncOperationHandle<IList<Sprite>>, Sprite[])>();


        public static async System.Threading.Tasks.Task<Sprite[]> LoadAllSpritesAsync(string address)
        {
            if (string.IsNullOrEmpty(address)) return System.Array.Empty<Sprite>();
            if (_spriteLists.TryGetValue(address, out var cached) && cached.assets != null && cached.assets.Length > 0)
                return cached.assets;

            var handle = Addressables.LoadAssetsAsync<Sprite>(address, null);
            var list = await handle.Task;
            var arr = list != null ? list.ToArray() : System.Array.Empty<Sprite>();
            _spriteLists[address] = (handle, arr);
            return arr;
        }
        public static async Task<Sprite> LoadIconSpriteAsync(string address)
        {
            if (string.IsNullOrEmpty(address)) return null;
            if (_icons.TryGetValue(address, out var cached) && cached.asset != null) return cached.asset;

            var handle = Addressables.LoadAssetAsync<Sprite>(address);
            var sprite = await handle.Task;
            _icons[address] = (handle, sprite);
            return sprite;
        }

        public static async Task<Texture2D> LoadTextureAsync(string address)
        {
            if (string.IsNullOrEmpty(address)) return null;
            if (_textures.TryGetValue(address, out var cached) && cached.asset != null) return cached.asset;

            var handle = Addressables.LoadAssetAsync<Texture2D>(address);
            var tex = await handle.Task;
            _textures[address] = (handle, tex);
            return tex;
        }

        public static void ReleaseIcon(string address)
        {
            if (string.IsNullOrEmpty(address)) return;
            if (_icons.TryGetValue(address, out var c))
            {
                Addressables.Release(c.handle);
                _icons.Remove(address);
            }
        }

        public static void ReleaseTexture(string address)
        {
            if (string.IsNullOrEmpty(address)) return;
            if (_textures.TryGetValue(address, out var c))
            {
                Addressables.Release(c.handle);
                _textures.Remove(address);
            }
        }

        public static void ReleaseAll()
        {
            foreach (var kv in _icons) Addressables.Release(kv.Value.handle);
            foreach (var kv in _textures) Addressables.Release(kv.Value.handle);
            foreach (var kv in _spriteLists) Addressables.Release(kv.Value.handle);
            _icons.Clear();
            _textures.Clear();
            _spriteLists.Clear();
        }

        public static void ReleaseSprites(string address)
        {
            if (string.IsNullOrEmpty(address)) return;
            if (_spriteLists.TryGetValue(address, out var c))
            {
                Addressables.Release(c.handle);
                _spriteLists.Remove(address);
            }
        }
    }
}