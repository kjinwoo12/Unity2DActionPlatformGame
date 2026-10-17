using Bastion.Core;

namespace Bastion.Player
{
    public abstract class PlayerState : IState
    {
        protected readonly PlayerController Player;

        protected PlayerState(PlayerController player) => Player = player;

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Tick() { }
        public virtual void FixedTick() { }

        /// <summary>지상 또는 공중 상태로 복귀한다.</summary>
        protected void ReturnToMovement() =>
            Player.StateMachine.ChangeState(Player.IsGrounded ? Player.GroundedState : Player.AirborneState);
    }
}
