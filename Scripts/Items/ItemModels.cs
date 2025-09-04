using System;

namespace Xianxia.Items
{
    [Serializable]
    public class Resist
    {
        public float kim, moc, thuy, hoa, tho, loi, am, duong;
    }

    [Serializable]
    public class BaseStats
    {
        public float atk;
        public float defense;
        public float hp;
        public float qi;
        public float moveSpd;
        public float critRate;
        public float critDmg;
        public float penetration;
        public float lifestealQi;
        public Resist res = new Resist();
    }

    [Serializable]
    public class AffixEntry
    {
        public string id;
        public float value;
        public int tier = 1;
    }

    [Serializable]
    public class UseEffect
    {
        public string type;   // heal_hp, restore_qi, buff, cast_spell, unlock_manual
        public float magnitude;
        public float duration;
        public string spellId;
    }
}