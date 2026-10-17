using Bastion.Combat;
using UnityEngine;
using UnityEngine.UI;

namespace Bastion.UI
{
    /// <summary>
    /// 체력 바. 실제 체력(fill)은 즉시, 잔상(trail)은 잠시 뒤에 따라 줄어들어 받은 피해량이 보이게 한다.
    /// </summary>
    public sealed class HealthBar : MonoBehaviour
    {
        [SerializeField] Health target;
        [SerializeField] Image fill;
        [SerializeField] Image trail;
        [SerializeField, Min(0f)] float trailDelay = 0.4f;
        [SerializeField, Min(0f)] float trailSpeed = 0.8f;

        float trailHoldUntil;

        public Health Target
        {
            get => target;
            set
            {
                Unbind();
                target = value;
                Bind();
            }
        }

        void OnEnable() => Bind();

        void OnDisable() => Unbind();

        void Bind()
        {
            if (target == null || !isActiveAndEnabled)
                return;
            target.Changed += OnChanged;
            Refresh();
        }

        // 대상 Health의 Awake가 이 컴포넌트의 OnEnable보다 늦을 수 있어 Start에서 한 번 더 맞춘다.
        void Start() => Refresh();

        void Refresh()
        {
            if (target == null)
                return;
            float ratio = target.Max > 0 ? (float)target.Current / target.Max : 0f;
            fill.fillAmount = ratio;
            if (trail != null)
                trail.fillAmount = ratio;
        }

        void Unbind()
        {
            if (target != null)
                target.Changed -= OnChanged;
        }

        void OnChanged(int current, int max)
        {
            fill.fillAmount = max > 0 ? (float)current / max : 0f;
            trailHoldUntil = Time.unscaledTime + trailDelay;
            if (trail != null && trail.fillAmount < fill.fillAmount)
                trail.fillAmount = fill.fillAmount; // 회복은 즉시 반영
        }

        void Update()
        {
            if (trail == null || Time.unscaledTime < trailHoldUntil)
                return;
            trail.fillAmount = Mathf.MoveTowards(trail.fillAmount, fill.fillAmount, trailSpeed * Time.unscaledDeltaTime);
        }
    }
}
