using Bastion.Combat;
using UnityEngine;

namespace Bastion.Hazards
{
    /// <summary>
    /// 레벨 아래에 까는 넓은 트리거. 떨어진 대상은 무적이어도 즉시 사망한다 (낙사).
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public sealed class KillZone : MonoBehaviour
    {
        void Awake() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            var health = other.GetComponentInParent<Health>();
            if (health != null && !health.IsDead)
                health.Kill();
        }
    }
}
