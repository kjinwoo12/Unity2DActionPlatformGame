using Bastion.Combat;
using Unity.Cinemachine;
using UnityEngine;

namespace Bastion.Player
{
    /// <summary>
    /// 플레이어 전투 연출: 공격 적중 시 히트스톱과 카메라 흔들림, 피격 시 더 강한 흔들림.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerFeedback : MonoBehaviour
    {
        [SerializeField] CinemachineImpulseSource impulseSource;
        [SerializeField, Min(0f)] float hitShakeForce = 0.15f;
        [SerializeField, Min(0f)] float finisherShakeForce = 0.35f;
        [SerializeField, Min(0f)] float hurtShakeForce = 0.5f;
        [SerializeField, Min(0f)] float hurtHitStop = 0.08f;

        PlayerController player;
        Health health;

        // 다른 컴포넌트의 Awake 순서에 의존하지 않도록 필요한 참조를 직접 가져온다.
        void Awake()
        {
            player = GetComponent<PlayerController>();
            health = GetComponent<Health>();
        }

        void OnEnable()
        {
            player.AttackLanded += OnAttackLanded;
            health.Damaged += OnDamaged;
        }

        void OnDisable()
        {
            player.AttackLanded -= OnAttackLanded;
            health.Damaged -= OnDamaged;
        }

        void OnAttackLanded(AttackStep step, int hits)
        {
            HitStop.Request(step.HitStop);
            bool isFinisher = step == player.CombatData.Combo[player.CombatData.Combo.Length - 1];
            Shake(new Vector2(player.FacingDirection, 0f), isFinisher ? finisherShakeForce : hitShakeForce);
        }

        void OnDamaged(DamageInfo info)
        {
            HitStop.Request(hurtHitStop);
            Shake(info.Direction, hurtShakeForce);
        }

        void Shake(Vector2 direction, float force)
        {
            if (impulseSource != null)
                impulseSource.GenerateImpulseWithVelocity(direction.normalized * force);
        }
    }
}
