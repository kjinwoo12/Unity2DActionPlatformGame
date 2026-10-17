using Bastion.Combat;
using UnityEngine;

namespace Bastion.Player
{
    [CreateAssetMenu(fileName = "PlayerCombatData", menuName = "Bastion/Player Combat Data")]
    public sealed class PlayerCombatData : ScriptableObject
    {
        public LayerMask TargetLayer;
        [Tooltip("공격 입력을 미리 보관하는 시간")]
        [Min(0f)] public float AttackBufferTime = 0.25f;
        [Tooltip("공중 공격 시 수평 속도 배율")]
        [Range(0f, 1f)] public float AirAttackMoveFactor = 0.6f;
        public AttackStep[] Combo =
        {
            new AttackStep { AnimationState = "Attack1", Duration = 0.27f, ActiveStart = 0.06f, ActiveEnd = 0.18f, CancelTime = 0.18f, Damage = 10, Knockback = 3f, HitStop = 0.04f, Lunge = 2f },
            new AttackStep { AnimationState = "Attack2", Duration = 0.4f, ActiveStart = 0.13f, ActiveEnd = 0.27f, CancelTime = 0.27f, Damage = 12, Knockback = 3.5f, HitStop = 0.05f, Lunge = 2.5f },
            new AttackStep { AnimationState = "Attack3", Duration = 0.6f, ActiveStart = 0.33f, ActiveEnd = 0.47f, CancelTime = 0.5f, Damage = 22, Knockback = 8f, HitStop = 0.09f, Lunge = 4f, HitboxSize = new Vector2(2.6f, 2.0f) },
        };

        [Header("Hurt")]
        [Min(0f)] public float HurtDuration = 0.3f;
        [Min(0f)] public float HurtInvincibleTime = 0.8f;
        [Min(0f)] public float HurtKnockback = 6f;
    }
}
