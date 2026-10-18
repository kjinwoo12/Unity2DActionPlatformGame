using System;
using Bastion.Boss;
using UnityEngine;

namespace Bastion.Audio
{
    /// <summary>보스의 행동(베기, 가시, 소환, 페이즈 전환)마다 효과음을 낸다. 행동 이름은 BossData의 애니메이션 이름과 같다.</summary>
    [RequireComponent(typeof(BossController))]
    public sealed class BossSounds : MonoBehaviour
    {
        [Serializable]
        struct ActionSound
        {
            public string Action;
            public AudioClip Clip;
        }

        [SerializeField] ActionSound[] sounds;

        BossController boss;

        void Awake() => boss = GetComponent<BossController>();

        void OnEnable() => boss.ActionStarted += OnActionStarted;

        void OnDisable() => boss.ActionStarted -= OnActionStarted;

        void OnActionStarted(string action)
        {
            foreach (var sound in sounds)
            {
                if (sound.Action == action)
                {
                    SoundManager.PlaySfx(sound.Clip);
                    return;
                }
            }
        }
    }
}
