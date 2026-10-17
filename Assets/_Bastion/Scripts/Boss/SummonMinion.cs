using Bastion.Combat;
using UnityEngine;

namespace Bastion.Boss
{
    /// <summary>보스가 소환하는 불꽃 정령. 잠시 뒤 플레이어에게 날아가 부딪히면 피해를 주고 사라진다.</summary>
    [RequireComponent(typeof(Health), typeof(Rigidbody2D))]
    public sealed class SummonMinion : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField, Min(0f)] float appearTime = 0.6f;
        [SerializeField, Min(0f)] float speed = 3.2f;
        [SerializeField, Min(0f)] float turnRate = 4f;
        [SerializeField, Min(0f)] float contactRadius = 0.4f;
        [SerializeField, Min(0)] int contactDamage = 8;
        [SerializeField, Min(0f)] float lifetime = 8f;
        [SerializeField, Min(0f)] float deathTime = 0.5f;

        Rigidbody2D body;
        Health health;
        Transform target;
        LayerMask targetLayer;
        float spawnTime;
        Vector2 velocity;
        bool dying;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            health = GetComponent<Health>();
            spawnTime = Time.time;
        }

        void OnEnable() => health.Died += OnDied;

        void OnDisable() => health.Died -= OnDied;

        public void Initialize(Transform chaseTarget, LayerMask damageLayer)
        {
            target = chaseTarget;
            targetLayer = damageLayer;
            animator.Play("Appear", 0, 0f);
        }

        public void Kill()
        {
            if (!health.IsDead)
                health.TakeDamage(new DamageInfo(health.Max, Vector2.zero, 0f, null));
        }

        void FixedUpdate()
        {
            if (dying)
                return;
            float age = Time.time - spawnTime;
            if (age >= lifetime)
            {
                Kill();
                return;
            }
            if (age < appearTime || target == null)
                return;

            // 목표 방향으로 서서히 선회하며 날아간다 (회피할 여지를 준다).
            Vector2 desired = ((Vector2)target.position + Vector2.up * 1.1f - body.position).normalized * speed;
            velocity = Vector2.Lerp(velocity, desired, turnRate * Time.fixedDeltaTime);
            body.MovePosition(body.position + velocity * Time.fixedDeltaTime);

            var hit = Physics2D.OverlapCircle(body.position, contactRadius, targetLayer);
            if (hit != null)
            {
                var damageable = hit.GetComponentInParent<IDamageable>();
                if (damageable != null && !damageable.IsDead)
                {
                    damageable.TakeDamage(new DamageInfo(contactDamage, new Vector2(Mathf.Sign(velocity.x), 0f), 4f, gameObject));
                    Kill();
                }
            }
        }

        void OnDied()
        {
            dying = true;
            foreach (var col in GetComponentsInChildren<Collider2D>())
                col.enabled = false;
            animator.Play("Death", 0, 0f);
            Destroy(gameObject, deathTime);
        }
    }
}
