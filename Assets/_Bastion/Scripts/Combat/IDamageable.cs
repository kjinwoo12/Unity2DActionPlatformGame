namespace Bastion.Combat
{
    public interface IDamageable
    {
        bool IsDead { get; }

        /// <returns>실제로 피해가 들어갔으면 true (무적, 사망, 방어 등으로 막히면 false)</returns>
        bool TakeDamage(in DamageInfo info);
    }
}
