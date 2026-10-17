using System.Collections;
using Bastion.Core;
using TMPro;
using UnityEngine;

namespace Bastion.UI
{
    /// <summary>사망/클리어 시 화면 중앙에 큰 문구를 페이드 인한다.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ScreenMessage : MonoBehaviour
    {
        [SerializeField] TMP_Text label;
        [SerializeField] string deathText = "YOU DIED";
        [SerializeField] Color deathColor = new Color(0.75f, 0.1f, 0.1f);
        [SerializeField] string clearText = "OATH FULFILLED";
        [SerializeField] Color clearColor = new Color(0.95f, 0.85f, 0.55f);
        [SerializeField, Min(0.01f)] float fadeTime = 1f;

        CanvasGroup group;

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
        }

        void OnEnable()
        {
            GameFlow.PlayerDied += OnPlayerDied;
            GameFlow.StageCleared += OnStageCleared;
        }

        void OnDisable()
        {
            GameFlow.PlayerDied -= OnPlayerDied;
            GameFlow.StageCleared -= OnStageCleared;
        }

        void OnPlayerDied() => Show(deathText, deathColor);

        void OnStageCleared() => Show(clearText, clearColor);

        public void Show(string text, Color color)
        {
            label.text = text;
            label.color = color;
            StopAllCoroutines();
            StartCoroutine(Fade());
        }

        IEnumerator Fade()
        {
            for (float t = 0f; t < fadeTime; t += Time.unscaledDeltaTime)
            {
                group.alpha = t / fadeTime;
                yield return null;
            }
            group.alpha = 1f;
        }
    }
}
