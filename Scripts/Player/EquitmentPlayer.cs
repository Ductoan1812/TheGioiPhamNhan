using System;
using UnityEngine;
using Xianxia.Items;
using Xianxia.PlayerDataSystem;

public class EquitmentPlayer : MonoBehaviour
{
    public enum DisplaySlot { LeftWeapon, RightWeapon, Helmet, Armor, Shirt, Boots }

    [Serializable]
    public class SlotBinding
    {
        public DisplaySlot slot;
        public SpriteRenderer target;
    }

    [SerializeField] private ItemDatabaseSO database;
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private SlotBinding[] bindings;
    [SerializeField] private bool useResources = false;

    private PlayerData _player;
    private string _playerId = "default";

    private void Awake()
    {
        if (playerStats == null) playerStats = FindObjectOfType<PlayerStats>();
        _playerId = playerStats != null && !string.IsNullOrWhiteSpace(playerStats.PlayerId) ? playerStats.PlayerId : "default";
        _player = PlayerData.GetForPlayer(_playerId, autoCreateIfMissing: true);
        if (_player.equipment == null) _player.equipment = new PlayerEquipment();
    }

    private void OnEnable()
    {
        RefreshAll();
    }

    public void RefreshAll()
    {
        foreach (var b in bindings)
        {
            string itemId = GetItemId(_player.equipment, b.slot);
            if (string.IsNullOrWhiteSpace(itemId) || database == null)
            {
                if (b.target) b.target.sprite = null;
                continue;
            }
            var data = database.GetById(itemId);
            string address = GetAddress(data);
            Sprite sprite = LoadSprite(address);
            if (b.target) b.target.sprite = sprite;
        }
    }

    private string GetItemId(PlayerEquipment eq, DisplaySlot slot)
    {
        // Ưu tiên field đúng tên, sau đó alias
        string[] names = slot switch
        {
            DisplaySlot.LeftWeapon => new[] { "LeftWeapon", "Weapon_L", "WeaponLeft", "VuKhiTrai" },
            DisplaySlot.RightWeapon => new[] { "RightWeapon", "Weapon_R", "WeaponRight", "VuKhiPhai" },
            DisplaySlot.Helmet => new[] { "Helmet", "Mu" },
            DisplaySlot.Armor => new[] { "Armor", "AoGiap" },
            DisplaySlot.Shirt => new[] { "Shirt", "AoThuong" },
            DisplaySlot.Boots => new[] { "Boots", "Giay" },
            _ => Array.Empty<string>()
        };
        foreach (var n in names)
        {
            var f = eq.GetType().GetField(n);
            if (f != null)
            {
                var val = f.GetValue(eq);
                if (val is string s && !string.IsNullOrWhiteSpace(s)) return s;
                if (val != null && val.GetType().GetField("itemId") is var fid && fid != null)
                {
                    var id = fid.GetValue(val) as string;
                    if (!string.IsNullOrWhiteSpace(id)) return id;
                }
            }
        }
        return null;
    }

    private string GetAddress(ItemData data)
    {
        if (data == null) return null;
        var t = data.GetType();
        var f = t.GetField("addressTexture");
        if (f != null) return f.GetValue(data) as string;
        f = t.GetField("addressTexture2D");
        if (f != null) return f.GetValue(data) as string;
        f = t.GetField("addressIcon");
        if (f != null) return f.GetValue(data) as string;
        return null;
    }

    private Sprite LoadSprite(string address)
    {
        if (string.IsNullOrWhiteSpace(address)) return null;
        if (useResources)
        {
            var arr = Resources.LoadAll<Sprite>(address);
            return arr != null && arr.Length > 0 ? arr[0] : null;
        }
#if ADDRESSABLES
        // Đơn giản: chỉ load đồng bộ sprite đầu tiên (nếu cần async thì mở rộng sau)
        Debug.LogWarning("EquitmentPlayer: LoadSprite với Addressables cần code async, demo này chỉ Resources.");
        return null;
#else
        return null;
#endif
    }
}