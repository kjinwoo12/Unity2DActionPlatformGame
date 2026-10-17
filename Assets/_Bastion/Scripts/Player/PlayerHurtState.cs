using Bastion.Combat;
using UnityEngine;

namespace Bastion.Player
{
    public sealed class PlayerHurtState : PlayerState
    {
        float endTime;

        public PlayerHurtState(PlayerController player) : base(player) { }

        public void Begin(in DamageInfo info)
        {
            var data = Player.CombatData;
            endTime = Time.time + data.HurtDuration;
            Player.Health.InvincibleUntil = Time.time + data.HurtInvincibleTime;

            float direction = Mathf.Sign(info.Direction.x == 0f ? -Player.FacingDirection : info.Direction.x);
            Player.SetVelocity(new Vector2(direction * data.HurtKnockback, data.HurtKnockback * 0.5f));

            Player.StateMachine.ChangeState(this);
        }

        public override void FixedTick()
        {
            Player.ApplyGravity();
            if (Time.time >= endTime)
                ReturnToMovement();
        }
    }
}
