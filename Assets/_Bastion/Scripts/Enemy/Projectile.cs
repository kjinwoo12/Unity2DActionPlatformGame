using Bastion.Combat;
using UnityEngine;

namespace Bastion.Enemy
{
    /// <summary>직선 투사체. 대상에 닿으면 피해를 주고, 지형에 닿거나 수명이 다하면 사라진다.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public sealed class Projectile : MonoBehaviour
    {
        [SerializeField, Min(0f)] float lifetime = 4f;

        Rigidbody2D body;
        AttackStep attack;
        LayerMask targetLayer;
        LayerMask groundLayer;
        GameObject owner;

        void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            GetComponent<Collider2D>().isTrigger = true;
        }

        public void Launch(Vector2 velocity, AttackStep attackStep, LayerMask targets, LayerMask ground, GameObject source)
        {
            attack = attackStep;
            targetLayer = targets;
            groundLayer = ground;
            owner = source;
            body.linearVelocity = velocity;
            float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
            Destroy(gameObject, lifetime);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            int layerBit = 1 << other.gameObject.layer;

            if ((targetLayer.value & layerBit) != 0)
            {
                var target = other.GetComponentInParent<IDamageable>();
                if (target != null && !target.IsDead)
                {
                    Vector2 direction = new Vector2(Mathf.Sign(body.linearVelocity.x), 0f);
                    target.TakeDamage(new DamageInfo(attack.Damage, direction, attack.Knockback, owner));
                }
                Destroy(gameObject);
            }
            else if ((groundLayer.value & layerBit) != 0)
            {
                Destroy(gameObject);
            }
        }
    }
}
