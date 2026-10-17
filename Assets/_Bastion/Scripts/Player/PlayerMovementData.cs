using UnityEngine;

namespace Bastion.Player
{
    /// <summary>
    /// 플레이어 이동 수치. 점프는 높이와 정점 도달 시간으로 정의하고 중력과 초기 속도를 역산한다.
    /// </summary>
    [CreateAssetMenu(fileName = "PlayerMovementData", menuName = "Bastion/Player Movement Data")]
    public sealed class PlayerMovementData : ScriptableObject
    {
        [Header("Run")]
        [Min(0f)] public float MaxRunSpeed = 7f;
        [Min(0f)] public float GroundAcceleration = 70f;
        [Min(0f)] public float GroundDeceleration = 90f;
        [Min(0f)] public float AirAcceleration = 45f;
        [Min(0f)] public float AirDeceleration = 35f;

        [Header("Jump")]
        [Min(0.1f)] public float JumpHeight = 3.2f;
        [Min(0.05f)] public float TimeToJumpApex = 0.38f;
        [Tooltip("점프 버튼을 일찍 떼었을 때 상승 중 중력 배율 (가변 점프)")]
        [Min(1f)] public float JumpCutGravityMultiplier = 2.5f;
        [Tooltip("하강 중 중력 배율")]
        [Min(1f)] public float FallGravityMultiplier = 1.7f;
        [Min(0f)] public float MaxFallSpeed = 20f;
        [Tooltip("발판을 벗어난 뒤에도 점프를 허용하는 시간")]
        [Min(0f)] public float CoyoteTime = 0.1f;
        [Tooltip("착지 직전에 누른 점프 입력을 보관하는 시간")]
        [Min(0f)] public float JumpBufferTime = 0.12f;
        [Tooltip("공중에서 추가로 뛸 수 있는 횟수 (1 = 더블점프)")]
        [Min(0)] public int MaxAirJumps = 1;
        [Tooltip("공중 점프 높이")]
        [Min(0.1f)] public float AirJumpHeight = 2.4f;

        [Header("Dash (Roll)")]
        [Tooltip("구르기 이동 속도")]
        [Min(0f)] public float DashSpeed = 8.5f;
        [Min(0.01f)] public float DashDuration = 0.4f;
        [Min(0f)] public float DashCooldown = 0.3f;
        [Tooltip("구르기 중 무적 시간 (시작부터)")]
        [Min(0f)] public float DashInvincibleTime = 0.32f;

        [Header("Ground Check")]
        public LayerMask GroundLayer;
        [Min(0f)] public float GroundCheckDistance = 0.1f;
        [Tooltip("지상에서 이동 중 바닥(경사 포함)에 붙어 있게 하는 아래 방향 속도")]
        [Min(0f)] public float GroundStickSpeed = 4f;

        public float Gravity => 2f * JumpHeight / (TimeToJumpApex * TimeToJumpApex);
        public float JumpVelocity => Gravity * TimeToJumpApex;
        public float AirJumpVelocity => Mathf.Sqrt(2f * Gravity * AirJumpHeight);
    }
}
