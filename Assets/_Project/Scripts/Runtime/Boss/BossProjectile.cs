using Game.Combat;
using UnityEngine;

namespace Game.Boss
{
    /// <summary>
    /// Bekçinin şok dalgası ve molozu. Düz çizgide ilerler; her fizik adımında kendi kutusunu Ground ve Player
    /// katmanlarına karşı sorgular. Trigger olayına güvenilmez: Kinematic gövde ile statik zemin arasında olay üretilmez.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class BossProjectile : MonoBehaviour
    {
        float damage = 1f;
        float lifetime = 4f;
        Vector2 direction;
        float speed;
        float elapsed;
        Collider2D area;
        Rigidbody2D body;
        int groundMask;
        int playerMask;

        void Awake()
        {
            area = GetComponent<Collider2D>();
            area.isTrigger = true;
            body = GetComponent<Rigidbody2D>();
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
            }
            groundMask = LayerMask.GetMask("Ground");
            playerMask = LayerMask.GetMask("Player");
        }

        public void Initialize(Vector2 moveDirection, float moveSpeed, float hitDamage, float maxLifetime)
        {
            direction = moveDirection.normalized;
            speed = moveSpeed;
            damage = hitDamage;
            lifetime = maxLifetime;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            elapsed += dt;
            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            Vector2 next = (body != null ? body.position : (Vector2)transform.position) + direction * speed * dt;
            if (body != null) body.MovePosition(next);
            else transform.position = next;

            Vector2 center = next + area.offset;
            Vector2 size = area.bounds.size;
            if (Physics2D.OverlapBox(center, size, 0f, groundMask) != null)
            {
                Destroy(gameObject);
                return;
            }

            var hit = Physics2D.OverlapBox(center, size, 0f, playerMask);
            if (hit == null || hit.attachedRigidbody == null) return;
            var components = hit.attachedRigidbody.GetComponentsInParent<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component is IDamageable damageable)
                {
                    damageable.TakeHit(new HitInfo(damage, direction, 0f, this));
                    break;
                }
            }
            Destroy(gameObject);
        }
    }
}
