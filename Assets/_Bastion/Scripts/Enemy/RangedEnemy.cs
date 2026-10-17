using Bastion.Core;
using UnityEngine;

namespace Bastion.Enemy
{
    /// <summary>제자리에서 대상을 감지하면 거리를 유지하며 투사체를 쏜다. 예: Wizard.</summary>
    public sealed class RangedEnemy : EnemyController
    {
        IdleState idle;
        CastState cast;

        public override IState CombatState => idle;
        protected override IState InitialState => idle;

        protected override void Awake()
        {
            base.Awake();
            idle = new IdleState(this);
            cast = new CastState(this);
        }

        void Fire()
        {
            if (Data.ProjectilePrefab == null)
                return;
            Vector2 offset = Data.ProjectileSpawnOffset;
            Vector3 spawn = transform.position + new Vector3(offset.x * FacingDirection, offset.y, 0f);
            Vector2 aim = Target != null ? ((Vector2)Target.position + Vector2.up - (Vector2)spawn).normalized : new Vector2(FacingDirection, 0f);
            var projectile = Instantiate(Data.ProjectilePrefab, spawn, Quaternion.identity);
            projectile.Launch(aim * Data.ProjectileSpeed, Data.Attack, Data.TargetLayer, Data.GroundLayer, gameObject);
        }

        sealed class IdleState : EnemyState
        {
            readonly RangedEnemy owner;
            public IdleState(RangedEnemy enemy) : base(enemy) => owner = enemy;

            public override void Enter()
            {
                Enemy.Stop();
                Enemy.PlayAnimation("Idle");
            }

            public override void FixedTick()
            {
                Enemy.Stop();
                if (!Enemy.CanSeeTarget(Enemy.Data.DetectRange))
                    return;

                Enemy.Face(Enemy.HorizontalDirectionToTarget);
                if (Time.time - Enemy.LastAttackTime >= Enemy.Data.AttackCooldown)
                    Enemy.StateMachine.ChangeState(owner.cast);
            }
        }

        sealed class CastState : EnemyState
        {
            readonly RangedEnemy owner;
            float startTime;
            bool fired;
            public CastState(RangedEnemy enemy) : base(enemy) => owner = enemy;

            public override void Enter()
            {
                startTime = Time.time;
                fired = false;
                Enemy.Stop();
                Enemy.LastAttackTime = Time.time;
                Enemy.PlayAnimation(Enemy.Data.Attack.AnimationState);
                Enemy.PlayTelegraph(Enemy.Data.Attack.ActiveStart);
            }

            public override void FixedTick()
            {
                float elapsed = Time.time - startTime;
                // 투사체는 ActiveStart 시점에 한 번 발사한다.
                if (!fired && elapsed >= Enemy.Data.Attack.ActiveStart)
                {
                    fired = true;
                    owner.Fire();
                }
                if (elapsed >= Enemy.Data.Attack.Duration)
                    Enemy.StateMachine.ChangeState(owner.idle);
            }
        }
    }
}
