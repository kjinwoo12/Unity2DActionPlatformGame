using System.Collections.Generic;
using UnityEngine;

namespace Bastion.Combat
{
    /// <summary>
    /// OverlapBox 기반 근접 판정. 트리거 콜라이더 대신 판정 프레임에 직접 질의해서 타이밍을 코드가 결정한다.
    /// </summary>
    public static class HitboxQuery
    {
        static readonly Collider2D[] Buffer = new Collider2D[16];

        public static Vector2 WorldCenter(Transform owner, int facing, Vector2 offset) =>
            (Vector2)owner.position + new Vector2(offset.x * facing, offset.y);

        /// <summary>
        /// 히트박스 안의 IDamageable을 찾아 아직 맞지 않은 대상에게 피해를 준다.
        /// </summary>
        /// <returns>이번 호출에서 새로 피해를 준 대상 수</returns>
        public static int Apply(Transform owner, int facing, AttackStep step, LayerMask targetMask,
            HashSet<IDamageable> alreadyHit)
        {
            Vector2 center = WorldCenter(owner, facing, step.HitboxOffset);
            var filter = new ContactFilter2D { useLayerMask = true, layerMask = targetMask, useTriggers = true };
            int count = Physics2D.OverlapBox(center, step.HitboxSize, 0f, filter, Buffer);

            int hits = 0;
            for (int i = 0; i < count; i++)
            {
                var target = Buffer[i].GetComponentInParent<IDamageable>();
                if (target == null || target.IsDead || !alreadyHit.Add(target))
                    continue;

                var direction = new Vector2(facing, 0f);
                if (target.TakeDamage(new DamageInfo(step.Damage, direction, step.Knockback, owner.gameObject)))
                    hits++;
            }
            return hits;
        }

        public static void DrawGizmo(Transform owner, int facing, AttackStep step)
        {
            Gizmos.DrawWireCube(WorldCenter(owner, facing, step.HitboxOffset), step.HitboxSize);
        }
    }
}
