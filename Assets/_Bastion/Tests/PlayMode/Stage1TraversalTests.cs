using System.Collections;
using Bastion.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bastion.Tests
{
    /// <summary>
    /// 레벨 회귀 테스트: 오른쪽을 누른 채 주기적으로 점프만 해도 시작 지점에서 마지막 구간까지 갈 수 있어야 한다.
    /// </summary>
    public sealed class Stage1TraversalTests : BastionInputTestFixture
    {
        const string ScenePath = "Assets/_Bastion/Scenes/Stage1.unity";
        const float FinalSectionX = 60f;

        Keyboard keyboard;

        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();
        }

        [UnityTest]
        public IEnumerator HoldRightAndJump_ReachesFinalSection()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Editor only");
            yield break;
#endif
            var player = Object.FindAnyObjectByType<PlayerController>();
            Assert.IsNotNull(player);

            // 이동 경로만 검증하므로 적과 함정은 제거한다.
            foreach (var name in new[] { "Enemies", "Traps" })
            {
                var go = GameObject.Find(name);
                if (go != null) Object.Destroy(go);
            }

            HoldKeys(keyboard, Key.RightArrow);
            float timeout = Time.time + 20f;
            float nextJump = Time.time + 0.5f;
            bool jumpHeld = false;

            while (player.transform.position.x < FinalSectionX && Time.time < timeout)
            {
                // 0.6초 누르고 0.3초 떼는 점프 반복
                if (Time.time >= nextJump)
                {
                    if (jumpHeld) HoldKeys(keyboard, Key.RightArrow);
                    else HoldKeys(keyboard, Key.RightArrow, Key.UpArrow);
                    nextJump = Time.time + (jumpHeld ? 0.3f : 0.6f);
                    jumpHeld = !jumpHeld;
                }
                yield return new WaitForFixedUpdate();
            }
            HoldKeys(keyboard);

            Assert.GreaterOrEqual(player.transform.position.x, FinalSectionX,
                $"Player got stuck at {player.transform.position} state={player.StateMachine.CurrentState.GetType().Name} v={player.Velocity} grounded={player.IsGrounded} hp={player.Health.Current} t={Time.time:F1}");
        }

        [UnityTest]
        public IEnumerator Player_StandsStillOnRamp()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            Assert.Ignore("Editor only");
            yield break;
#endif
            foreach (var name in new[] { "Enemies", "Traps" })
            {
                var go = GameObject.Find(name);
                if (go != null) Object.Destroy(go);
            }
            var player = Object.FindAnyObjectByType<PlayerController>();
            // 경사로(x 53~57) 중간에 내려놓고 입력 없이 기다린다.
            player.transform.position = new Vector3(55f, 5f, 0f);
            for (int i = 0; i < 40; i++)
                yield return new WaitForFixedUpdate();
            Assert.IsTrue(player.IsGrounded);
            float x = player.transform.position.x;

            for (int i = 0; i < 50; i++)
                yield return new WaitForFixedUpdate();

            Assert.AreEqual(x, player.transform.position.x, 0.02f, "Player should not slide down the ramp");
        }
    }
}
