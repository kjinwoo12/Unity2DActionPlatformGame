using UnityEngine;

namespace Bastion.Player
{
    public sealed class PlayerDashState : PlayerState
    {
        float endTime;

        public PlayerDashState(PlayerController player) : base(player) { }

        public override void Enter()
        {
            Player.Input.ConsumeDash();

            // 방향 입력이 있으면 그쪽으로, 없으면 바라보는 방향으로 대시한다.
            Player.UpdateFacing(Player.Input.MoveX);
            if (!Player.IsGrounded)
                Player.CanAirDash = false;

            endTime = Time.time + Player.Data.DashDuration;
            Player.Health.InvincibleUntil = Mathf.Max(Player.Health.InvincibleUntil, Time.time + Player.Data.DashInvincibleTime);
            Player.SetVelocity(new Vector2(Player.FacingDirection * Player.Data.DashSpeed, Player.Velocity.y));
        }

        public override void Exit()
        {
            Player.LastDashEndTime = Time.time;
            // 대시 관성을 최대 달리기 속도로 제한해 미끄러지지 않게 한다.
            float x = Mathf.Clamp(Player.Velocity.x, -Player.Data.MaxRunSpeed, Player.Data.MaxRunSpeed);
            Player.SetVelocity(new Vector2(x, Player.Velocity.y));
        }

        public override void FixedTick()
        {
            // 구르는 동안 수평 속도는 고정하고 중력은 그대로 받는다.
            Player.SetVelocity(new Vector2(Player.FacingDirection * Player.Data.DashSpeed, Player.Velocity.y));
            Player.ApplyGravity();

            if (Time.time >= endTime)
                ReturnToMovement();
        }
    }
}
