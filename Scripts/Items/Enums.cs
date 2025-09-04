using System;

namespace Xianxia.Items
{
    [Serializable]
    public enum ItemCategory
    {
        weapon, armor, accessory, artifact, consumable, material, manual, currency, quest
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
        none, luyen_khi, truc_co, kim_dan, nguyen_anh, hoa_than, luyen_hu, hop_the, dai_thua, do_kiep
    }

    [Serializable]
    public enum BindType
    {
        none, on_equip, account
    }
}