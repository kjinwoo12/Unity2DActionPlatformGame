using Bastion.Enemy;
using UnityEngine;

namespace Bastion.Audio
{
    /// <summary>적이 공격을 내지르는 순간(근접 판정 시작, 투사체 발사)에 효과음을 낸다.</summary>
    [RequireComponent(typeof(EnemyController))]
    public sealed class EnemySounds : MonoBehaviour
    {
        [SerializeField] AudioClip attackSound;

        EnemyController enemy;

        void Awake() => enemy = GetComponent<EnemyController>();

        void OnEnable() => enemy.AttackReleased += OnAttackReleased;

        void OnDisable() => enemy.AttackReleased -= OnAttackReleased;

        void OnAttackReleased() => SoundManager.PlaySfx(attackSound);
    }
}
