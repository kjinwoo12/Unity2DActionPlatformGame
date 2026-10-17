using Bastion.Combat;
using Bastion.Core;
using UnityEngine;

namespace Bastion.Enemy
{
    /// <summary>
    /// 적 공통 기반: 물리 이동, 감지, 피격/사망 처리. 행동 상태 구성은 파생 클래스가 정한다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Health))]
    public abstract class EnemyController : MonoBehaviour
    {
        [SerializeField] protected EnemyData data;
        [SerializeField] Animator animator;
        [SerializeField] Collider2D bodyCollider;
        [SerializeField] AttackTelegraph telegraph;

        Rigidbody2D body;
        readonly Collider2D[] senseBuffer = new Collider2D[4];

        public EnemyData Data => data;
        public Health Health { get; private set; }
        public StateMachine StateMachine { get; } = new StateMachine();
        public Transform Target { get; private set; }
        public int FacingDirection { get; private set; } = 1;
        public Vector2 HomePosition { get; private set; }
        public float LastAttackTime { get; set; } = float.NegativeInfinity;
        public Vector2 Velocity => body.linearVelocity;

        public EnemyHurtState HurtState { get; private set; }
        public EnemyDeadState DeadState { get; private set; }
        public EnemyBlockState BlockState { get; private set; }

        /// <summary>피격/방어가 끝난 뒤 돌아갈 기본 전투 상태.</summary>
        public abstract IState CombatState { get; }
        protected abstract IState InitialState { get; }

        protected virtual void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            Health = GetComponent<Health>();
            HomePosition = transform.position;
            HurtState = new EnemyHurtState(this);
            DeadState = new EnemyDeadState(this);
            BlockState = new EnemyBlockState(this);
            Health.BlockFilter = ShouldBlock;
        }

        protected virtual void OnEnable()
        {
            Health.Damaged += OnDamaged;
            Health.Blocked += OnBlocked;
            Health.Died += OnDied;
        }

        protected virtual void OnDisable()
        {
            Health.Damaged -= OnDamaged;
            Health.Blocked -= OnBlocked;
            Health.Died -= OnDied;
        }

        protected virtual void Start()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            Target = player != null ? player.transform : null;
            StateMachine.Initialize(InitialState);
        }

        protected virtual void Update() => StateMachine.Tick();

        protected virtual void FixedUpdate() => StateMachine.FixedTick();

        // ---------- 감지 ----------

        public float DistanceToTarget => Target == null ? float.PositiveInfinity : Vector2.Distance(transform.position, Target.position);
        public float HorizontalDirectionToTarget => Target == null ? FacingDirection : Mathf.Sign(Target.position.x - transform.position.x);

        public bool CanSeeTarget(float range)
        {
            if (Target == null || DistanceToTarget > range)
                return false;
            var targetHealth = Target.GetComponent<Health>();
            if (targetHealth != null && targetHealth.IsDead)
                return false;

            // 벽 너머의 대상은 보지 못한다.
            Vector2 eye = (Vector2)transform.position + Vector2.up * 1f;
            Vector2 to = (Vector2)Target.position + Vector2.up * 1f - eye;
            return !Physics2D.Raycast(eye, to.normalized, to.magnitude, data.GroundLayer);
        }

        /// <summary>앞에 벽이 있거나 발밑 앞쪽이 낭떠러지면 true.</summary>
        public bool IsBlockedAhead()
        {
            Bounds b = bodyCollider.bounds;
            Vector2 front = new Vector2(FacingDirection > 0 ? b.max.x : b.min.x, b.min.y);
            bool wall = Physics2D.Raycast(front + Vector2.up * 0.5f, Vector2.right * FacingDirection, 0.2f, data.GroundLayer);
            bool ground = Physics2D.Raycast(front + Vector2.right * FacingDirection * 0.2f + Vector2.up * 0.1f, Vector2.down, 0.6f, data.GroundLayer);
            return wall || !ground;
        }

        // ---------- 이동 ----------

        public void Face(float direction)
        {
            if (Mathf.Abs(direction) < 0.01f)
                return;
            int d = direction > 0f ? 1 : -1;
            if (d == FacingDirection)
                return;
            FacingDirection = d;
            transform.rotation = Quaternion.Euler(0f, d > 0 ? 0f : 180f, 0f);
        }

        public void MoveHorizontal(float speed) => body.linearVelocity = new Vector2(speed, body.linearVelocity.y);

        public void Stop() => MoveHorizontal(0f);

        public void SetVelocity(Vector2 velocity) => body.linearVelocity = velocity;

        // ---------- 공격 예고 ----------

        public void PlayTelegraph(float duration)
        {
            if (telegraph != null)
                telegraph.Play(duration);
        }

        void StopTelegraph()
        {
            if (telegraph != null)
                telegraph.Stop();
        }

        // ---------- 애니메이션 ----------

        public void PlayAnimation(string stateName)
        {
            if (animator != null)
                animator.Play(stateName, 0, 0f);
        }

        // ---------- 피격 ----------

        bool ShouldBlock(DamageInfo info)
        {
            if (data.BlockChance <= 0f || StateMachine.CurrentState == HurtState)
                return false;
            // 정면에서 들어온 공격만 막는다 (공격 방향이 내가 바라보는 방향의 반대).
            bool fromFront = Mathf.Sign(info.Direction.x) == -FacingDirection;
            return fromFront && Random.value < data.BlockChance;
        }

        void OnDamaged(DamageInfo info)
        {
            StopTelegraph();
            if (!Health.IsDead)
                HurtState.Begin(info);
        }

        void OnBlocked(DamageInfo info) => StateMachine.ChangeState(BlockState);

        void OnDied()
        {
            StopTelegraph();
            StateMachine.ChangeState(DeadState);
            bodyCollider.enabled = false;
            body.bodyType = RigidbodyType2D.Kinematic;
            body.linearVelocity = Vector2.zero;
            Destroy(gameObject, data.CorpseLifetime);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            if (data == null)
                return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, data.DetectRange);
            Gizmos.color = Color.red;
            HitboxQuery.DrawGizmo(transform, Application.isPlaying ? FacingDirection : 1, data.Attack);
        }
    }
}
