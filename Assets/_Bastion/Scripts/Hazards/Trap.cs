using System.Collections.Generic;
using Bastion.Combat;
using UnityEngine;

namespace Bastion.Hazards
{
    /// <summary>
    /// 주기형 함정: 대기 → 예고 → 피해 → 복귀를 반복한다. 스프라이트 프레임을 코드가 직접 넘겨서
    /// 그림과 판정 시간이 항상 일치한다. alwaysActive면 상시 피해를 주는 고정 함정(가시 구덩이 등)이 된다.
    /// </summary>
    public sealed class Trap : MonoBehaviour
    {
        public enum Phase { Idle, Warning, Active, Recover }
        public enum KnockbackMode { AwayFromTrap, Forward }

        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] Sprite[] frames;
        [SerializeField, Min(1f)] float frameRate = 12f;

        [Header("Cycle (frame indices)")]
        [Tooltip("대기 중 보여줄 프레임")]
        [SerializeField, Min(0)] int idleFrame;
        [Tooltip("예고가 시작되는 프레임 (여기서부터 마지막 프레임까지 재생)")]
        [SerializeField, Min(0)] int warningFrame;
        [Tooltip("피해를 주는 프레임 범위 (포함)")]
        [SerializeField] Vector2Int activeFrames;
        [Tooltip("한 번 작동한 뒤 다음 작동까지 대기 시간")]
        [SerializeField, Min(0f)] float idleTime = 1.5f;
        [Tooltip("여러 함정을 엇갈리게 작동시키기 위한 시작 지연")]
        [SerializeField, Min(0f)] float startDelay;
        [Tooltip("항상 피해를 주는 고정 함정")]
        [SerializeField] bool alwaysActive;

        [Header("Damage")]
        [SerializeField] AttackStep hit = new AttackStep { Damage = 15, Knockback = 6f, HitboxOffset = new Vector2(0f, 0.5f), HitboxSize = new Vector2(1.6f, 1f) };
        [SerializeField] LayerMask targetLayer;
        [SerializeField] KnockbackMode knockback = KnockbackMode.AwayFromTrap;

        [Header("Telegraph (optional)")]
        [Tooltip("예고 단계에 재생할 입자 (그림만으로 예고가 잘 안 보일 때)")]
        [SerializeField] AttackTelegraph telegraph;

        readonly HashSet<IDamageable> hitThisCycle = new HashSet<IDamageable>();
        static readonly Collider2D[] Buffer = new Collider2D[8];
        int lastCycle = -1;
        float enabledTime;

        public Phase CurrentPhase { get; private set; } = Phase.Idle;
        public int CurrentFrame { get; private set; }
        public bool IsDangerous => alwaysActive || CurrentPhase == Phase.Active;

        float AnimationLength => frames == null || frames.Length == 0 ? 0f : (frames.Length - warningFrame) / frameRate;
        float CycleLength => idleTime + AnimationLength;
        int FacingDirection => transform.right.x >= 0f ? 1 : -1;

        // 활성화된 시점부터 대기 상태로 시작한다 (씬 로드 직후나 생성 직후 바로 작동하지 않도록).
        void OnEnable()
        {
            enabledTime = Time.time;
            lastCycle = -1;
        }

        void Update()
        {
            if (alwaysActive || frames == null || frames.Length == 0)
            {
                CurrentPhase = alwaysActive ? Phase.Active : Phase.Idle;
                return;
            }

            float time = Time.time - enabledTime - startDelay;
            if (time < 0f)
            {
                SetFrame(idleFrame, Phase.Idle);
                return;
            }

            int cycle = Mathf.FloorToInt(time / CycleLength);
            if (cycle != lastCycle)
            {
                lastCycle = cycle;
                hitThisCycle.Clear();
            }

            float t = time - cycle * CycleLength;
            if (t < idleTime)
            {
                SetFrame(idleFrame, Phase.Idle);
                return;
            }

            int frame = Mathf.Min(warningFrame + Mathf.FloorToInt((t - idleTime) * frameRate), frames.Length - 1);
            Phase phase = frame < activeFrames.x ? Phase.Warning
                : frame <= activeFrames.y ? Phase.Active
                : Phase.Recover;
            SetFrame(frame, phase);
        }

        void SetFrame(int frame, Phase phase)
        {
            if (phase == Phase.Warning && CurrentPhase != Phase.Warning && telegraph != null)
                telegraph.Play((activeFrames.x - frame) / frameRate);
            CurrentFrame = frame;
            CurrentPhase = phase;
            if (spriteRenderer != null && frames != null && frame < frames.Length)
                spriteRenderer.sprite = frames[frame];
        }

        void FixedUpdate()
        {
            if (!IsDangerous)
                return;

            // 고정 함정은 매 판정마다 새로 검사하고, 연속 피해는 피격 무적 시간이 막는다.
            if (alwaysActive)
                hitThisCycle.Clear();

            Vector2 center = (Vector2)transform.position + new Vector2(hit.HitboxOffset.x * FacingDirection, hit.HitboxOffset.y);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = targetLayer, useTriggers = false };
            int count = Physics2D.OverlapBox(center, hit.HitboxSize, 0f, filter, Buffer);
            for (int i = 0; i < count; i++)
            {
                var target = Buffer[i].GetComponentInParent<IDamageable>();
                if (target == null || target.IsDead || !hitThisCycle.Add(target))
                    continue;

                float x = knockback == KnockbackMode.Forward
                    ? FacingDirection
                    : Mathf.Sign(Buffer[i].transform.position.x - transform.position.x);
                target.TakeDamage(new DamageInfo(hit.Damage, new Vector2(x == 0f ? 1f : x, 0f), hit.Knockback, gameObject));
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.4f, 0f);
            HitboxQuery.DrawGizmo(transform, FacingDirection, hit);
        }
    }
}
