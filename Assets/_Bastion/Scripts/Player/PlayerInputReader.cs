using UnityEngine;
using UnityEngine.InputSystem;

namespace Bastion.Player
{
    /// <summary>
    /// Input System 액션을 읽어 게임플레이 코드가 쓰기 쉬운 형태로 노출한다.
    /// 버튼 입력은 "눌린 시점"을 기록해 두고, 소비하는 쪽에서 버퍼 시간 안인지 판단한다.
    /// </summary>
    public sealed class PlayerInputReader : MonoBehaviour
    {
        [SerializeField] InputActionReference moveAction;
        [SerializeField] InputActionReference jumpAction;
        [SerializeField] InputActionReference dashAction;
        [SerializeField] InputActionReference attackAction;

        const float NotPressed = float.NegativeInfinity;

        public float MoveX { get; private set; }
        public bool JumpHeld { get; private set; }
        public float LastJumpPressedTime { get; private set; } = NotPressed;
        public float LastDashPressedTime { get; private set; } = NotPressed;
        public float LastAttackPressedTime { get; private set; } = NotPressed;

        void OnEnable()
        {
            moveAction.action.Enable();
            jumpAction.action.Enable();
            dashAction.action.Enable();
            attackAction.action.Enable();
            jumpAction.action.performed += OnJumpPerformed;
            dashAction.action.performed += OnDashPerformed;
            attackAction.action.performed += OnAttackPerformed;
        }

        void OnDisable()
        {
            jumpAction.action.performed -= OnJumpPerformed;
            dashAction.action.performed -= OnDashPerformed;
            attackAction.action.performed -= OnAttackPerformed;
        }

        void Update()
        {
            MoveX = moveAction.action.ReadValue<float>();
            JumpHeld = jumpAction.action.IsPressed();
        }

        void OnJumpPerformed(InputAction.CallbackContext _) => LastJumpPressedTime = Time.time;

        void OnDashPerformed(InputAction.CallbackContext _) => LastDashPressedTime = Time.time;

        void OnAttackPerformed(InputAction.CallbackContext _) => LastAttackPressedTime = Time.time;

        public void ConsumeJump() => LastJumpPressedTime = NotPressed;

        public void ConsumeDash() => LastDashPressedTime = NotPressed;

        public void ConsumeAttack() => LastAttackPressedTime = NotPressed;
    }
}
