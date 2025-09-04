using UnityEngine;

public struct DamageContext
{
    public GameObject attacker;
    public int amount;
    public bool isCrit;
    public string damageType;
    public float time;

    public DamageContext(GameObject attacker, int amount, bool isCrit = false, string damageType = "physical")
    {
        this.attacker = attacker;
        this.amount = amount;
        this.isCrit = isCrit;
        this.damageType = damageType;
        this.time = Time.time;
    }
}
