using System.Collections;
using Bastion.Hazards;
using Bastion.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Bastion.Tests
{
    public sealed class TrapTests : BastionInputTestFixture
    {
        GameObject floor;
        PlayerController player;
        Trap trap;

        static GameObject LoadPrefab(string path)
        {
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
#else
            return null;
#endif
        }

        public override void Setup()
        {
            base.Setup();
            InputSystem.AddDevice<Keyboard>();
            floor = new GameObject("TestFloor") { layer = LayerMask.NameToLayer("Ground") };
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);
            player = Object.Instantiate(LoadPrefab("Assets/_Bastion/Prefabs/Player.prefab"), Vector3.zero, Quaternion.identity)
                .GetComponent<PlayerController>();
        }

        public override void TearDown()
        {
            if (trap != null) Object.DestroyImmediate(trap.gameObject);
            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(floor);
            base.TearDown();
        }

        void SpawnTrap(string name, Vector3 position, bool faceLeft = false)
        {
            trap = Object.Instantiate(LoadPrefab($"Assets/_Bastion/Prefabs/Traps/{name}.prefab"), position,
                faceLeft ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity).GetComponent<Trap>();
        }

        IEnumerator WaitForPhase(Trap.Phase phase, float timeout = 5f)
        {
            float end = Time.time + timeout;
            while (trap.CurrentPhase != phase && Time.time < end)
                yield return new WaitForFixedUpdate();
            Assert.AreEqual(phase, trap.CurrentPhase, $"Trap never reached {phase}");
        }

        [UnityTest]
        public IEnumerator FireTrap_NoDamageWhileIdleOrWarning()
        {
            SpawnTrap("FireTrap", Vector3.zero);
            yield return WaitForPhase(Trap.Phase.Warning);

            Assert.AreEqual(player.Health.Max, player.Health.Current);
        }

        [UnityTest]
        public IEnumerator FireTrap_HitsOncePerCycle()
        {
            SpawnTrap("FireTrap", Vector3.zero);
            yield return WaitForPhase(Trap.Phase.Active);
            int afterFirst = -1;
            // 피격 무적이 끝난 뒤에도 같은 작동 주기 안에서는 다시 맞지 않아야 한다.
            player.Health.InvincibleUntil = float.NegativeInfinity;
            yield return new WaitForFixedUpdate();
            afterFirst = player.Health.Current;
            player.Health.InvincibleUntil = float.NegativeInfinity;
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(afterFirst, player.Health.Current);
        }

        [UnityTest]
        public IEnumerator FireTrap_DamagesWhenActive()
        {
            SpawnTrap("FireTrap", Vector3.zero);
            yield return WaitForPhase(Trap.Phase.Active);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.Less(player.Health.Current, player.Health.Max);
        }

        [UnityTest]
        public IEnumerator PushTrap_PushesForward()
        {
            // 플레이어 오른쪽 벽에 붙어 왼쪽으로 밀어내는 배치
            SpawnTrap("PushTrap", new Vector3(1.2f, -0.1f, 0f), faceLeft: true);
            float startX = player.transform.position.x;
            yield return WaitForPhase(Trap.Phase.Active);
            for (int i = 0; i < 10; i++)
                yield return new WaitForFixedUpdate();

            Assert.Less(player.Health.Current, player.Health.Max);
            Assert.Less(player.transform.position.x, startX - 0.5f, "Player should be pushed left");
        }

    }
}
