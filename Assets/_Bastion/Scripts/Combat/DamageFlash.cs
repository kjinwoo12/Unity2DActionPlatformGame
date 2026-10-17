using System.Collections;
using UnityEngine;

namespace Bastion.Combat
{
    /// <summary>피격 시 스프라이트를 잠깐 붉게 물들인다.</summary>
    [RequireComponent(typeof(Health))]
    public sealed class DamageFlash : MonoBehaviour
    {
        [SerializeField] SpriteRenderer[] renderers;
        [SerializeField] Color flashColor = new Color(1f, 0.35f, 0.35f);
        [SerializeField, Min(0f)] float duration = 0.12f;

        Health health;
        Color[] originalColors;
        Coroutine routine;

        void Awake()
        {
            health = GetComponent<Health>();
            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<SpriteRenderer>();
            originalColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                originalColors[i] = renderers[i].color;
        }

        void OnEnable() => health.Damaged += OnDamaged;

        void OnDisable()
        {
            health.Damaged -= OnDamaged;
            Restore();
        }

        void OnDamaged(DamageInfo _)
        {
            if (routine != null)
                StopCoroutine(routine);
            routine = StartCoroutine(Flash());
        }

        IEnumerator Flash()
        {
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].color = flashColor;
            // 히트스톱 중에도 깜빡임이 보이도록 실시간 기준으로 기다린다.
            yield return new WaitForSecondsRealtime(duration);
            Restore();
            routine = null;
        }

        void Restore()
        {
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].color = originalColors[i];
        }
    }
}
