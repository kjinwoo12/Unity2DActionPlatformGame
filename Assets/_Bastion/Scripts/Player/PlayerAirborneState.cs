namespace Bastion.Player
{
    public sealed class PlayerAirborneState : PlayerState
    {
        public PlayerAirborneState(PlayerController player) : base(player) { }

        public override void Tick()
        {
            Player.UpdateFacing(Player.Input.MoveX);

            if (Player.TryStartDashOrAttack())
                return;

            if (!Player.HasBufferedJump)
                return;

            // 발판을 막 벗어났으면 지상 점프, 아니면 남은 공중 점프 사용
            if (Player.IsInCoyoteTime)
                Player.Jump();
            else if (Player.CanAirJump)
                Player.AirJump();
        }

        public override void FixedTick()
        {
            if (Player.IsGrounded)
            {
                Player.StateMachine.ChangeState(Player.GroundedState);
                return;
            }

            Player.ApplyHorizontalMovement(Player.Data.AirAcceleration, Player.Data.AirDeceleration);
            Player.ApplyGravity();
        }
    }
}
