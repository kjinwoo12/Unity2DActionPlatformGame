using Bastion.Combat;
using Bastion.Core;
using Bastion.Player;
using UnityEngine;

namespace Bastion.Audio
{
    /// <summary>플레이어 행동 이벤트에 맞춰 효과음을 낸다. 게임 로직은 이 컴포넌트를 모른다.</summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerSounds : MonoBehaviour
    {
        [SerializeField] AudioClip jumpSound;
        [SerializeField] AudioClip airJumpSound;
        [SerializeField] AudioClip rollSound;
        [Tooltip("콤보 순서대로 (1타, 2타, 3타)")]
        [SerializeField] AudioClip[] swingSounds;
        [SerializeField] AudioClip hitSound;

        PlayerController player;

        void Awake() => player = GetComponent<PlayerController>();

        void OnEnable()
        {
            player.Jumped += OnJumped;
            player.AirJumped += OnAirJumped;
            player.AttackStarted += OnAttackStarted;
            player.AttackLanded += OnAttackLanded;
            player.StateMachine.StateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            player.Jumped -= OnJumped;
            player.AirJumped -= OnAirJumped;
            player.AttackStarted -= OnAttackStarted;
            player.AttackLanded -= OnAttackLanded;
            player.StateMachine.StateChanged -= OnStateChanged;
        }

        void OnJumped() => SoundManager.PlaySfx(jumpSound);

        void OnAirJumped() => SoundManager.PlaySfx(airJumpSound);

        void OnAttackStarted(int comboIndex)
        {
            if (swingSounds != null && swingSounds.Length > 0)
                SoundManager.PlaySfx(swingSounds[Mathf.Min(comboIndex, swingSounds.Length - 1)]);
        }

        void OnAttackLanded(AttackStep step, int hits) => SoundManager.PlaySfx(hitSound);

        void OnStateChanged(IState previous, IState next)
        {
            if (next == player.DashState)
                SoundManager.PlaySfx(rollSound);
        }
    }
}
