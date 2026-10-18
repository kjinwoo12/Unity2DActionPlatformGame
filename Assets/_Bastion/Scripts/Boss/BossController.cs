using System;
using System.Collections.Generic;
using Bastion.Combat;
using Bastion.Core;
using UnityEngine;

namespace Bastion.Boss
{
    /// <summary>
    /// 타락한 수호자. 공중에 떠서 플레이어를 따라가며 낫 베기 / 가시 폭발 / 소환(2페이즈) 패턴을 사용한다.
    /// 피격 경직이 없는 슈퍼아머이며, 체력이 절반 이하가 되면 2페이즈로 전환한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public sealed class BossController : MonoBehaviour
    {
        [SerializeField] BossData data;
        [SerializeField] Animator animator;
        [SerializeField] AttackTelegraph telegraph;
        [SerializeField] Transform target;
        [Tooltip("부유 높이의 기준이 되는 아레나 바닥 y")]
        [SerializeField] float arenaFloorY;
        [SerializeField] Vector2 arenaRangeX = new Vector2(-100f, 100f);

        Rigidbody2D body;
        readonly List<SummonMinion> minions = new List<SummonMinion>();

        public BossData Data => data;
        public Health Health { get; private set; }
        public StateMachine StateMachine { get; } = new StateMachine();
        public int Phase { get; private set; } = 1;
        public int FacingDirection { get; private set; } = -1;
        public bool IsActive => StateMachine.CurrentState != null && StateMachine.CurrentState != dormant;
        public Transform Target => target;

        public event Action<int> PhaseChanged;
        /// <summary>행동을 시작할 때 (BossData의 애니메이션 이름). 공격은 예비 동작이 끝나는 순간에 알린다. 효과음 등 연출용.</summary>
        public event Action<string> ActionStarted;

        DormantState dormant;
        IdleState idle;
        ChaseState chase;
        SlashState slash;
        SpikeState spike;
        SummonState summon;
        PhaseState phaseChange;
        DeadState dead;

        float SpeedMultiplier => Phase >= 2 ? data.Phase2SpeedMultiplier : 1f;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            Health = GetComponent<Health>();

            dormant = new DormantState(this);
            idle = new IdleState(this);
            chase = new ChaseState(this);
            slash = new SlashState(this);
            spike = new SpikeState(this);
            summon = new SummonState(this);
            phaseChange = new PhaseState(this);
            dead = new DeadState(this);
        }

        void OnEnable()
        {
            Health.Damaged += OnDamaged;
            Health.Died += OnDied;
        }

        void OnDisable()
        {
            Health.Damaged -= OnDamaged;
            Health.Died -= OnDied;
        }

        void Start()
        {
            if (target == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null) target = player.transform;
            }
            Face(-1);
            StateMachine.Initialize(dormant);
        }

        void Update() => StateMachine.Tick();

        void FixedUpdate() => StateMachine.FixedTick();

        /// <summary>아레나 진입 시 호출되어 전투를 시작한다.</summary>
        public void Activate()
        {
            if (StateMachine.CurrentState != dormant)
                return;
            Health.InvincibleUntil = float.NegativeInfinity;
            StateMachine.ChangeState(idle);
        }

        void OnDamaged(DamageInfo info)
        {
            if (Phase == 1 && Health.Current <= Health.Max * data.Phase2HealthRatio && !Health.IsDead)
                StateMachine.ChangeState(phaseChange);
        }

        void OnDied()
        {
            StateMachine.ChangeState(dead);
            foreach (var minion in minions)
                if (minion != null) minion.Kill();
        }

        // ---------- 공용 동작 ----------

        float DistanceX => target == null ? float.PositiveInfinity : Mathf.Abs(target.position.x - transform.position.x);
        bool TargetAlive
        {
            get
            {
                if (target == null)
                    return false;
                var health = target.GetComponent<Health>();
                return health == null || !health.IsDead;
            }
        }

        void Face(float direction)
        {
            if (Mathf.Abs(direction) < 0.01f)
                return;
            FacingDirection = direction > 0f ? 1 : -1;
            transform.rotation = Quaternion.Euler(0f, FacingDirection > 0 ? 0f : 180f, 0f);
        }

        void FaceTarget()
        {
            if (target != null)
                Face(target.position.x - transform.position.x);
        }

        void Hover(float horizontalSpeed)
        {
            float x = Mathf.Clamp(body.position.x + horizontalSpeed * Time.fixedDeltaTime, arenaRangeX.x, arenaRangeX.y);
            float y = arenaFloorY + data.HoverHeight + Mathf.Sin(Time.time * data.BobFrequency * Mathf.PI * 2f) * data.BobAmplitude;
            body.MovePosition(new Vector2(x, y));
        }

        void Play(string state)
        {
            if (animator != null)
                animator.Play(state, 0, 0f);
        }

        int AliveMinions()
        {
            minions.RemoveAll(m => m == null);
            return minions.Count;
        }

        // ---------- 상태 ----------

        abstract class BossState : IState
        {
            protected readonly BossController Boss;
            protected float StartTime;
            protected float Elapsed => Time.time - StartTime;
            protected BossState(BossController boss) => Boss = boss;
            public virtual void Enter() => StartTime = Time.time;
            public virtual void Exit() { }
            public virtual void Tick() { }
            public virtual void FixedTick() => Boss.Hover(0f);
        }

        sealed class DormantState : BossState
        {
            public DormantState(BossController boss) : base(boss) { }
            public override void Enter()
            {
                base.Enter();
                Boss.Play("Idle");
                // 전투 시작 전에는 피해를 받지 않는다.
                Boss.Health.InvincibleUntil = float.PositiveInfinity;
            }
        }

        sealed class IdleState : BossState
        {
            public IdleState(BossController boss) : base(boss) { }

            public override void Enter() { base.Enter(); Boss.Play("Idle"); }

            public override void Tick()
            {
                Boss.FaceTarget();
                float wait = Boss.data.IdleTime * (Boss.Phase >= 2 ? Boss.data.Phase2IdleMultiplier : 1f);
                if (Elapsed < wait || !Boss.TargetAlive)
                    return;

                // 2페이즈: 소환수가 적으면 일정 확률로 소환
                if (Boss.Phase >= 2 && Boss.AliveMinions() < Boss.data.MaxMinionsAlive && UnityEngine.Random.value < 0.3f)
                {
                    Boss.StateMachine.ChangeState(Boss.summon);
                    return;
                }

                if (Boss.DistanceX <= Boss.data.SpikeRange && UnityEngine.Random.value < 0.45f)
                    Boss.StateMachine.ChangeState(Boss.spike);
                else
                    Boss.StateMachine.ChangeState(Boss.chase);
            }
        }

        sealed class ChaseState : BossState
        {
            public ChaseState(BossController boss) : base(boss) { }

            public override void FixedTick()
            {
                Boss.FaceTarget();
                if (Boss.DistanceX <= Boss.data.SlashRange)
                {
                    Boss.Hover(0f);
                    Boss.StateMachine.ChangeState(Boss.slash);
                    return;
                }
                if (Boss.Phase >= 2 && Elapsed >= Boss.data.MaxChaseTime && Boss.AliveMinions() < Boss.data.MaxMinionsAlive)
                {
                    // 2페이즈: 오래 따라가도 못 닿으면 소환수로 압박
                    Boss.StateMachine.ChangeState(Boss.summon);
                    return;
                }
                float speed = Boss.data.MoveSpeed * Boss.SpeedMultiplier;
                if (Boss.DistanceX > Boss.data.FarDistance)
                    speed *= Boss.data.FarSpeedMultiplier;
                Boss.Hover(Boss.FacingDirection * speed);
            }
        }

        abstract class AttackState : BossState
        {
            readonly HashSet<IDamageable> hit = new HashSet<IDamageable>();
            bool released;
            protected AttackState(BossController boss) : base(boss) { }
            protected abstract string Animation { get; }
            protected abstract float Duration { get; }

            public override void Enter()
            {
                base.Enter();
                released = false;
                Boss.FaceTarget();
                Boss.Play(Animation);

                // 예비 동작: 첫 프레임에서 멈춘 채 입자가 모이고, 그 뒤에 공격 시간표가 시작된다.
                float windup = Boss.data.TelegraphTime * (Boss.Phase >= 2 ? Boss.data.Phase2TelegraphMultiplier : 1f);
                StartTime = Time.time + windup;
                Boss.animator.speed = 0f;
                if (Boss.telegraph != null)
                    Boss.telegraph.Play(windup + FirstActiveStart / Boss.SpeedMultiplier);
            }

            public override void Exit()
            {
                Boss.animator.speed = 1f;
                if (Boss.telegraph != null)
                    Boss.telegraph.Stop();
            }

            // 첫 판정이 시작되는 시점 (예고 입자를 여기까지 유지)
            protected abstract float FirstActiveStart { get; }

            // 2페이즈에서는 애니메이션과 판정 시간표가 함께 빨라진다. 예비 동작 중에는 음수.
            protected float ScaledElapsed => Elapsed * Boss.SpeedMultiplier;

            protected void ApplyHit(AttackStep step)
            {
                if (step.IsActive(ScaledElapsed))
                    HitboxQuery.Apply(Boss.transform, Boss.FacingDirection, step, Boss.data.TargetLayer, hit);
                else if (ScaledElapsed > step.ActiveEnd)
                    hit.Clear(); // 다음 판정 구간을 위해 초기화
            }

            public override void FixedTick()
            {
                base.FixedTick();
                Boss.animator.speed = Elapsed < 0f ? 0f : Boss.SpeedMultiplier;
                if (!released && Elapsed >= 0f)
                {
                    released = true;
                    Boss.ActionStarted?.Invoke(Animation);
                }
                if (ScaledElapsed >= Duration)
                    Boss.StateMachine.ChangeState(Boss.idle);
            }
        }

        sealed class SlashState : AttackState
        {
            public SlashState(BossController boss) : base(boss) { }
            protected override string Animation => Boss.data.SlashAnimation;
            protected override float Duration => Boss.data.SlashDuration;
            protected override float FirstActiveStart => Boss.data.SlashHits[0].ActiveStart;

            public override void FixedTick()
            {
                foreach (var step in Boss.data.SlashHits)
                    ApplyHit(step);
                base.FixedTick();
            }
        }

        sealed class SpikeState : AttackState
        {
            public SpikeState(BossController boss) : base(boss) { }
            protected override string Animation => Boss.data.SpikeAnimation;
            protected override float Duration => Boss.data.Spike.Duration;
            protected override float FirstActiveStart => Boss.data.Spike.ActiveStart;

            public override void FixedTick()
            {
                ApplyHit(Boss.data.Spike);
                base.FixedTick();
            }
        }

        sealed class SummonState : BossState
        {
            bool spawned;
            public SummonState(BossController boss) : base(boss) { }

            public override void Enter()
            {
                base.Enter();
                spawned = false;
                Boss.FaceTarget();
                Boss.Play(Boss.data.SummonAnimation);
                Boss.ActionStarted?.Invoke(Boss.data.SummonAnimation);
            }

            public override void FixedTick()
            {
                base.FixedTick();
                if (!spawned && Elapsed >= Boss.data.SummonSpawnTime)
                {
                    spawned = true;
                    Spawn();
                }
                if (Elapsed >= Boss.data.SummonDuration)
                    Boss.StateMachine.ChangeState(Boss.idle);
            }

            void Spawn()
            {
                if (Boss.data.MinionPrefab == null)
                    return;
                int count = Mathf.Min(Boss.data.MinionCount, Boss.data.MaxMinionsAlive - Boss.AliveMinions());
                for (int i = 0; i < count; i++)
                {
                    float side = i % 2 == 0 ? -1f : 1f;
                    Vector3 position = Boss.transform.position + new Vector3(side * (1.5f + i), 2.5f, 0f);
                    var minion = Instantiate(Boss.data.MinionPrefab, position, Quaternion.identity);
                    minion.Initialize(Boss.target, Boss.data.TargetLayer);
                    Boss.minions.Add(minion);
                }
            }
        }

        sealed class PhaseState : BossState
        {
            public PhaseState(BossController boss) : base(boss) { }

            public override void Enter()
            {
                base.Enter();
                Boss.Phase = 2;
                Boss.Health.InvincibleUntil = Time.time + Boss.data.PhaseTransitionTime;
                Boss.Play(Boss.data.PhaseAnimation);
                Boss.ActionStarted?.Invoke(Boss.data.PhaseAnimation);
                Boss.PhaseChanged?.Invoke(2);
            }

            public override void FixedTick()
            {
                base.FixedTick();
                if (Elapsed >= Boss.data.PhaseTransitionTime)
                    Boss.StateMachine.ChangeState(Boss.summon);
            }
        }

        sealed class DeadState : BossState
        {
            public DeadState(BossController boss) : base(boss) { }

            public override void Enter()
            {
                base.Enter();
                Boss.Play("Death");
                foreach (var col in Boss.GetComponentsInChildren<Collider2D>())
                    col.enabled = false;
            }

            public override void FixedTick() { }
        }

        void OnDrawGizmosSelected()
        {
            if (data == null)
                return;
            int facing = Application.isPlaying ? FacingDirection : -1;
            Gizmos.color = Color.red;
            foreach (var step in data.SlashHits)
                HitboxQuery.DrawGizmo(transform, facing, step);
            Gizmos.color = Color.magenta;
            HitboxQuery.DrawGizmo(transform, facing, data.Spike);
        }
    }
}
