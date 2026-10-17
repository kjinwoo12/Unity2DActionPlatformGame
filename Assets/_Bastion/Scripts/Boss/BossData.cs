using Bastion.Combat;
using UnityEngine;

namespace Bastion.Boss
{
    [CreateAssetMenu(fileName = "BossData", menuName = "Bastion/Boss Data")]
    public sealed class BossData : ScriptableObject
    {
        public LayerMask TargetLayer;

        [Header("Movement")]
        [Tooltip("아레나 바닥 기준 부유 높이")]
        [Min(0f)] public float HoverHeight = 0.6f;
        [Min(0f)] public float MoveSpeed = 3f;
        [Tooltip("이 거리보다 멀면 빠르게 활공해 접근한다")]
        [Min(0f)] public float FarDistance = 6f;
        [Min(1f)] public float FarSpeedMultiplier = 2f;
        [Min(0f)] public float BobAmplitude = 0.15f;
        [Min(0f)] public float BobFrequency = 1.5f;

        [Header("Decision")]
        [Min(0f)] public float IdleTime = 0.9f;
        [Min(0f)] public float SlashRange = 3.2f;
        [Min(0f)] public float SpikeRange = 2.8f;
        [Tooltip("2페이즈에서 이 시간 동안 접근하지 못하면 소환으로 전환한다")]
        [Min(0f)] public float MaxChaseTime = 2.5f;

        [Header("Telegraph")]
        [Tooltip("공격 전 예비 동작 시간 (입자가 모이는 동안 첫 프레임에서 멈춤)")]
        [Min(0f)] public float TelegraphTime = 0.5f;
        [Range(0.1f, 1f)] public float Phase2TelegraphMultiplier = 0.7f;

        [Header("Pattern 1: Scythe Slash (2 hits)")]
        public string SlashAnimation = "Attack";
        [Min(0.01f)] public float SlashDuration = 1.1f;
        public AttackStep[] SlashHits =
        {
            new AttackStep { ActiveStart = 0.17f, ActiveEnd = 0.33f, Damage = 18, Knockback = 7f, HitboxOffset = new Vector2(1.6f, 1.6f), HitboxSize = new Vector2(3.2f, 3.0f) },
            new AttackStep { ActiveStart = 0.75f, ActiveEnd = 0.92f, Damage = 18, Knockback = 8f, HitboxOffset = new Vector2(1.8f, 1.0f), HitboxSize = new Vector2(3.6f, 1.6f) },
        };

        [Header("Pattern 2: Spike Burst")]
        public string SpikeAnimation = "Skill";
        public AttackStep Spike = new AttackStep { Duration = 1.0f, ActiveStart = 0.42f, ActiveEnd = 0.83f, Damage = 22, Knockback = 9f, HitboxOffset = new Vector2(0f, 1.5f), HitboxSize = new Vector2(6f, 3f) };

        [Header("Pattern 3: Summon (phase 2)")]
        public string SummonAnimation = "Summon";
        [Min(0.01f)] public float SummonDuration = 0.9f;
        [Min(0f)] public float SummonSpawnTime = 0.55f;
        public SummonMinion MinionPrefab;
        [Min(0)] public int MinionCount = 2;
        [Min(0)] public int MaxMinionsAlive = 4;

        [Header("Phase 2")]
        [Range(0f, 1f)] public float Phase2HealthRatio = 0.5f;
        [Min(1f)] public float Phase2SpeedMultiplier = 1.35f;
        [Range(0.1f, 1f)] public float Phase2IdleMultiplier = 0.6f;
        public string PhaseAnimation = "Phase";
        [Min(0f)] public float PhaseTransitionTime = 1.4f;
    }
}
