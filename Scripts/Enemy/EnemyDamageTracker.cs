using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(EnemyStats))]
public class EnemyDamageTracker : MonoBehaviour, IDamageable
{
    [Header("EXP Reward")] [SerializeField] private int expReward = 20;

    [Header("Chia EXP")] 
    [SerializeField, Tooltip("Bonus thêm vào người last hit (tỉ lệ cộng trước chuẩn hoá)")] private float lastHitBonusRatio = 0.1f;
    [SerializeField, Tooltip("Tối thiểu % damage để được tính assist (trừ last hit)")] private float minimalAssistRatio = 0.05f;
    [SerializeField, Tooltip("Sau bao lâu không gây damage thì bị loại khỏi bảng (giây)")] private float contributionExpire = 10f;

    private EnemyStats stats;
    private GameObject lastHitAttacker;

    private class Entry
    {
        public GameObject attacker;
        public int damage;
        public float lastTime;
    }

    private readonly Dictionary<GameObject, Entry> table = new();

    public int ExpReward => expReward;
    public bool IsDead => stats.IsDead;

    private void Awake()
    {
        stats = GetComponent<EnemyStats>();
        stats.onDeath.AddListener(OnDeathInternal);
    }

    private void OnDestroy()
    {
        stats.onDeath.RemoveListener(OnDeathInternal);
    }

    public bool ApplyDamage(DamageContext ctx)
    {
        if (ctx.amount <= 0 || IsDead) return false;
        if (stats.TakeDamage(ctx.amount))
        {
            // Show floating combat text
            if (FloatingCombatTextSpawner.InstanceFCT)
            {
                FloatingCombatTextSpawner.InstanceFCT.ShowDamage(transform.position, ctx.amount, ctx.isCrit);
            }
            Record(ctx.attacker, ctx.amount);
            lastHitAttacker = ctx.attacker;
            return true;
        }
        return false;
    }

    private void Record(GameObject attacker, int dmg)
    {
        if (!attacker) return;
        if (!table.TryGetValue(attacker, out var e))
        {
            e = new Entry { attacker = attacker, damage = dmg, lastTime = Time.time };
            table.Add(attacker, e);
        }
        else
        {
            e.damage += dmg;
            e.lastTime = Time.time;
        }
    }

    private void OnDeathInternal()
    {
        var snaps = BuildSnapshots();
        //GameEvents.RaiseEnemyDied(stats, lastHitAttacker, snaps);
        table.Clear();
        lastHitAttacker = null;
    }

    private DamageSnapshot[] BuildSnapshots()
    {
        float now = Time.time;
        int total = 0;
        List<Entry> valid = new();
        foreach (var kv in table)
        {
            var e = kv.Value;
            if (now - e.lastTime > contributionExpire) continue;
            total += e.damage;
            valid.Add(e);
        }
        if (total <= 0) return new DamageSnapshot[0];

        List<DamageSnapshot> list = new();
        foreach (var e in valid)
        {
            float r = (float)e.damage / total;
            if (e.attacker != lastHitAttacker && r < minimalAssistRatio) continue;
            list.Add(new DamageSnapshot
            {
                attacker = e.attacker,
                totalDamage = e.damage,
                lastHitTime = e.lastTime,
                ratio = r
            });
        }

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].attacker == lastHitAttacker)
            {
                var s = list[i];
                s.ratio += lastHitBonusRatio;
                list[i] = s;
                break;
            }
        }

        float sum = 0f;
        foreach (var s in list) sum += s.ratio;
        if (sum > 0f)
        {
            for (int i = 0; i < list.Count; i++)
            {
                var s = list[i];
                s.ratio /= sum;
                list[i] = s;
            }
        }

        return list.ToArray();
    }
}
