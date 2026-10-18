using Bastion.Combat;
using UnityEngine;

namespace Bastion.Items
{
    /// <summary>
    /// 회복 아이템. 플레이어가 닿으면 체력을 회복하고 사라진다.
    /// 체력이 가득 차 있으면 먹지 않고 남겨 둔다.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class HealthPickup : MonoBehaviour
    {
        [SerializeField, Min(1)] int healAmount = 30;
        [Tooltip("위아래로 떠다니는 그림")]
        [SerializeField] Transform visual;
        [SerializeField, Min(0f)] float bobHeight = 0.12f;
        [SerializeField, Min(0f)] float bobSpeed = 2.5f;

        Vector3 visualStart;

        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            if (visual != null)
                visualStart = visual.localPosition;
        }

        void Update()
        {
            if (visual != null)
                visual.localPosition = visualStart + Vector3.up * (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        }

        // 가득 찬 상태로 서 있다가 피해를 입으면 바로 먹을 수 있게 Stay에서 검사한다.
        void OnTriggerStay2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
                return;
            var health = other.GetComponentInParent<Health>();
            if (health != null && health.Heal(healAmount))
                Destroy(gameObject);
        }
    }
}
