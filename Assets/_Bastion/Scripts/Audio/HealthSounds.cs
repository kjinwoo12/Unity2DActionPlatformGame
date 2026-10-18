using Bastion.Combat;
using UnityEngine;

namespace Bastion.Audio
{
    /// <summary>Health 이벤트(피격, 방어, 사망)에 맞춰 효과음을 낸다. 플레이어, 적, 보스에 공통으로 붙인다.</summary>
    [RequireComponent(typeof(Health))]
    public sealed class HealthSounds : MonoBehaviour
    {
        [SerializeField] AudioClip hurtSound;
        [SerializeField] AudioClip blockSound;
        [SerializeField] AudioClip deathSound;
        [SerializeField] AudioClip healSound;

        Health health;

        void Awake() => health = GetComponent<Health>();

        void OnEnable()
        {
            health.Damaged += OnDamaged;
            health.Blocked += OnBlocked;
            health.Died += OnDied;
            health.Healed += OnHealed;
        }

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            health.Blocked -= OnBlocked;
            health.Died -= OnDied;
            health.Healed -= OnHealed;
        }

        // 죽는 순간에는 피격음 대신 사망음만 낸다.
        void OnDamaged(DamageInfo _)
        {
            if (!health.IsDead)
                SoundManager.PlaySfx(hurtSound);
        }

        void OnBlocked(DamageInfo _) => SoundManager.PlaySfx(blockSound);

        void OnDied() => SoundManager.PlaySfx(deathSound);

        void OnHealed(int amount) => SoundManager.PlaySfx(healSound);
    }
}
