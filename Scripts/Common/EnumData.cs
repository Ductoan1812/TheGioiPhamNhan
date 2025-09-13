using System;

// File dùng chung chứa các enum core cho toàn bộ dự án.
// Khi cần thêm enum mới (ví dụ StatId, DamageType, etc.) đặt vào đây hoặc tách file cùng thư mục để dễ quản lý.
// Tránh định nghĩa trùng lặp nhiều nơi.

namespace Xianxia.Core
{
   // Nếu muốn gom meta sau này có thể tạo Attribute chung trong namespace này
}

namespace Xianxia.Items
{
    [Serializable]
    public enum ItemCategory
    {
        weapon, armor, cloth, back, foot, helmet, pet, accessory, artifact, consumable, material, manual, currency, quest
    }

    [Serializable]
    public enum Rarity
    {
        pham, hoang, huyen, dia, thien, tien, than
    }

    [Serializable]
    public enum Element
    {
        none, kim, moc, thuy, hoa, tho, loi, am, duong
    }

    [Serializable]
    public enum Realm
    {
        PhamNhan, luyen_khi, luyen_khi_1, luyen_khi_2, luyen_khi_3, luyen_khi_4, luyen_khi_5, luyen_khi_6, luyen_khi_7, luyen_khi_8, luyen_khi_9, truc_co, kim_dan, nguyen_anh, hoa_than, luyen_hu, hop_the, dai_thua, chuan_tien
    }
}
