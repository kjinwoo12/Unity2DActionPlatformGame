using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Bastion.Tests
{
    /// <summary>
    /// Bastion PlayMode 테스트 공통 픽스처.
    /// - 에디터 창이 포커스를 잃어도 테스트 입력이 리셋되지 않게 한다.
    /// - 여러 키를 동시에 누르는 상황은 <see cref="HoldKeys"/>로 키보드 전체 상태를 한 번에 보낸다.
    ///   (InputTestFixture.Press는 키 하나만 바꾸는데, 같은 바이트에 있는 키(예: →와 ↑)를 연달아 누르면
    ///   앞서 누른 키가 지워질 수 있다)
    /// </summary>
    public abstract class BastionInputTestFixture : InputTestFixture
    {
        public override void Setup()
        {
            base.Setup();
            // InputTestFixture가 설정을 테스트마다 격리하고 복원하므로 프로젝트 설정에는 영향이 없다.
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            Application.runInBackground = true;
        }

        /// <summary>
        /// 테스트가 불러온 씬(Stage1 등)이 다음 테스트로 새지 않도록 빈 씬으로 교체한다.
        /// (남아 있으면 원점 근처의 벽 등이 다른 테스트의 오브젝트를 밀어낸다)
        /// </summary>
        [UnityTearDown]
        public IEnumerator UnloadLoadedScenes()
        {
            var active = SceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(active.path))
                yield break; // 테스트 러너의 임시 씬이면 그대로 둔다
            var empty = SceneManager.CreateScene("TestEmpty_" + System.Guid.NewGuid().ToString("N"));
            SceneManager.SetActiveScene(empty);
            yield return SceneManager.UnloadSceneAsync(active);
        }

        /// <summary>지정한 키만 눌린 상태로 만든다 (나머지는 모두 뗌).</summary>
        protected static void HoldKeys(Keyboard keyboard, params Key[] keys)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys));
            InputSystem.Update();
        }
    }
}
