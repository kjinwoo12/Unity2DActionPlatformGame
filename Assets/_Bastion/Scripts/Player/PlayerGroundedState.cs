namespace Bastion.Player
{
    public sealed class PlayerGroundedState : PlayerState
    {
        public PlayerGroundedState(PlayerController player) : base(player) { }

        public override void Tick()
        {
            Player.UpdateFacing(Player.Input.MoveX);

            if (Player.TryStartDashOrAttack())
                return;

            if (Player.HasBufferedJump)
            {
                Player.Jump();
                Player.StateMachine.ChangeState(Player.AirborneState);
            }
        }

        public override void FixedTick()
        {
            if (!Player.IsGrounded)
            {
                Player.StateMachine.ChangeState(Player.AirborneState);
                return;
            }

            Player.ApplyHorizontalMovement(Player.Data.GroundAcceleration, Player.Data.GroundDeceleration);
            Player.StickToGround();
        }
    }
}
