using UnityEngine;

namespace Bastion.Core
{
    /// <summary>
    /// 마지막으로 활성화한 체크포인트를 씬 재시작 사이에 기억한다.
    /// 도메인 리로드를 끈 에디터 설정에서도 플레이마다 초기화되도록 SubsystemRegistration에서 리셋한다.
    /// </summary>
    public static class CheckpointSystem
    {
        public static bool HasCheckpoint { get; private set; }
        public static string SceneName { get; private set; }
        public static Vector3 Position { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnPlay() => Clear();

        public static void Activate(string sceneName, Vector3 position)
        {
            HasCheckpoint = true;
            SceneName = sceneName;
            Position = position;
        }

        public static void Clear()
        {
            HasCheckpoint = false;
            SceneName = null;
            Position = Vector3.zero;
        }
    }
}
