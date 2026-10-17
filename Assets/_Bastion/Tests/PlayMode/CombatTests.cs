using System.Collections;
using Bastion.Combat;
using Bastion.Enemy;
using Bastion.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Bastion.Tests
{
    public sealed class CombatTests : BastionInputTestFixture
    {
        Keyboard keyboard;
        GameObject floor;
        PlayerController player;
        readonly System.Collections.Generic.List<GameObject> spawned = new System.Collections.Generic.List<GameObject>();

        static GameObject LoadPrefab(string name)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Bastion/Prefabs/{name}.prefab");
#else
            return null;
#endif
        }

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();

            floor = new GameObject("TestFloor") { layer = LayerMask.NameToLayer("Ground") };
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);

            player = Object.Instantiate(LoadPrefab("Player"), Vector3.zero, Quaternion.identity).GetComponent<PlayerController>();
        }

        public override void TearDown()
        {
            foreach (var go in spawned)
                if (go != null) Object.DestroyImmediate(go);
            spawned.Clear();
            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(floor);
            Time.timeScale = 1f;
            base.TearDown();
        }

        T Spawn<T>(string prefab, Vector3 position) where T : Component
        {
            var go = Object.Instantiate(LoadPrefab(prefab), position, Quaternion.identity);
            spawned.Add(go);
            return go.GetComponent<T>();
        }

        static IEnumerator WaitFixed(int steps)
        {
            for (int i = 0; i < steps; i++)
                yield return new WaitForFixedUpdate();
        }

        static IEnumerator WaitSeconds(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
                yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator Attack_DamagesEnemyInFront()
        {
            var enemy = Spawn<EnemyController>("Skeleton", new Vector3(1.5f, 0f, 0f));
            enemy.Health.BlockFilter = null;
            enemy.enabled = false; // AI 없이 허수아비로 사용
            yield return WaitFixed(5);

            PressAndRelease(keyboard.zKey);
            yield return WaitSeconds(0.4f);

            Assert.AreEqual(enemy.Health.Max - player.CombatData.Combo[0].Damage, enemy.Health.Current);
        }

        [UnityTest]
        public IEnumerator Attack_DoesNotHitEnemyBehind()
        {
            var enemy = Spawn<EnemyController>("Skeleton", new Vector3(-1.5f, 0f, 0f));
            enemy.Health.BlockFilter = null;
            enemy.enabled = false;
            yield return WaitFixed(5);

            PressAndRelease(keyboard.zKey);
            yield return WaitSeconds(0.4f);

            Assert.AreEqual(enemy.Health.Max, enemy.Health.Current);
        }

        [UnityTest]
        public IEnumerator Attack_ThreeHitComboDealsAllSteps()
        {
            var enemy = Spawn<EnemyController>("Skeleton", new Vector3(1.6f, 0f, 0f));
            enemy.Health.BlockFilter = null;
            enemy.enabled = false;
            yield return WaitFixed(5);

            int expected = 0;
            for (int i = 0; i < 3; i++)
            {
                PressAndRelease(keyboard.zKey);
                expected += player.CombatData.Combo[i].Damage;
                // 캔슬 시점 직후 다음 입력
                yield return WaitSeconds(player.CombatData.Combo[i].CancelTime + 0.05f);
            }
            yield return WaitSeconds(0.6f);

            Assert.AreEqual(enemy.Health.Max - expected, enemy.Health.Current);
        }

        [UnityTest]
        public IEnumerator Attack_HitStopRestoresTimeScale()
        {
            var enemy = Spawn<EnemyController>("Skeleton", new Vector3(1.5f, 0f, 0f));
            enemy.Health.BlockFilter = null;
            enemy.enabled = false;
            yield return WaitFixed(5);

            bool sawStop = false;
            player.AttackLanded += (_, __) => sawStop = true;
            PressAndRelease(keyboard.zKey);
            yield return WaitSeconds(0.3f);
            yield return new WaitForSecondsRealtime(0.2f);

            Assert.IsTrue(sawStop);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [UnityTest]
        public IEnumerator Shield_BlocksFrontalAttack()
        {
            var enemy = Spawn<EnemyController>("Skeleton", new Vector3(1.5f, 0f, 0f));
            enemy.enabled = false;
            enemy.Face(-1); // 플레이어를 바라봄
            enemy.Health.BlockFilter = info => Mathf.Sign(info.Direction.x) == -enemy.FacingDirection;
            yield return WaitFixed(5);

            PressAndRelease(keyboard.zKey);
            yield return WaitSeconds(0.4f);

            Assert.AreEqual(enemy.Health.Max, enemy.Health.Current);
        }

        [UnityTest]
        public IEnumerator Skeleton_ChasesAndHitsPlayer()
        {
            var enemy = Spawn<EnemyController>("Skeleton", new Vector3(5f, 0f, 0f));
            int before = player.Health.Current;

            yield return WaitSeconds(3f);

            Assert.Less(player.Health.Current, before);
        }

        [UnityTest]
        public IEnumerator Skeleton_TelegraphsBeforeHit()
        {
            var enemy = Spawn<EnemyController>("Skeleton", new Vector3(5f, 0f, 0f));
            var telegraph = enemy.GetComponentInChildren<AttackTelegraph>();
            Assert.IsNotNull(telegraph);

            float end = Time.time + 4f;
            while (telegraph.PlayCount == 0 && Time.time < end)
                yield return new WaitForFixedUpdate();

            Assert.Greater(telegraph.PlayCount, 0, "Telegraph should play when attacking");
            Assert.IsTrue(telegraph.IsPlaying);
            Assert.AreEqual(player.Health.Max, player.Health.Current, "No damage while telegraphing");

            yield return WaitSeconds(enemy.Data.Attack.ActiveEnd);
            Assert.Less(player.Health.Current, player.Health.Max, "Hit lands after the telegraph");
        }

        [UnityTest]
        public IEnumerator Dash_InvincibilityAvoidsDamage()
        {
            yield return WaitFixed(5);
            PressAndRelease(keyboard.xKey);
            yield return WaitFixed(1);

            bool damaged = player.Health.TakeDamage(new DamageInfo(10, Vector2.left, 3f, null));

            Assert.IsFalse(damaged);
            Assert.AreEqual(player.Health.Max, player.Health.Current);
        }

        [UnityTest]
        public IEnumerator Wizard_FireballHitsPlayer()
        {
            var wizard = Spawn<EnemyController>("Wizard", new Vector3(7f, 0f, 0f));
            int before = player.Health.Current;

            yield return WaitSeconds(3f);

            Assert.Less(player.Health.Current, before);
        }

        [UnityTest]
        public IEnumerator Enemy_DiesAndIsRemoved()
        {
            var enemy = Spawn<EnemyController>("Skeleton", new Vector3(10f, 0f, 0f));
            enemy.Health.BlockFilter = null;
            yield return WaitFixed(5);

            enemy.Health.TakeDamage(new DamageInfo(enemy.Health.Max, Vector2.right, 0f, null));
            Assert.IsTrue(enemy.Health.IsDead);

            yield return WaitSeconds(enemy.Data.CorpseLifetime + 0.2f);
            Assert.IsTrue(enemy == null, "Corpse should be destroyed");
        }

        [UnityTest]
        public IEnumerator Player_DiesAtZeroHealth()
        {
            yield return WaitFixed(5);
            player.Health.TakeDamage(new DamageInfo(player.Health.Max, Vector2.left, 0f, null));
            yield return WaitFixed(2);

            Assert.AreSame(player.DeadState, player.StateMachine.CurrentState);
        }
    }
}
