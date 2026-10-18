using System;
using System.Collections;
using Bastion.Audio;
using Bastion.Combat;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Bastion.Core
{
    /// <summary>
    /// 스테이지 흐름: 체크포인트에서 시작, 플레이어 사망 시 재시작, 보스 처치 시 클리어.
    /// </summary>
    public sealed class GameFlow : MonoBehaviour
    {
        [SerializeField] Health player;
        [SerializeField] Health boss;
        [SerializeField, Min(0f)] float respawnDelay = 2.5f;
        [SerializeField, Min(0f)] float victoryDelay = 2f;
        [Tooltip("클리어 문구가 뜬 뒤 이동할 씬 (비워 두면 이동하지 않음)")]
        [SerializeField] string sceneAfterClear;
        [SerializeField, Min(0f)] float sceneAfterClearDelay = 4f;

        public static event Action PlayerDied;
        public static event Action StageCleared;

        void Start()
        {
            if (CheckpointSystem.HasCheckpoint && CheckpointSystem.SceneName == gameObject.scene.name)
                player.transform.position = CheckpointSystem.Position;
        }

        void OnEnable()
        {
            player.Died += OnPlayerDied;
            if (boss != null)
                boss.Died += OnBossDied;
        }

        void OnDisable()
        {
            player.Died -= OnPlayerDied;
            if (boss != null)
                boss.Died -= OnBossDied;
        }

        void OnPlayerDied()
        {
            PlayerDied?.Invoke();
            StartCoroutine(ReloadAfterDelay());
        }

        void OnBossDied() => StartCoroutine(ClearAfterDelay());

        IEnumerator ReloadAfterDelay()
        {
            yield return new WaitForSecondsRealtime(respawnDelay);
            SceneManager.LoadScene(gameObject.scene.name);
        }

        IEnumerator ClearAfterDelay()
        {
            SoundManager.StopMusic(victoryDelay);
            yield return new WaitForSecondsRealtime(victoryDelay);
            StageCleared?.Invoke();

            if (string.IsNullOrEmpty(sceneAfterClear))
                yield break;
            yield return new WaitForSecondsRealtime(sceneAfterClearDelay);
            CheckpointSystem.Clear();
            SceneManager.LoadScene(sceneAfterClear);
        }
    }
}
