using System;
using UnityEngine;

namespace Bastion.Combat
{
    /// <summary>
    /// 체력과 무적 시간을 관리한다. 피격 반응(경직, 넉백, 연출)은 Damaged 이벤트를 구독해서 처리한다.
    /// </summary>
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1)] int maxHealth = 100;
        [Tooltip("피격 후 무적 시간")]
        [SerializeField, Min(0f)] float invincibleAfterHit;

        public int Max => maxHealth;
        public int Current { get; private set; }
        public bool IsDead => Current <= 0;
        public float InvincibleUntil { get; set; } = float.NegativeInfinity;
        public bool IsInvincible => Time.time < InvincibleUntil;

        /// <summary>true를 반환하면 피해를 막는다 (예: 방패 방어).</summary>
        public Func<DamageInfo, bool> BlockFilter { get; set; }

        public event Action<DamageInfo> Damaged;
        public event Action<DamageInfo> Blocked;
        public event Action Died;
        public event Action<int, int> Changed;

        void Awake() => Current = maxHealth;

        public bool TakeDamage(in DamageInfo info)
        {
            if (IsDead || IsInvincible)
                return false;

            if (BlockFilter != null && BlockFilter(info))
            {
                Blocked?.Invoke(info);
                return false;
            }

            Current = Mathf.Max(0, Current - info.Amount);
            InvincibleUntil = Time.time + invincibleAfterHit;
            Changed?.Invoke(Current, maxHealth);
            Damaged?.Invoke(info);

            if (IsDead)
                Died?.Invoke();
            return true;
        }

        public void ResetHealth()
        {
            Current = maxHealth;
            InvincibleUntil = float.NegativeInfinity;
            Changed?.Invoke(Current, maxHealth);
        }
    }
}
