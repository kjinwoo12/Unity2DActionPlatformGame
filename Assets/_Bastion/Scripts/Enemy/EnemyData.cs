using Bastion.Combat;
using UnityEngine;

namespace Bastion.Enemy
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "Bastion/Enemy Data")]
    public sealed class EnemyData : ScriptableObject
    {
        [Header("Movement")]
        [Min(0f)] public float PatrolSpeed = 1.5f;
        [Min(0f)] public float ChaseSpeed = 3f;
        [Tooltip("시작 위치 기준 순찰 반경")]
        [Min(0f)] public float PatrolRadius = 3f;

        [Header("Sensing")]
        public LayerMask TargetLayer;
        public LayerMask GroundLayer;
        [Min(0f)] public float DetectRange = 7f;
        [Tooltip("이 거리 밖으로 벗어나면 추적을 포기")]
        [Min(0f)] public float LoseRange = 11f;
        [Min(0f)] public float AttackRange = 1.8f;

        [Header("Attack")]
        public AttackStep Attack = new AttackStep { AnimationState = "Attack", Duration = 0.8f, ActiveStart = 0.4f, ActiveEnd = 0.55f, CancelTime = 0.8f, Damage = 15, Knockback = 5f, Lunge = 0f };
        [Min(0f)] public float AttackCooldown = 1.2f;

        [Header("Ranged (optional)")]
        public Projectile ProjectilePrefab;
        public Vector2 ProjectileSpawnOffset = new Vector2(0.8f, 1.2f);
        [Min(0f)] public float ProjectileSpeed = 7f;
        [Tooltip("원거리 적이 유지하려는 최소 거리")]
        [Min(0f)] public float PreferredDistance = 5f;

        [Header("Defense")]
        [Range(0f, 1f)] public float BlockChance;
        [Min(0f)] public float BlockDuration = 0.35f;

        [Header("Hurt")]
        [Min(0f)] public float HurtDuration = 0.3f;
        [Range(0f, 1f)] public float KnockbackResistance = 0f;
        [Min(0f)] public float CorpseLifetime = 2f;
    }
}
