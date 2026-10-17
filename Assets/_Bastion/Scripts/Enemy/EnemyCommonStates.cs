using Bastion.Combat;
using UnityEngine;

namespace Bastion.Enemy
{
    public sealed class EnemyHurtState : EnemyState
    {
        float endTime;

        public EnemyHurtState(EnemyController enemy) : base(enemy) { }

        public void Begin(in DamageInfo info)
        {
            endTime = Time.time + Enemy.Data.HurtDuration;
            float knockback = info.Knockback * (1f - Enemy.Data.KnockbackResistance);
            Enemy.SetVelocity(new Vector2(Mathf.Sign(info.Direction.x) * knockback, Enemy.Velocity.y));
            // 맞으면 공격자 쪽을 바라본다.
            Enemy.Face(-info.Direction.x);

            if (Enemy.StateMachine.CurrentState == this)
                Enter();
            else
                Enemy.StateMachine.ChangeState(this);
        }

        public override void Enter() => Enemy.PlayAnimation("Hurt");

        public override void FixedTick()
        {
            float x = Mathf.MoveTowards(Enemy.Velocity.x, 0f, 20f * Time.fixedDeltaTime);
            Enemy.MoveHorizontal(x);
            if (Time.time >= endTime)
                Enemy.StateMachine.ChangeState(Enemy.CombatState);
        }
    }

    public sealed class EnemyBlockState : EnemyState
    {
        float endTime;

        public EnemyBlockState(EnemyController enemy) : base(enemy) { }

        public override void Enter()
        {
            endTime = Time.time + Enemy.Data.BlockDuration;
            Enemy.Stop();
            Enemy.PlayAnimation("Block");
        }

        public override void Tick()
        {
            if (Time.time >= endTime)
                Enemy.StateMachine.ChangeState(Enemy.CombatState);
        }
    }

    public sealed class EnemyDeadState : EnemyState
    {
        public EnemyDeadState(EnemyController enemy) : base(enemy) { }

        public override void Enter() => Enemy.PlayAnimation("Death");
    }
}
