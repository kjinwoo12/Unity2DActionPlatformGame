using Bastion.Player;
using UnityEngine;

namespace Bastion.Dialogue
{
    /// <summary>
    /// 말을 거는 NPC. 플레이어가 주변(트리거 범위)에 들어오면 안내 표시를 띄우고,
    /// 상호작용 키를 누르면 대화 상자를 연다. 대화 중에는 플레이어 입력을 끈다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class DialogueNpc : MonoBehaviour
    {
        [SerializeField] DialogueData dialogue;
        [SerializeField] DialogueBox dialogueBox;
        [Tooltip("말을 걸 수 있을 때 머리 위에 보여 줄 안내 표시")]
        [SerializeField] GameObject prompt;

        PlayerInputReader playerInput;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (prompt != null)
                prompt.SetActive(false);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;
            playerInput = other.GetComponentInParent<PlayerInputReader>();
            SetPrompt(true);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;
            playerInput = null;
            SetPrompt(false);
        }

        void Update()
        {
            if (playerInput == null || DialogueBox.IsOpen || !playerInput.enabled)
                return;

            if (playerInput.ConsumeInteract())
            {
                SetPrompt(false);
                playerInput.enabled = false;
                dialogueBox.Open(dialogue, OnDialogueClosed);
            }
        }

        void OnDialogueClosed()
        {
            if (playerInput == null)
                return;
            playerInput.enabled = true;
            SetPrompt(true);
        }

        void SetPrompt(bool visible)
        {
            if (prompt != null)
                prompt.SetActive(visible);
        }
    }
}
