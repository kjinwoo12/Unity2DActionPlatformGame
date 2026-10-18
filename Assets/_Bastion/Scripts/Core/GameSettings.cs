using System;
using UnityEngine;

namespace Bastion.Core
{
    /// <summary>
    /// 플레이어가 바꾸는 설정(음량)을 PlayerPrefs에 저장하고 불러온다.
    /// 기본값은 이 클래스 한 곳에만 두고, 실제 값은 설정 화면에서 바꾼다.
    /// </summary>
    public static class GameSettings
    {
        const string MusicVolumeKey = "Bastion.MusicVolume";
        const string SfxVolumeKey = "Bastion.SfxVolume";
        const float DefaultMusicVolume = 0.7f;
        const float DefaultSfxVolume = 0.8f;

        /// <summary>음량이 바뀌었을 때 (배경음, 효과음).</summary>
        public static event Action Changed;

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolumeKey, DefaultMusicVolume);
            set => SetVolume(MusicVolumeKey, value);
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, DefaultSfxVolume);
            set => SetVolume(SfxVolumeKey, value);
        }

        static void SetVolume(string key, float value)
        {
            PlayerPrefs.SetFloat(key, Mathf.Clamp01(value));
            PlayerPrefs.Save();
            Changed?.Invoke();
        }

        /// <summary>저장된 설정을 지우고 기본값으로 되돌린다.</summary>
        public static void ResetToDefaults()
        {
            PlayerPrefs.DeleteKey(MusicVolumeKey);
            PlayerPrefs.DeleteKey(SfxVolumeKey);
            PlayerPrefs.Save();
            Changed?.Invoke();
        }
    }
}
