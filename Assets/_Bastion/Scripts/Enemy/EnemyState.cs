using Bastion.Core;

namespace Bastion.Enemy
{
    public abstract class EnemyState : IState
    {
        protected readonly EnemyController Enemy;

        protected EnemyState(EnemyController enemy) => Enemy = enemy;

        public virtual void Enter() { }
        public virtual void Exit() { }
        public virtual void Tick() { }
        public virtual void FixedTick() { }
    }
}
