using System;
using Bastion.Audio;
using Bastion.Combat;
using UnityEngine;

namespace Bastion.Core
{
    /// <summary>플레이어가 닿으면 부활 지점으로 등록하고 체력을 회복시킨다.</summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class Checkpoint : MonoBehaviour
    {
        [SerializeField] Transform spawnPoint;
        [SerializeField] SpriteRenderer statue;
        [SerializeField] Sprite inactiveSprite;
        [SerializeField] Sprite activeSprite;
        [Tooltip("활성화되면 켜지는 오브젝트 (불빛 등)")]
        [SerializeField] GameObject activeOnly;
        [SerializeField] AudioClip activateSound;

        public bool IsActive { get; private set; }
        public Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;

        public static event Action<Checkpoint> Activated;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            // 씬을 다시 불러왔을 때 이미 활성화한 체크포인트면 그 상태를 유지한다.
            IsActive = CheckpointSystem.HasCheckpoint && Vector3.Distance(CheckpointSystem.Position, SpawnPosition) < 0.01f;
            UpdateVisual();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (IsActive || !other.CompareTag("Player"))
                return;

            IsActive = true;
            CheckpointSystem.Activate(gameObject.scene.name, SpawnPosition);
            var health = other.GetComponentInParent<Health>();
            if (health != null && !health.IsDead)
                health.ResetHealth();
            UpdateVisual();
            SoundManager.PlaySfx(activateSound);
            Activated?.Invoke(this);
        }

        void UpdateVisual()
        {
            if (statue != null)
            {
                Sprite sprite = IsActive ? activeSprite : inactiveSprite;
                if (sprite != null)
                    statue.sprite = sprite;
            }
            if (activeOnly != null)
                activeOnly.SetActive(IsActive);
        }
    }
}
