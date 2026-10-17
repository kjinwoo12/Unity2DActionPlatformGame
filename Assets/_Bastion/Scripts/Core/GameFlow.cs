using System;
using System.Collections;
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
            yield return new WaitForSecondsRealtime(victoryDelay);
            StageCleared?.Invoke();
        }
    }
}
