using UnityEngine;

namespace Bastion.Player
{
    public sealed class PlayerDeadState : PlayerState
    {
        public PlayerDeadState(PlayerController player) : base(player) { }

        public override void Enter() => Player.SetVelocity(new Vector2(0f, Player.Velocity.y));

        public override void FixedTick()
        {
            float x = Mathf.MoveTowards(Player.Velocity.x, 0f, Player.Data.GroundDeceleration * Time.fixedDeltaTime);
            Player.SetVelocity(new Vector2(x, Player.Velocity.y));
            Player.ApplyGravity();
        }
    }
}
