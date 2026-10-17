using UnityEngine;

namespace Bastion.Combat
{
    /// <summary>
    /// 타격 순간 게임 시간을 잠깐 멈춰 타격감을 만든다. 겹친 요청은 가장 늦게 끝나는 시점까지 유지한다.
    /// </summary>
    public sealed class HitStop : MonoBehaviour
    {
        static HitStop instance;

        float resumeAt = float.NegativeInfinity;
        float savedTimeScale = 1f;
        bool stopped;

        public static bool IsStopped => instance != null && instance.stopped;

        public static void Request(float duration)
        {
            if (duration <= 0f)
                return;

            if (instance == null)
            {
                var go = new GameObject(nameof(HitStop));
                DontDestroyOnLoad(go);
                instance = go.AddComponent<HitStop>();
            }
            instance.Begin(duration);
        }

        void Begin(float duration)
        {
            if (!stopped)
            {
                savedTimeScale = Time.timeScale;
                stopped = true;
            }
            resumeAt = Mathf.Max(resumeAt, Time.unscaledTime + duration);
            Time.timeScale = 0f;
        }

        void Update()
        {
            if (stopped && Time.unscaledTime >= resumeAt)
                Resume();
        }

        void Resume()
        {
            stopped = false;
            Time.timeScale = savedTimeScale;
        }

        void OnDestroy()
        {
            if (stopped)
                Resume();
            if (instance == this)
                instance = null;
        }
    }
}
