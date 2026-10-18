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
        [SerializeField] InputActionReference interactAction;
        [Tooltip("상호작용 입력을 보관하는 시간 (실시간)")]
        [SerializeField, Min(0f)] float interactBufferTime = 0.2f;

        const float NotPressed = float.NegativeInfinity;

        public float MoveX { get; private set; }
        public bool JumpHeld { get; private set; }
        public float LastJumpPressedTime { get; private set; } = NotPressed;
        public float LastDashPressedTime { get; private set; } = NotPressed;
        public float LastAttackPressedTime { get; private set; } = NotPressed;
        public float LastInteractPressedTime { get; private set; } = NotPressed;

        void OnEnable()
        {
            moveAction.action.Enable();
            jumpAction.action.Enable();
            dashAction.action.Enable();
            attackAction.action.Enable();
            if (interactAction != null)
            {
                interactAction.action.Enable();
                interactAction.action.performed += OnInteractPerformed;
            }
            jumpAction.action.performed += OnJumpPerformed;
            dashAction.action.performed += OnDashPerformed;
            attackAction.action.performed += OnAttackPerformed;
        }

        void OnDisable()
        {
            jumpAction.action.performed -= OnJumpPerformed;
            dashAction.action.performed -= OnDashPerformed;
            attackAction.action.performed -= OnAttackPerformed;
            if (interactAction != null)
                interactAction.action.performed -= OnInteractPerformed;
            // 비활성화된 동안(대화, 일시정지)에는 이동 입력이 남아 있지 않게 한다.
            MoveX = 0f;
            JumpHeld = false;
        }

        void Update()
        {
            MoveX = moveAction.action.ReadValue<float>();
            JumpHeld = jumpAction.action.IsPressed();
        }

        void OnJumpPerformed(InputAction.CallbackContext _) => LastJumpPressedTime = Time.time;

        void OnDashPerformed(InputAction.CallbackContext _) => LastDashPressedTime = Time.time;

        void OnAttackPerformed(InputAction.CallbackContext _) => LastAttackPressedTime = Time.time;

        void OnInteractPerformed(InputAction.CallbackContext _) => LastInteractPressedTime = Time.unscaledTime;

        public void ConsumeJump() => LastJumpPressedTime = NotPressed;

        public void ConsumeDash() => LastDashPressedTime = NotPressed;

        public void ConsumeAttack() => LastAttackPressedTime = NotPressed;

        /// <summary>상호작용 키가 보관 시간 안에 눌렸으면 true를 돌려주고 입력을 소비한다.</summary>
        public bool ConsumeInteract()
        {
            if (Time.unscaledTime - LastInteractPressedTime > interactBufferTime)
                return false;
            LastInteractPressedTime = NotPressed;
            return true;
        }
    }
}
