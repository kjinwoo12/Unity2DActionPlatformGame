using System;
using UnityEngine;

namespace Bastion.Combat
{
    /// <summary>
    /// 공격 한 동작의 데이터. 시간은 동작 시작 기준(초), 히트박스는 오른쪽을 바라볼 때 기준이다.
    /// </summary>
    [Serializable]
    public sealed class AttackStep
    {
        public string AnimationState = "Attack1";
        [Min(0.01f)] public float Duration = 0.3f;
        [Min(0f)] public float ActiveStart = 0.07f;
        [Min(0f)] public float ActiveEnd = 0.2f;
        [Tooltip("이 시간 이후부터 다음 콤보나 대시로 캔슬할 수 있다")]
        [Min(0f)] public float CancelTime = 0.2f;

        [Min(0)] public int Damage = 10;
        [Min(0f)] public float Knockback = 4f;
        [Tooltip("히트 시 경직 연출 시간 (실시간)")]
        [Min(0f)] public float HitStop = 0.05f;
        [Tooltip("공격 시작 시 앞으로 내딛는 속도")]
        public float Lunge = 2f;

        public Vector2 HitboxOffset = new Vector2(1.3f, 1.1f);
        public Vector2 HitboxSize = new Vector2(2.2f, 1.8f);

        public bool IsActive(float elapsed) => elapsed >= ActiveStart && elapsed <= ActiveEnd;
    }
}
