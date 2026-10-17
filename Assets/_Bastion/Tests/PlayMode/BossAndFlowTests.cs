using System.Collections;
using Bastion.Boss;
using Bastion.Combat;
using Bastion.Core;
using Bastion.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bastion.Tests
{
    /// <summary>Stage1 씬을 그대로 불러와 보스전과 게임 흐름(체크포인트, 사망, 클리어)을 검증한다.</summary>
    public sealed class BossAndFlowTests : BastionInputTestFixture
    {
        const string ScenePath = "Assets/_Bastion/Scenes/Stage1.unity";

        PlayerController player;
        BossController boss;

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            CheckpointSystem.Clear();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            CheckpointSystem.Clear();
            base.TearDown();
        }

        IEnumerator LoadStage()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Editor only");
#endif
            yield return null;
            foreach (var name in new[] { "Enemies", "Traps" })
            {
                var go = GameObject.Find(name);
                if (go != null) Object.Destroy(go);
            }
            player = Object.FindAnyObjectByType<PlayerController>();
            boss = Object.FindAnyObjectByType<BossController>();
            Assert.IsNotNull(player);
            Assert.IsNotNull(boss);
        }

        static IEnumerator WaitSeconds(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return new WaitForFixedUpdate();
        }

        IEnumerator EnterArena()
        {
            player.transform.position = new Vector3(70f, 6f, 0f);
            yield return WaitSeconds(0.3f);
        }

        [UnityTest]
        public IEnumerator Boss_IsInvincibleUntilArenaEntered()
        {
            yield return LoadStage();
            Assert.IsFalse(boss.IsActive);
            Assert.IsFalse(boss.Health.TakeDamage(new DamageInfo(50, Vector2.right, 0f, null)));

            var gate = GameObject.Find("BossArena/Gate");
            Assert.IsNull(gate, "Gate should start inactive");

            yield return EnterArena();

            Assert.IsTrue(boss.IsActive);
            Assert.IsNotNull(GameObject.Find("BossArena/Gate"), "Gate should close");
            Assert.IsTrue(boss.Health.TakeDamage(new DamageInfo(50, Vector2.right, 0f, null)));
        }

        [UnityTest]
        public IEnumerator Boss_EntersPhaseTwoAtHalfHealth()
        {
            yield return LoadStage();
            yield return EnterArena();

            int phaseEvents = 0;
            boss.PhaseChanged += _ => phaseEvents++;
            boss.Health.TakeDamage(new DamageInfo(boss.Health.Max / 2, Vector2.right, 0f, null));
            yield return null;

            Assert.AreEqual(2, boss.Phase);
            Assert.AreEqual(1, phaseEvents);
            Assert.IsTrue(boss.Health.IsInvincible, "Phase transition should be invulnerable");
        }

        [UnityTest]
        public IEnumerator Boss_AttacksPlayerInArena()
        {
            yield return LoadStage();
            yield return EnterArena();
            int before = player.Health.Current;

            yield return WaitSeconds(8f);

            Assert.Less(player.Health.Current, before, "Boss should land at least one attack in 8 seconds");
        }

        [UnityTest]
        public IEnumerator Boss_TelegraphsBeforeAttacking()
        {
            yield return LoadStage();
            yield return EnterArena();
            var telegraph = boss.GetComponentInChildren<AttackTelegraph>();
            Assert.IsNotNull(telegraph);

            float end = Time.time + 8f;
            while (telegraph.PlayCount == 0 && Time.time < end)
                yield return new WaitForFixedUpdate();

            Assert.Greater(telegraph.PlayCount, 0);
            int hpAtTelegraph = player.Health.Current;
            // 예비 동작 동안은 피해가 들어가지 않는다.
            yield return WaitSeconds(boss.Data.TelegraphTime * 0.9f);
            Assert.AreEqual(hpAtTelegraph, player.Health.Current);
        }

        [UnityTest]
        public IEnumerator Boss_SummonsMinionsInPhaseTwo()
        {
            yield return LoadStage();
            yield return EnterArena();
            boss.Health.TakeDamage(new DamageInfo(boss.Health.Max / 2 + 1, Vector2.right, 0f, null));

            // 페이즈 전환 직후 소환 패턴을 사용한다.
            yield return WaitSeconds(boss.Data.PhaseTransitionTime + boss.Data.SummonSpawnTime + 0.3f);

            Assert.GreaterOrEqual(Object.FindObjectsByType<SummonMinion>(FindObjectsSortMode.None).Length, 1);
        }

        [UnityTest]
        public IEnumerator BossDeath_ClearsStage()
        {
            yield return LoadStage();
            yield return EnterArena();
            bool cleared = false;
            System.Action onClear = () => cleared = true;
            GameFlow.StageCleared += onClear;
            try
            {
                boss.Health.TakeDamage(new DamageInfo(boss.Health.Max / 2, Vector2.right, 0f, null));
                yield return WaitSeconds(boss.Data.PhaseTransitionTime + 0.1f);
                boss.Health.TakeDamage(new DamageInfo(boss.Health.Max, Vector2.right, 0f, null));
                Assert.IsTrue(boss.Health.IsDead);

                yield return new WaitForSecondsRealtime(2.5f);
                Assert.IsTrue(cleared);
            }
            finally
            {
                GameFlow.StageCleared -= onClear;
            }
        }

        [UnityTest]
        public IEnumerator Checkpoint_ActivatesAndHeals()
        {
            yield return LoadStage();
            player.Health.TakeDamage(new DamageInfo(30, Vector2.left, 0f, null));
            yield return WaitSeconds(1f); // 피격 무적 해제 대기

            var checkpoint = GameObject.Find("Checkpoints").transform.GetChild(1).GetComponent<Checkpoint>();
            player.transform.position = checkpoint.SpawnPosition + Vector3.up * 0.5f;
            yield return WaitSeconds(0.3f);

            Assert.IsTrue(checkpoint.IsActive);
            Assert.IsTrue(CheckpointSystem.HasCheckpoint);
            Assert.AreEqual(player.Health.Max, player.Health.Current);
        }

        [UnityTest]
        public IEnumerator PlayerDeath_RespawnsAtCheckpoint()
        {
            yield return LoadStage();
            var checkpoint = GameObject.Find("Checkpoints").transform.GetChild(1).GetComponent<Checkpoint>();
            Vector3 spawnPosition = checkpoint.SpawnPosition; // 재시작 후에는 이 오브젝트가 파괴되므로 미리 저장
            player.transform.position = spawnPosition + Vector3.up * 0.5f;
            yield return WaitSeconds(0.3f);

            player.Health.TakeDamage(new DamageInfo(player.Health.Max, Vector2.left, 0f, null));
            // 재시작 대기 후 새 씬의 플레이어 확인
            yield return new WaitForSecondsRealtime(3.5f);
            yield return null;

            var respawned = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsFalse(respawned.Health.IsDead);
            Assert.AreEqual(spawnPosition.x, respawned.transform.position.x, 0.5f);
        }
    }
}
