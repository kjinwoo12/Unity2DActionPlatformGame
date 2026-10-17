using System.Collections;
using UnityEngine;

namespace Bastion.Combat
{
    /// <summary>
    /// 공격 예고 연출: 공격 판정 직전까지 무기/몸 주변으로 입자가 모여든다.
    /// 플레이어가 회피 타이밍을 읽을 수 있게 하는 신호.
    /// </summary>
    public sealed class AttackTelegraph : MonoBehaviour
    {
        [SerializeField] ParticleSystem particles;
        [Tooltip("예고가 끝날 때 남은 입자를 빠르게 정리할지")]
        [SerializeField] bool clearOnEnd;

        Coroutine routine;

        public int PlayCount { get; private set; }
        public bool IsPlaying => particles != null && particles.isEmitting;

        public void Play(float duration)
        {
            if (particles == null || duration <= 0f)
                return;

            PlayCount++;
            if (routine != null)
                StopCoroutine(routine);
            particles.Clear();
            particles.Play();
            routine = StartCoroutine(StopAfter(duration));
        }

        public void Stop()
        {
            if (routine != null)
            {
                StopCoroutine(routine);
                routine = null;
            }
            if (particles != null)
                particles.Stop(true, clearOnEnd ? ParticleSystemStopBehavior.StopEmittingAndClear : ParticleSystemStopBehavior.StopEmitting);
        }

        IEnumerator StopAfter(float duration)
        {
            yield return new WaitForSeconds(duration);
            routine = null;
            Stop();
        }

        void OnDisable() => Stop();
    }
}
