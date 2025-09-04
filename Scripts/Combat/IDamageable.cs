public interface IDamageable
{
    bool ApplyDamage(DamageContext ctx);
    bool IsDead { get; }
}
