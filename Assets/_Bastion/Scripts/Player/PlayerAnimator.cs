using Bastion.Core;
using UnityEngine;

namespace Bastion.Player
{
    /// <summary>
    /// PlayerController의 상태를 Animator로 전달한다. 게임 로직은 애니메이션에 의존하지 않는다.
    /// 이동 계열은 파라미터로 전이하고, 공격/피격/사망은 코드에서 직접 재생한다.
    /// </summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class PlayerAnimator : MonoBehaviour
    {
        [SerializeField] Animator animator;

        static readonly int SpeedX = Animator.StringToHash("SpeedX");
        static readonly int VelocityY = Animator.StringToHash("VelocityY");
        static readonly int IsGrounded = Animator.StringToHash("IsGrounded");
        static readonly int IsDashing = Animator.StringToHash("IsDashing");
        static readonly int InAction = Animator.StringToHash("InAction");
        static readonly int HurtState = Animator.StringToHash("Hurt");
        static readonly int DeathState = Animator.StringToHash("Death");
        static readonly int JumpState = Animator.StringToHash("Jump");

        PlayerController player;

        void Awake() => player = GetComponent<PlayerController>();

        void OnEnable()
        {
            player.AttackStarted += OnAttackStarted;
            player.AirJumped += OnAirJumped;
            player.StateMachine.StateChanged += OnStateChanged;
        }

        void OnDisable()
        {
            player.AttackStarted -= OnAttackStarted;
            player.AirJumped -= OnAirJumped;
            player.StateMachine.StateChanged -= OnStateChanged;
        }

        void OnAttackStarted(int comboIndex) =>
            animator.Play(player.CombatData.Combo[comboIndex].AnimationState, 0, 0f);

        // 공중 점프는 같은 Jump 상태라 전이가 일어나지 않으므로 처음부터 다시 재생한다.
        void OnAirJumped() => animator.Play(JumpState, 0, 0f);

        void OnStateChanged(IState previous, IState next)
        {
            if (next == player.HurtState)
                animator.Play(HurtState, 0, 0f);
            else if (next == player.DeadState)
                animator.Play(DeathState, 0, 0f);
        }

        void LateUpdate()
        {
            var state = player.StateMachine.CurrentState;
            animator.SetFloat(SpeedX, Mathf.Abs(player.Velocity.x));
            animator.SetFloat(VelocityY, player.Velocity.y);
            animator.SetBool(IsGrounded, player.IsGrounded);
            animator.SetBool(IsDashing, state == player.DashState);
            animator.SetBool(InAction, state == player.AttackState || state == player.HurtState || state == player.DeadState);
        }
    }
}
