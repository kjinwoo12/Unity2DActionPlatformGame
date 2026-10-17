using UnityEngine;

namespace Bastion.Combat
{
    public readonly struct DamageInfo
    {
        public readonly int Amount;
        /// <summary>넉백 방향 (정규화). 보통 공격자 → 피격자 방향.</summary>
        public readonly Vector2 Direction;
        public readonly float Knockback;
        public readonly GameObject Source;

        public DamageInfo(int amount, Vector2 direction, float knockback, GameObject source)
        {
            Amount = amount;
            Direction = direction;
            Knockback = knockback;
            Source = source;
        }
    }
}
