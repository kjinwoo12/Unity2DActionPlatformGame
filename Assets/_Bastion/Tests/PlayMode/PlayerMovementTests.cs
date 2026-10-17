using System.Collections;
using Bastion.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TestTools;

namespace Bastion.Tests
{
    public sealed class PlayerMovementTests : BastionInputTestFixture
    {
        const string PlayerPrefabPath = "Assets/_Bastion/Prefabs/Player.prefab";

        Keyboard keyboard;
        GameObject floor;
        PlayerController player;
        PlayerMovementData data;

        // InputTestFixture.Setup이 입력 시스템을 초기화하므로, 플레이어(입력 액션 활성화)는 반드시 그 뒤에 생성한다.
        // [UnitySetUp]은 [SetUp]보다 먼저 실행되므로 사용하지 않는다.
        public override void Setup()
        {
            base.Setup();
            keyboard = InputSystem.AddDevice<Keyboard>();

            floor = new GameObject("TestFloor") { layer = LayerMask.NameToLayer("Ground") };
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(20f, 1f);

#if UNITY_EDITOR
            var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
#else
            GameObject prefab = null;
#endif
            Assert.IsNotNull(prefab, "Player prefab not found");
            player = Object.Instantiate(prefab, Vector3.zero, Quaternion.identity).GetComponent<PlayerController>();
            data = player.Data;
        }

        public override void TearDown()
        {
            Object.DestroyImmediate(player.gameObject);
            Object.DestroyImmediate(floor);
            base.TearDown();
        }

        IEnumerator Settle()
        {
            yield return WaitFixed(5);
            Assert.IsTrue(player.IsGrounded, "Player should start grounded");
        }

        static IEnumerator WaitFixed(int steps)
        {
            for (int i = 0; i < steps; i++)
                yield return new WaitForFixedUpdate();
        }

        static int Steps(float seconds) => Mathf.CeilToInt(seconds / Time.fixedDeltaTime);

        IEnumerator TrackMaxHeight(float seconds, System.Action<float> result)
        {
            float maxY = player.transform.position.y;
            for (int i = 0; i < Steps(seconds); i++)
            {
                yield return new WaitForFixedUpdate();
                maxY = Mathf.Max(maxY, player.transform.position.y);
            }
            result(maxY);
        }

        [UnityTest]
        public IEnumerator Run_ReachesMaxRunSpeed()
        {
            yield return Settle();
            Press(keyboard.rightArrowKey);
            yield return WaitFixed(Steps(0.4f));

            Assert.AreEqual(data.MaxRunSpeed, player.Velocity.x, 0.01f);
            Assert.AreEqual(1, player.FacingDirection);
            Release(keyboard.rightArrowKey);
        }

        [UnityTest]
        public IEnumerator Run_FacesLeftWhenMovingLeft()
        {
            yield return Settle();
            Press(keyboard.leftArrowKey);
            yield return WaitFixed(3);

            Assert.AreEqual(-1, player.FacingDirection);
            Release(keyboard.leftArrowKey);
        }

        [UnityTest]
        public IEnumerator Jump_HeldReachesConfiguredHeight()
        {
            yield return Settle();
            Press(keyboard.upArrowKey);
            float maxY = 0f;
            yield return TrackMaxHeight(1f, y => maxY = y);
            Release(keyboard.upArrowKey);

            Assert.AreEqual(data.JumpHeight, maxY, 0.3f);
        }

        [UnityTest]
        public IEnumerator Jump_ReleasedEarlyIsLower()
        {
            yield return Settle();
            Press(keyboard.upArrowKey);
            yield return WaitFixed(3);
            Release(keyboard.upArrowKey);

            float maxY = 0f;
            yield return TrackMaxHeight(1f, y => maxY = y);

            Assert.Less(maxY, data.JumpHeight * 0.6f);
            Assert.Greater(maxY, 0.2f);
        }

        [UnityTest]
        public IEnumerator Jump_AllowedDuringCoyoteTime()
        {
            yield return Settle();
            // 발판에서 막 떨어진 상황
            player.transform.position = new Vector3(0f, 3f, 0f);
            yield return WaitFixed(1);
            Assert.IsFalse(player.IsGrounded);

            PressAndRelease(keyboard.upArrowKey);
            yield return WaitFixed(2);

            Assert.Greater(player.Velocity.y, 0f);
        }

        [UnityTest]
        public IEnumerator Jump_AfterCoyoteTimeUsesAirJump()
        {
            yield return Settle();
            player.transform.position = new Vector3(0f, 8f, 0f);
            yield return WaitFixed(Steps(data.CoyoteTime + 0.1f));

            // 코요테 시간이 지나면 지상 점프 대신 공중 점프를 쓴다.
            PressAndRelease(keyboard.upArrowKey);
            yield return WaitFixed(2);
            Assert.Greater(player.Velocity.y, 0f);
            Assert.AreEqual(0, player.AirJumpsRemaining);

            // 공중 점프를 다 썼으면 더는 뛸 수 없다.
            yield return WaitFixed(Steps(0.5f));
            PressAndRelease(keyboard.upArrowKey);
            yield return WaitFixed(2);
            Assert.LessOrEqual(player.Velocity.y, 0f);
        }

        [UnityTest]
        public IEnumerator DoubleJump_ReachesHigherThanSingleJump()
        {
            yield return Settle();
            Press(keyboard.upArrowKey);
            yield return WaitFixed(Steps(data.TimeToJumpApex));
            Release(keyboard.upArrowKey);
            yield return WaitFixed(1);
            Press(keyboard.upArrowKey);

            float maxY = 0f;
            yield return TrackMaxHeight(1.2f, y => maxY = y);
            Release(keyboard.upArrowKey);

            Assert.Greater(maxY, data.JumpHeight + data.AirJumpHeight * 0.8f);
        }

        [UnityTest]
        public IEnumerator DoubleJump_RefreshesOnLanding()
        {
            yield return Settle();
            PressAndRelease(keyboard.upArrowKey);
            yield return WaitFixed(Steps(0.3f));
            PressAndRelease(keyboard.upArrowKey);
            yield return WaitFixed(2);
            Assert.AreEqual(0, player.AirJumpsRemaining);

            yield return WaitFixed(Steps(2f));
            Assert.IsTrue(player.IsGrounded);
            Assert.AreEqual(data.MaxAirJumps, player.AirJumpsRemaining);
        }

        [UnityTest]
        public IEnumerator Roll_InAirIsAffectedByGravity()
        {
            yield return Settle();
            player.transform.position = new Vector3(0f, 8f, 0f);
            yield return WaitFixed(1);
            float startY = player.transform.position.y;

            PressAndRelease(keyboard.xKey);
            yield return WaitFixed(Steps(data.DashDuration * 0.8f));

            Assert.AreSame(player.DashState, player.StateMachine.CurrentState);
            Assert.Less(player.Velocity.y, 0f);
            Assert.Less(player.transform.position.y, startY - 0.3f);
        }

        [UnityTest]
        public IEnumerator Jump_BufferedBeforeLandingTriggersOnLanding()
        {
            yield return Settle();
            player.transform.position = new Vector3(0f, 0.2f, 0f);
            yield return WaitFixed(Steps(data.CoyoteTime + 0.02f));
            player.transform.position = new Vector3(0f, 0.2f, 0f);
            player.SetVelocity(Vector2.zero);
            yield return WaitFixed(1);

            Press(keyboard.upArrowKey);
            float maxY = 0f;
            yield return TrackMaxHeight(0.6f, y => maxY = y);
            Release(keyboard.upArrowKey);

            Assert.Greater(maxY, 1f);
        }

        [UnityTest]
        public IEnumerator Dash_MovesForwardAndGrantsInvincibility()
        {
            yield return Settle();
            float startX = player.transform.position.x;
            PressAndRelease(keyboard.xKey);
            yield return WaitFixed(1);

            Assert.IsTrue(player.IsInvincible);
            Assert.AreSame(player.DashState, player.StateMachine.CurrentState);

            yield return WaitFixed(Steps(data.DashDuration) + 1);

            Assert.AreNotSame(player.DashState, player.StateMachine.CurrentState);
            float expected = data.DashSpeed * data.DashDuration;
            Assert.AreEqual(expected, player.transform.position.x - startX, expected * 0.25f);
        }

        [UnityTest]
        public IEnumerator Dash_OnlyOncePerAirtime()
        {
            yield return Settle();
            player.transform.position = new Vector3(0f, 40f, 0f); // 구르기 중에도 떨어지므로 충분히 높은 곳에서 시작
            yield return WaitFixed(1);

            PressAndRelease(keyboard.xKey);
            yield return WaitFixed(Steps(data.DashDuration + data.DashCooldown) + 2);
            Assert.IsFalse(player.CanAirDash);

            PressAndRelease(keyboard.xKey);
            yield return WaitFixed(1);

            Assert.AreNotSame(player.DashState, player.StateMachine.CurrentState);
        }
    }
}
