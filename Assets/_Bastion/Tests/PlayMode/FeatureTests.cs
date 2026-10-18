using System.Collections;
using Bastion.Combat;
using Bastion.Core;
using Bastion.Dialogue;
using Bastion.Items;
using Bastion.Player;
using Bastion.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bastion.Tests
{
    /// <summary>회복 아이템, 대화, 일시정지, 설정 저장.</summary>
    public sealed class FeatureTests : BastionInputTestFixture
    {
        const string StagePath = "Assets/_Bastion/Scenes/Stage1.unity";
        Keyboard keyboard;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        public override void TearDown()
        {
            Time.timeScale = 1f;
            base.TearDown();
        }

        static IEnumerator LoadStage()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                StagePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Editor only");
            yield break;
#endif
            // 검증과 관계없는 적과 함정은 치운다.
            foreach (var name in new[] { "Enemies", "Traps" })
            {
                var go = GameObject.Find(name);
                if (go != null) Object.Destroy(go);
            }
            yield return null;
        }

        static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++)
                yield return null;
        }

        [Test]
        public void Health_HealIsCappedAtMax()
        {
            var go = new GameObject("HealTarget");
            var health = go.AddComponent<Health>();
            health.TakeDamage(new DamageInfo(30, Vector2.right, 0f, null));
            Assert.IsTrue(health.Heal(100));
            Assert.AreEqual(health.Max, health.Current);
            Assert.IsFalse(health.Heal(10), "가득 찬 상태에서는 회복하지 않는다");
            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator KillZone_KillsFallingTargetEvenIfInvincible()
        {
            var zone = new GameObject("KillZone");
            zone.transform.position = new Vector3(0f, -50f, 0f);
            zone.AddComponent<BoxCollider2D>().size = new Vector2(20f, 2f);
            zone.AddComponent<Bastion.Hazards.KillZone>();

            var faller = new GameObject("Faller");
            faller.transform.position = new Vector3(0f, -46f, 0f);
            faller.AddComponent<Rigidbody2D>();
            faller.AddComponent<BoxCollider2D>();
            var health = faller.AddComponent<Health>();
            health.InvincibleUntil = float.PositiveInfinity;
            bool died = false;
            health.Died += () => died = true;

            for (float t = 0f; t < 2f && !health.IsDead; t += Time.deltaTime)
                yield return null;

            Assert.IsTrue(health.IsDead, "떨어지면 무적이어도 사망한다");
            Assert.IsTrue(died);
            Object.Destroy(zone);
            Object.Destroy(faller);
        }

        [UnityTest]
        public IEnumerator HealthPickup_HealsInjuredPlayerAndDisappears()
        {
            yield return LoadStage();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var potion = Object.FindAnyObjectByType<HealthPickup>();
            Assert.IsNotNull(potion);

            player.Health.TakeDamage(new DamageInfo(40, Vector2.right, 0f, null));
            player.Health.InvincibleUntil = float.NegativeInfinity;
            int before = player.Health.Current;
            player.transform.position = potion.transform.position;
            yield return new WaitForSeconds(0.2f);

            Assert.Greater(player.Health.Current, before);
            Assert.IsTrue(potion == null, "먹은 물약은 사라진다");
        }

        [UnityTest]
        public IEnumerator HealthPickup_StaysWhenHealthIsFull()
        {
            yield return LoadStage();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var potion = Object.FindAnyObjectByType<HealthPickup>();
            player.transform.position = potion.transform.position;
            yield return new WaitForSeconds(0.2f);
            Assert.IsTrue(potion != null);
        }

        [UnityTest]
        public IEnumerator Dialogue_OpensWithInteractAndClosesAfterLastLine()
        {
            yield return LoadStage();
            var player = Object.FindAnyObjectByType<PlayerController>();
            var npc = Object.FindAnyObjectByType<DialogueNpc>();
            player.transform.position = npc.transform.position;
            yield return new WaitForSeconds(0.3f);

            PressAndRelease(keyboard.downArrowKey);
            yield return Frames(3);
            Assert.IsTrue(DialogueBox.IsOpen, "↓ 키로 대화를 연다");
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsFalse(player.Input.enabled, "대화 중에는 플레이어 입력을 끈다");

            // Z를 반복해서 누르면 (글자 모두 보이기 → 다음 문장) 끝까지 넘어가고 닫힌다.
            for (int i = 0; i < 20 && DialogueBox.IsOpen; i++)
            {
                PressAndRelease(keyboard.zKey);
                yield return Frames(2);
            }
            Assert.IsFalse(DialogueBox.IsOpen);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(player.Input.enabled);
        }

        [UnityTest]
        public IEnumerator Pause_EscapeTogglesTimeScale()
        {
            yield return LoadStage();
            var pause = Object.FindAnyObjectByType<PauseMenu>();
            yield return Frames(2);

            PressAndRelease(keyboard.escapeKey);
            yield return Frames(2);
            Assert.IsTrue(pause.IsPaused);
            Assert.AreEqual(0f, Time.timeScale);

            PressAndRelease(keyboard.escapeKey);
            yield return Frames(2);
            Assert.IsFalse(pause.IsPaused);
            Assert.AreEqual(1f, Time.timeScale);
        }

        [Test]
        public void GameSettings_SavesVolume()
        {
            float music = GameSettings.MusicVolume;
            GameSettings.MusicVolume = 0.25f;
            Assert.AreEqual(0.25f, GameSettings.MusicVolume, 0.0001f);
            GameSettings.MusicVolume = 2f;
            Assert.AreEqual(1f, GameSettings.MusicVolume, 0.0001f, "0~1로 제한한다");
            GameSettings.MusicVolume = music;
        }
    }
}
