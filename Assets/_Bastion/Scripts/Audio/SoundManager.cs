using System.Collections;
using Bastion.Core;
using UnityEngine;

namespace Bastion.Audio
{
    /// <summary>
    /// 씬마다 하나 두는 소리 담당. 배경음(반복 재생)과 효과음(한 번 재생)을 각각의 AudioSource로 낸다.
    /// 다른 스크립트는 SoundManager.PlaySfx(clip)처럼 정적 함수로 부른다.
    /// </summary>
    public sealed class SoundManager : MonoBehaviour
    {
        [SerializeField] AudioSource musicSource;
        [SerializeField] AudioSource sfxSource;
        [Tooltip("씬이 시작될 때 재생할 배경음 (비워 두면 재생하지 않음)")]
        [SerializeField] AudioClip startMusic;
        [Tooltip("같은 효과음을 반복해도 덜 단조롭게 들리도록 주는 음높이 흔들림")]
        [SerializeField, Range(0f, 0.3f)] float pitchVariation = 0.06f;

        static SoundManager instance;
        float musicFade = 1f;
        Coroutine fadeRoutine;

        void Awake()
        {
            instance = this;
            musicSource.loop = true;
            musicSource.playOnAwake = false;
            sfxSource.playOnAwake = false;
            ApplyVolume();
        }

        void OnEnable() => GameSettings.Changed += ApplyVolume;

        void OnDisable() => GameSettings.Changed -= ApplyVolume;

        void OnDestroy()
        {
            if (instance == this)
                instance = null;
        }

        void Start()
        {
            if (startMusic != null)
                PlayMusic(startMusic, 0f);
        }

        void ApplyVolume()
        {
            musicSource.volume = GameSettings.MusicVolume * musicFade;
            sfxSource.volume = GameSettings.SfxVolume;
        }

        /// <summary>효과음을 한 번 재생한다. 볼륨 배율(0~1)을 줄 수 있다.</summary>
        public static void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (instance == null || clip == null)
                return;
            var source = instance.sfxSource;
            source.pitch = 1f + Random.Range(-instance.pitchVariation, instance.pitchVariation);
            source.PlayOneShot(clip, volumeScale);
        }

        /// <summary>배경음을 바꾼다. fadeTime 동안 이전 곡을 줄이고 새 곡을 키운다.</summary>
        public static void PlayMusic(AudioClip clip, float fadeTime = 1f)
        {
            if (instance == null || clip == null || instance.musicSource.clip == clip && instance.musicSource.isPlaying)
                return;
            instance.StartFade(clip, fadeTime);
        }

        /// <summary>배경음을 서서히 멈춘다.</summary>
        public static void StopMusic(float fadeTime = 1f)
        {
            if (instance != null)
                instance.StartFade(null, fadeTime);
        }

        void StartFade(AudioClip next, float fadeTime)
        {
            if (fadeRoutine != null)
                StopCoroutine(fadeRoutine);
            fadeRoutine = StartCoroutine(Fade(next, fadeTime));
        }

        // 일시정지(Time.timeScale = 0) 중에도 진행되도록 실제 시간 기준으로 줄이고 키운다.
        IEnumerator Fade(AudioClip next, float fadeTime)
        {
            if (musicSource.isPlaying)
            {
                for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
                {
                    musicFade = 1f - t / fadeTime;
                    ApplyVolume();
                    yield return null;
                }
            }

            musicSource.Stop();
            musicSource.clip = next;
            if (next == null)
                yield break;

            musicSource.Play();
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                musicFade = t / fadeTime;
                ApplyVolume();
                yield return null;
            }
            musicFade = 1f;
            ApplyVolume();
            fadeRoutine = null;
        }
    }
}
