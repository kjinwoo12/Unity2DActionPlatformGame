using System;
using Bastion.Audio;
using Bastion.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Bastion.UI
{
    /// <summary>음량 설정 창. 슬라이더 값은 GameSettings를 통해 PlayerPrefs에 바로 저장된다.</summary>
    public sealed class SettingsPanel : MonoBehaviour
    {
        [SerializeField] Slider musicSlider;
        [SerializeField] Slider sfxSlider;
        [SerializeField] Button closeButton;
        [Tooltip("효과음 슬라이더를 움직일 때 들려줄 소리")]
        [SerializeField] AudioClip previewSound;

        public event Action Closed;

        void Awake()
        {
            musicSlider.onValueChanged.AddListener(value => GameSettings.MusicVolume = value);
            sfxSlider.onValueChanged.AddListener(OnSfxChanged);
            closeButton.onClick.AddListener(Close);
        }

        void OnEnable()
        {
            // 저장된 값으로 슬라이더를 맞춘다. 이때는 저장 이벤트가 다시 일어나지 않게 한다.
            musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            sfxSlider.SetValueWithoutNotify(GameSettings.SfxVolume);
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(musicSlider.gameObject);
        }

        void OnSfxChanged(float value)
        {
            GameSettings.SfxVolume = value;
            SoundManager.PlaySfx(previewSound);
        }

        public void Open() => gameObject.SetActive(true);

        public void Close()
        {
            gameObject.SetActive(false);
            Closed?.Invoke();
        }
    }
}
