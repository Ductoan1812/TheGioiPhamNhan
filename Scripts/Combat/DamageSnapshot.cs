using UnityEngine;

public struct DamageSnapshot
{
    public GameObject attacker;
    public int totalDamage;
    public float lastHitTime;
    public float ratio; // final normalized ratio after adjustments
}
