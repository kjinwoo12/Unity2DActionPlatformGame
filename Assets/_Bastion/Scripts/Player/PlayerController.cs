using System;
using Bastion.Combat;
using Bastion.Core;
using UnityEngine;

namespace Bastion.Player
{
    /// <summary>
    /// 플레이어의 진입점. 물리 상태(접지, 속도, 방향)와 공용 이동 함수를 제공하고
    /// 실제 행동 결정은 상태(PlayerGroundedState 등)에 맡긴다.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(PlayerInputReader))]
    [RequireComponent(typeof(Health))]
    public sealed class PlayerController : MonoBehaviour
    {
        [SerializeField] PlayerMovementData data;
        [SerializeField] PlayerCombatData combatData;

        Rigidbody2D body;
        Collider2D bodyCollider;
        readonly RaycastHit2D[] groundHits = new RaycastHit2D[4];

        public PlayerMovementData Data => data;
        public PlayerCombatData CombatData => combatData;
        public PlayerInputReader Input { get; private set; }
        public Health Health { get; private set; }
        public StateMachine StateMachine { get; } = new StateMachine();

        public PlayerGroundedState GroundedState { get; private set; }
        public PlayerAirborneState AirborneState { get; private set; }
        public PlayerDashState DashState { get; private set; }
        public PlayerAttackState AttackState { get; private set; }
        public PlayerHurtState HurtState { get; private set; }
        public PlayerDeadState DeadState { get; private set; }

        public bool IsGrounded { get; private set; }
        public float LastGroundedTime { get; private set; } = float.NegativeInfinity;
        public int FacingDirection { get; private set; } = 1;
        public Vector2 Velocity => body.linearVelocity;
        public bool CanAirDash { get; set; } = true;
        public int AirJumpsRemaining { get; private set; }
        public float LastDashEndTime { get; set; } = float.NegativeInfinity;
        public bool IsInvincible => Health.IsInvincible;

        /// <summary>공격이 대상을 맞혔을 때 (동작, 맞힌 수). 히트스톱 등 연출용.</summary>
        public event Action<AttackStep, int> AttackLanded;
        /// <summary>콤보 동작이 시작될 때 (콤보 인덱스).</summary>
        public event Action<int> AttackStarted;
        /// <summary>공중 점프(더블점프)를 했을 때.</summary>
        public event Action AirJumped;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            Input = GetComponent<PlayerInputReader>();
            Health = GetComponent<Health>();

            // 중력은 PlayerMovementData 기준으로 직접 적용한다.
            body.gravityScale = 0f;

            GroundedState = new PlayerGroundedState(this);
            AirborneState = new PlayerAirborneState(this);
            DashState = new PlayerDashState(this);
            AttackState = new PlayerAttackState(this);
            HurtState = new PlayerHurtState(this);
            DeadState = new PlayerDeadState(this);
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

        void Start() => StateMachine.Initialize(AirborneState);

        void Update() => StateMachine.Tick();

        void FixedUpdate()
        {
            UpdateGrounded();
            StateMachine.FixedTick();
        }

        void OnDamaged(DamageInfo info)
        {
            if (!Health.IsDead)
                HurtState.Begin(info);
        }

        void OnDied() => StateMachine.ChangeState(DeadState);

        void UpdateGrounded()
        {
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = data.GroundLayer, useTriggers = false };
            int count = bodyCollider.Cast(Vector2.down, filter, groundHits, data.GroundCheckDistance);

            IsGrounded = false;
            for (int i = 0; i < count; i++)
            {
                // 벽 옆면에 닿은 것은 접지로 보지 않는다.
                if (groundHits[i].normal.y > 0.7f)
                {
                    IsGrounded = true;
                    break;
                }
            }

            // 상승 중에는 발판을 스쳐도 접지로 처리하지 않는다.
            if (IsGrounded && body.linearVelocity.y > 0.01f)
                IsGrounded = false;

            if (IsGrounded)
            {
                LastGroundedTime = Time.time;
                CanAirDash = true;
                AirJumpsRemaining = data.MaxAirJumps;
            }
        }

        public bool HasBufferedJump => Time.time - Input.LastJumpPressedTime <= data.JumpBufferTime;
        public bool HasBufferedDash => Time.time - Input.LastDashPressedTime <= data.JumpBufferTime;
        public bool HasBufferedAttack => Time.time - Input.LastAttackPressedTime <= combatData.AttackBufferTime;
        public bool IsInCoyoteTime => Time.time - LastGroundedTime <= data.CoyoteTime;
        public bool IsDashReady => Time.time - LastDashEndTime >= data.DashCooldown;

        public void ApplyHorizontalMovement(float acceleration, float deceleration, float speedFactor = 1f)
        {
            float target = Input.MoveX * data.MaxRunSpeed * speedFactor;
            float rate = Mathf.Abs(target) > 0.01f ? acceleration : deceleration;
            float x = Mathf.MoveTowards(body.linearVelocity.x, target, rate * Time.fixedDeltaTime);
            body.linearVelocity = new Vector2(x, body.linearVelocity.y);
        }

        public void ApplyGravity()
        {
            float multiplier = 1f;
            if (body.linearVelocity.y < 0f)
                multiplier = data.FallGravityMultiplier;
            else if (body.linearVelocity.y > 0f && !Input.JumpHeld)
                multiplier = data.JumpCutGravityMultiplier;

            float y = body.linearVelocity.y - data.Gravity * multiplier * Time.fixedDeltaTime;
            body.linearVelocity = new Vector2(body.linearVelocity.x, Mathf.Max(y, -data.MaxFallSpeed));
        }

        /// <summary>
        /// 지상에서는 중력을 누적하지 않는다. 가만히 있으면 세로 속도 0 (경사에서 미끄러지지 않음),
        /// 움직이는 중에는 약하게 눌러 내리막에서도 바닥에서 떨어지지 않게 한다.
        /// </summary>
        public void StickToGround()
        {
            float vy = Mathf.Abs(body.linearVelocity.x) > 0.01f ? -data.GroundStickSpeed : 0f;
            body.linearVelocity = new Vector2(body.linearVelocity.x, vy);
        }

        public void Jump()
        {
            Input.ConsumeJump();
            LastGroundedTime = float.NegativeInfinity; // 코요테 점프 중복 방지
            body.linearVelocity = new Vector2(body.linearVelocity.x, data.JumpVelocity);
        }

        public bool CanAirJump => AirJumpsRemaining > 0;

        public void AirJump()
        {
            Input.ConsumeJump();
            AirJumpsRemaining--;
            body.linearVelocity = new Vector2(body.linearVelocity.x, data.AirJumpVelocity);
            AirJumped?.Invoke();
        }

        public void SetVelocity(Vector2 velocity) => body.linearVelocity = velocity;

        public void UpdateFacing(float moveX)
        {
            if (Mathf.Abs(moveX) < 0.01f)
                return;

            int direction = moveX > 0f ? 1 : -1;
            if (direction == FacingDirection)
                return;

            FacingDirection = direction;
            // 스케일 대신 회전으로 뒤집어 자식 오브젝트까지 함께 반전시킨다.
            transform.rotation = Quaternion.Euler(0f, direction > 0 ? 0f : 180f, 0f);
        }

        /// <summary>지상/공중 상태에서 공통으로 쓰는 행동 우선순위: 대시 → 공격.</summary>
        public bool TryStartDashOrAttack()
        {
            if (HasBufferedDash && IsDashReady && (IsGrounded || CanAirDash))
            {
                StateMachine.ChangeState(DashState);
                return true;
            }
            if (HasBufferedAttack)
            {
                AttackState.BeginCombo();
                return true;
            }
            return false;
        }

        internal void RaiseAttackStarted(int comboIndex) => AttackStarted?.Invoke(comboIndex);

        internal void RaiseAttackLanded(AttackStep step, int hits) => AttackLanded?.Invoke(step, hits);

        void OnDrawGizmosSelected()
        {
            if (combatData == null)
                return;
            Gizmos.color = Color.red;
            foreach (var step in combatData.Combo)
                HitboxQuery.DrawGizmo(transform, Application.isPlaying ? FacingDirection : 1, step);
        }
    }
}
