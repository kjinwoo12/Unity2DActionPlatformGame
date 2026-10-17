using System.Collections.Generic;
using Bastion.Combat;
using Bastion.Core;
using UnityEngine;

namespace Bastion.Enemy
{
    /// <summary>순찰 → 추적 → 근접 공격. 예: Skeleton.</summary>
    public sealed class MeleeEnemy : EnemyController
    {
        PatrolState patrol;
        ChaseState chase;
        AttackState attack;

        public override IState CombatState => chase;
        protected override IState InitialState => patrol;

        protected override void Awake()
        {
            base.Awake();
            patrol = new PatrolState(this);
            chase = new ChaseState(this);
            attack = new AttackState(this);
        }

        sealed class PatrolState : EnemyState
        {
            readonly MeleeEnemy owner;
            public PatrolState(MeleeEnemy enemy) : base(enemy) => owner = enemy;

            public override void Enter() => Enemy.PlayAnimation("Walk");

            public override void FixedTick()
            {
                if (Enemy.CanSeeTarget(Enemy.Data.DetectRange))
                {
                    Enemy.StateMachine.ChangeState(owner.chase);
                    return;
                }

                float offset = Enemy.transform.position.x - Enemy.HomePosition.x;
                if (Mathf.Abs(offset) > Enemy.Data.PatrolRadius && Mathf.Sign(offset) == Enemy.FacingDirection)
                    Enemy.Face(-Enemy.FacingDirection);
                else if (Enemy.IsBlockedAhead())
                    Enemy.Face(-Enemy.FacingDirection);

                Enemy.MoveHorizontal(Enemy.FacingDirection * Enemy.Data.PatrolSpeed);
            }
        }

        sealed class ChaseState : EnemyState
        {
            readonly MeleeEnemy owner;
            bool walking;
            public ChaseState(MeleeEnemy enemy) : base(enemy) => owner = enemy;

            public override void Enter() => walking = false;

            public override void FixedTick()
            {
                if (!Enemy.CanSeeTarget(Enemy.Data.LoseRange))
                {
                    Enemy.StateMachine.ChangeState(owner.patrol);
                    return;
                }

                Enemy.Face(Enemy.HorizontalDirectionToTarget);
                float dx = Mathf.Abs(Enemy.Target.position.x - Enemy.transform.position.x);

                if (dx <= Enemy.Data.AttackRange)
                {
                    SetWalking(false);
                    Enemy.Stop();
                    if (Time.time - Enemy.LastAttackTime >= Enemy.Data.AttackCooldown)
                        Enemy.StateMachine.ChangeState(owner.attack);
                    return;
                }

                if (Enemy.IsBlockedAhead())
                {
                    SetWalking(false);
                    Enemy.Stop();
                    return;
                }

                SetWalking(true);
                Enemy.MoveHorizontal(Enemy.FacingDirection * Enemy.Data.ChaseSpeed);
            }

            void SetWalking(bool value)
            {
                if (walking == value)
                    return;
                walking = value;
                Enemy.PlayAnimation(value ? "Walk" : "Idle");
            }
        }

        sealed class AttackState : EnemyState
        {
            readonly MeleeEnemy owner;
            readonly HashSet<IDamageable> hit = new HashSet<IDamageable>();
            float startTime;
            public AttackState(MeleeEnemy enemy) : base(enemy) => owner = enemy;

            public override void Enter()
            {
                startTime = Time.time;
                hit.Clear();
                Enemy.Stop();
                Enemy.LastAttackTime = Time.time;
                Enemy.PlayAnimation(Enemy.Data.Attack.AnimationState);
                // 판정 시작 직전까지 무기 주변으로 입자가 모인다.
                Enemy.PlayTelegraph(Enemy.Data.Attack.ActiveStart);
            }

            public override void FixedTick()
            {
                var step = Enemy.Data.Attack;
                float elapsed = Time.time - startTime;
                if (step.IsActive(elapsed))
                    HitboxQuery.Apply(Enemy.transform, Enemy.FacingDirection, step, Enemy.Data.TargetLayer, hit);

                if (elapsed >= step.Duration)
                    Enemy.StateMachine.ChangeState(owner.chase);
            }
        }
    }
}
