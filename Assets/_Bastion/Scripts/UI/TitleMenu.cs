using Bastion.Audio;
using Bastion.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bastion.UI
{
    /// <summary>타이틀 화면: 게임 시작, 설정, 종료.</summary>
    public sealed class TitleMenu : MonoBehaviour
    {
        [Tooltip("시작 버튼을 누르면 불러올 씬 이름 (Build Profiles의 씬 목록에 있어야 한다)")]
        [SerializeField] string firstStageScene;
        [SerializeField] Button startButton;
        [SerializeField] Button settingsButton;
        [SerializeField] Button quitButton;
        [SerializeField] SettingsPanel settingsPanel;
        [SerializeField] AudioClip confirmSound;

        void Awake()
        {
            startButton.onClick.AddListener(StartGame);
            settingsButton.onClick.AddListener(OpenSettings);
            quitButton.onClick.AddListener(Quit);
            settingsPanel.Closed += OnSettingsClosed;
            settingsPanel.gameObject.SetActive(false);
        }

        void Start()
        {
            Time.timeScale = 1f;
            Select(startButton);
        }

        void StartGame()
        {
            SoundManager.PlaySfx(confirmSound);
            // 새 게임은 처음부터 시작한다.
            CheckpointSystem.Clear();
            SceneManager.LoadScene(firstStageScene);
        }

        void OpenSettings()
        {
            SoundManager.PlaySfx(confirmSound);
            settingsPanel.Open();
        }

        void OnSettingsClosed() => Select(settingsButton);

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        static void Select(Selectable selectable)
        {
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(selectable.gameObject);
        }
    }
}
