using System.Collections.Generic;
using Bastion.Combat;
using UnityEngine;

namespace Bastion.Player
{
    /// <summary>
    /// 3타 콤보. 각 동작은 AttackStep의 시간표대로 판정하고, 캔슬 시점 이후 버퍼된 입력으로 다음 동작이나 대시로 넘어간다.
    /// </summary>
    public sealed class PlayerAttackState : PlayerState
    {
        readonly HashSet<IDamageable> hitThisSwing = new HashSet<IDamageable>();
        float stepStartTime;

        public int ComboIndex { get; private set; }
        public AttackStep CurrentStep => Player.CombatData.Combo[ComboIndex];

        public PlayerAttackState(PlayerController player) : base(player) { }

        public void BeginCombo()
        {
            ComboIndex = 0;
            if (Player.StateMachine.CurrentState == this)
                StartStep();
            else
                Player.StateMachine.ChangeState(this);
        }

        public override void Enter() => StartStep();

        void StartStep()
        {
            Player.Input.ConsumeAttack();
            Player.UpdateFacing(Player.Input.MoveX);
            hitThisSwing.Clear();
            stepStartTime = Time.time;

            if (Player.IsGrounded)
                Player.SetVelocity(new Vector2(Player.FacingDirection * CurrentStep.Lunge, Player.Velocity.y));

            Player.RaiseAttackStarted(ComboIndex);
        }

        public override void Tick()
        {
            float elapsed = Time.time - stepStartTime;
            if (elapsed < CurrentStep.CancelTime)
                return;

            if (Player.HasBufferedDash && Player.IsDashReady && (Player.IsGrounded || Player.CanAirDash))
            {
                Player.StateMachine.ChangeState(Player.DashState);
                return;
            }

            if (Player.HasBufferedAttack && ComboIndex < Player.CombatData.Combo.Length - 1)
            {
                ComboIndex++;
                StartStep();
                return;
            }

            if (elapsed >= CurrentStep.Duration)
                ReturnToMovement();
        }

        public override void FixedTick()
        {
            float elapsed = Time.time - stepStartTime;

            if (Player.IsGrounded)
            {
                // 지상 공격 중에는 런지 속도를 서서히 줄인다.
                float x = Mathf.MoveTowards(Player.Velocity.x, 0f, Player.Data.GroundDeceleration * 0.5f * Time.fixedDeltaTime);
                Player.SetVelocity(new Vector2(x, Player.Velocity.y));
            }
            else
            {
                Player.ApplyHorizontalMovement(Player.Data.AirAcceleration, Player.Data.AirDeceleration, Player.CombatData.AirAttackMoveFactor);
            }
            Player.ApplyGravity();

            if (CurrentStep.IsActive(elapsed))
            {
                int hits = HitboxQuery.Apply(Player.transform, Player.FacingDirection, CurrentStep,
                    Player.CombatData.TargetLayer, hitThisSwing);
                if (hits > 0)
                    Player.RaiseAttackLanded(CurrentStep, hits);
            }
        }
    }
}
