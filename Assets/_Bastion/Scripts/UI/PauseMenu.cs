using Bastion.Combat;
using Bastion.Dialogue;
using Bastion.Player;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bastion.UI
{
    /// <summary>
    /// 일시정지 메뉴. 일시정지 키로 열고 닫으며, 열려 있는 동안 Time.timeScale을 0으로 둔다.
    /// </summary>
    public sealed class PauseMenu : MonoBehaviour
    {
        [SerializeField] InputActionReference pauseAction;
        [SerializeField] GameObject panel;
        [SerializeField] Button resumeButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button titleButton;
        [SerializeField] SettingsPanel settingsPanel;
        [Tooltip("일시정지 동안 끌 플레이어 입력")]
        [SerializeField] PlayerInputReader playerInput;
        [Tooltip("'타이틀로' 버튼을 누르면 불러올 씬 이름")]
        [SerializeField] string titleScene;

        public bool IsPaused { get; private set; }

        void Awake()
        {
            resumeButton.onClick.AddListener(Resume);
            settingsButton.onClick.AddListener(() => settingsPanel.Open());
            titleButton.onClick.AddListener(GoToTitle);
            settingsPanel.Closed += () => Select(settingsButton);
            panel.SetActive(false);
            settingsPanel.gameObject.SetActive(false);
        }

        void OnEnable() => pauseAction.action.Enable();

        void Update()
        {
            if (!pauseAction.action.WasPressedThisFrame())
                return;

            if (!IsPaused)
            {
                // 대화 중이거나 히트스톱으로 잠깐 멈춘 순간에는 열지 않는다.
                if (!DialogueBox.IsOpen && !HitStop.IsStopped)
                    Pause();
            }
            else if (settingsPanel.gameObject.activeSelf)
                settingsPanel.Close();
            else
                Resume();
        }

        public void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            if (playerInput != null)
                playerInput.enabled = false;
            panel.SetActive(true);
            Select(resumeButton);
        }

        public void Resume()
        {
            IsPaused = false;
            Time.timeScale = 1f;
            if (playerInput != null)
                playerInput.enabled = true;
            settingsPanel.gameObject.SetActive(false);
            panel.SetActive(false);
        }

        void GoToTitle()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(titleScene);
        }

        // 일시정지한 채로 씬이 바뀌거나 종료돼도 시간이 멈춘 상태로 남지 않게 한다.
        void OnDestroy()
        {
            if (IsPaused)
                Time.timeScale = 1f;
        }

        static void Select(Selectable selectable)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }
}
