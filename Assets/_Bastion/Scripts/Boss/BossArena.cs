using Bastion.Audio;
using UnityEngine;

namespace Bastion.Boss
{
    /// <summary>플레이어가 아레나에 들어오면 입구를 막고 보스전을 시작한다.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class BossArena : MonoBehaviour
    {
        [SerializeField] BossController boss;
        [SerializeField] GameObject gate;
        [SerializeField] GameObject bossHud;
        [Tooltip("보스전이 시작되면 바꿀 배경음")]
        [SerializeField] AudioClip bossMusic;

        bool started;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (gate != null) gate.SetActive(false);
            if (bossHud != null) bossHud.SetActive(false);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (started || !other.CompareTag("Player"))
                return;

            started = true;
            if (gate != null) gate.SetActive(true);
            if (bossHud != null) bossHud.SetActive(true);
            boss.Activate();
            SoundManager.PlayMusic(bossMusic);
        }
    }
}
