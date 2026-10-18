using System;
using Bastion.Audio;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Bastion.Dialogue
{
    /// <summary>
    /// 화면 아래 대화 상자. 열려 있는 동안 게임 시간을 멈추고, 글자를 한 자씩 보여 준다.
    /// 넘기기 키를 누르면 글자를 모두 보여 주거나 다음 문장으로 넘어가고, 마지막 문장이면 닫는다.
    /// </summary>
    public sealed class DialogueBox : MonoBehaviour
    {
        [SerializeField] GameObject panel;
        [SerializeField] TMP_Text speakerLabel;
        [SerializeField] TMP_Text bodyLabel;
        [Tooltip("문장을 다 보여 줬을 때 나타나는 '다음' 표시")]
        [SerializeField] GameObject nextIndicator;
        [Tooltip("대화를 넘기는 입력 (예: 상호작용, 공격)")]
        [SerializeField] InputActionReference[] advanceActions;
        [SerializeField, Min(1f)] float charactersPerSecond = 30f;
        [SerializeField] AudioClip blipSound;
        [Tooltip("몇 글자마다 소리를 낼지")]
        [SerializeField, Min(1)] int blipEvery = 2;

        public static bool IsOpen { get; private set; }

        // 도메인 리로드를 끈 에디터에서도 플레이마다 초기화한다.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => IsOpen = false;

        DialogueData current;
        Action onClosed;
        int lineIndex;
        float visibleCount;
        int openedFrame;
        float savedTimeScale = 1f;

        bool IsTyping => bodyLabel.maxVisibleCharacters < bodyLabel.textInfo.characterCount;

        void Awake() => panel.SetActive(false);

        void OnEnable()
        {
            foreach (var action in advanceActions)
                action.action.Enable();
        }

        void OnDisable()
        {
            if (IsOpen && current != null)
                Close();
        }

        public void Open(DialogueData data, Action closed = null)
        {
            if (data == null || data.Lines == null || data.Lines.Length == 0)
            {
                closed?.Invoke();
                return;
            }

            current = data;
            onClosed = closed;
            IsOpen = true;
            openedFrame = Time.frameCount;
            savedTimeScale = Time.timeScale;
            Time.timeScale = 0f;

            panel.SetActive(true);
            speakerLabel.text = data.SpeakerName;
            ShowLine(0);
        }

        void ShowLine(int index)
        {
            lineIndex = index;
            visibleCount = 0f;
            bodyLabel.text = current.Lines[index];
            bodyLabel.maxVisibleCharacters = 0;
            bodyLabel.ForceMeshUpdate();
            nextIndicator.SetActive(false);
        }

        void Update()
        {
            if (!IsOpen || current == null)
                return;

            // 대화를 연 키 입력이 같은 프레임에 '넘기기'로 처리되지 않게 한다.
            if (Time.frameCount != openedFrame && AdvancePressed())
            {
                if (IsTyping)
                    bodyLabel.maxVisibleCharacters = bodyLabel.textInfo.characterCount;
                else if (lineIndex + 1 < current.Lines.Length)
                    ShowLine(lineIndex + 1);
                else
                {
                    Close();
                    return;
                }
            }

            if (IsTyping)
            {
                int before = bodyLabel.maxVisibleCharacters;
                visibleCount += charactersPerSecond * Time.unscaledDeltaTime;
                bodyLabel.maxVisibleCharacters = Mathf.Min(Mathf.FloorToInt(visibleCount), bodyLabel.textInfo.characterCount);
                if (bodyLabel.maxVisibleCharacters / blipEvery != before / blipEvery)
                    SoundManager.PlaySfx(blipSound, 0.6f);
            }

            nextIndicator.SetActive(!IsTyping);
        }

        bool AdvancePressed()
        {
            foreach (var action in advanceActions)
                if (action.action.WasPressedThisFrame())
                    return true;
            return false;
        }

        void Close()
        {
            panel.SetActive(false);
            IsOpen = false;
            current = null;
            Time.timeScale = savedTimeScale;
            var callback = onClosed;
            onClosed = null;
            callback?.Invoke();
        }
    }
}
