using System;
using UnityEngine;

namespace Xianxia.Items
{
    // Model dùng trong runtime/DB (đã parse enum)
    [Serializable]
    public class ItemData
    {
        public string id;
        public string name;
        public ItemCategory category;
        public Rarity rarity;
        public Element element = Element.none;
        public Realm realmRequirement = Realm.PhamNhan;
        public BindType bindType = BindType.none;

        public int level = 1;
        public int maxStack = 1;

        public BaseStats baseStats = new BaseStats();
        public int sockets = 0;
        public AffixEntry[] affixes = Array.Empty<AffixEntry>();
        public UseEffect useEffect;
        public string flavor;

        // Addressables
        public string addressIcon;     // address Sprite
        public string addressTexture;  // address Texture2D

        /// <summary>
        /// Log toàn bộ thông tin chi tiết của item này ra console.
        /// </summary>
        public void LogDetail()
        {
            Debug.Log($"--- Thông tin Item ---\n" +
                $"ID: {id}\n" +
                $"Tên: {name}\n" +
                $"Loại: {category}\n" +
                $"Phẩm chất: {rarity}\n" +
                $"Ngũ hành: {element}\n" +
                $"Yêu cầu cảnh giới: {realmRequirement}\n" +
                $"Ràng buộc: {bindType}\n" +
                $"Cấp: {level}\n" +
                $"Stack tối đa: {maxStack}\n" +
                $"Chỉ số gốc: {baseStats}\n" +
                $"Số socket: {sockets}\n" +
                $"Affix: {AffixArrayToString(affixes)}\n" +
                $"Hiệu ứng dùng: {(useEffect != null ? useEffect.ToString() : "None")}\n" +
                $"Mô tả: {flavor}\n" +
                $"Icon: {addressIcon}\n" +
                $"Texture: {addressTexture}");
        }

        private string AffixArrayToString(AffixEntry[] arr)
        {
            if (arr == null || arr.Length == 0) return "Không có";
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < arr.Length; i++)
            {
                var a = arr[i];
                if (a == null) continue;
                sb.Append($"[{a.id}: {a.value} (tier {a.tier})]");
                if (i < arr.Length - 1) sb.Append(", ");
            }
            return sb.ToString();
        }
    }

    // DTO để parse JSON (enum ở dạng string)
    [Serializable]
    public class ItemRecordDTO
    {
        public string id;
        public string name;
        public string category;
        public string rarity;
        public string element;
        public string realmRequirement;
        public string bindType;

    public int level = 1;
    public int maxStack = 1;

        public BaseStats baseStats = new BaseStats();
        public int sockets = 0;
        public AffixEntry[] affixes = Array.Empty<AffixEntry>();
        public UseEffect useEffect;
        public string flavor;

        public string addressIcon;
        public string addressTexture;
    }

    [Serializable]
    public class ItemDTOWrapper
    {
        public ItemRecordDTO[] items;
    }

    public static class ItemDTOMapper
    {
        public static bool TryMap(ItemRecordDTO dto, out ItemData data, out string error)
        {
            data = null;
            error = null;
            if (dto == null) { error = "DTO is null"; return false; }
            if (string.IsNullOrWhiteSpace(dto.id)) { error = "Missing id"; return false; }

            if (!Enum.TryParse(dto.category, true, out ItemCategory category))
            { error = $"Invalid category: {dto.category}"; return false; }

            if (!Enum.TryParse(dto.rarity, true, out Rarity rarity))
            { error = $"Invalid rarity: {dto.rarity}"; return false; }

            Enum.TryParse(dto.element ?? "none", true, out Element element);
            Enum.TryParse(dto.realmRequirement ?? "none", true, out Realm realm);
            Enum.TryParse(dto.bindType ?? "none", true, out BindType bind);

            data = new ItemData
            {
                id = dto.id,
                name = dto.name,
                category = category,
                rarity = rarity,
                element = element,
                realmRequirement = realm,
                bindType = bind,
                level = dto.level,
                maxStack = Mathf.Max(1, dto.maxStack),
                baseStats = dto.baseStats ?? new BaseStats(),
                sockets = Mathf.Max(0, dto.sockets),
                affixes = dto.affixes ?? Array.Empty<AffixEntry>(),
                useEffect = dto.useEffect,
                flavor = dto.flavor,
                addressIcon = dto.addressIcon,
                addressTexture = dto.addressTexture
            };
            return true;
        }
    }
}