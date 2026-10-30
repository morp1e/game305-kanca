using Game.Combat;
using Game.Player;
using UnityEngine;
namespace Game.Enemies
{
    /// <summary>Düşman mermisi; kılıçla sektirilince düşmanları vurabilir.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class Projectile : MonoBehaviour, IDamageable
    {
        [SerializeField] ProjectileDefinition definition;
        Rigidbody2D body;
        float remaining;
        bool reflected;
        bool initialized;
        MonoBehaviour owner;
        void Awake()  {
            body = GetComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            GetComponent<CircleCollider2D>().isTrigger = true;
        }
        public void Initialize(ProjectileDefinition settings, Vector2 direction, MonoBehaviour source)
        {
            definition = settings;
            owner = source;
            remaining = settings.lifetime;
            initialized = true;
            body.linearVelocity = direction.normalized * settings.speed;
            GetComponent<CircleCollider2D>().radius = settings.radius;
        }
        void FixedUpdate()  {
            if (!initialized) return;
            remaining -= Time.fixedDeltaTime;
            if (remaining <= 0f) Destroy(gameObject);
        }
        public void TakeHit(HitInfo hit)
        {
            if (!initialized || !definition.deflectable || reflected) return;
            reflected = true;
            owner = null;
            Vector2 direction = EnemyMath.DeflectDirection(hit.Direction);
            body.linearVelocity = direction * EnemyMath.DeflectSpeed(definition.speed, definition.reflectSpeedMult);
        }
        void OnTriggerEnter2D(Collider2D other)
        {
            if (!initialized) return;
            if (other.gameObject.layer == LayerMask.NameToLayer("Ground"))  {
                Destroy(gameObject);
                return;
            }
            if (other.attachedRigidbody == null) return;
            if (!reflected && other.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                var health = other.attachedRigidbody.GetComponentInParent<PlayerHealth>();
                health?.TakeHit(new HitInfo(definition.damage, body.linearVelocity.normalized, 0f, this));
                Destroy(gameObject);
                return;
            }
            if (reflected && other.gameObject.layer == LayerMask.NameToLayer("Enemy"))
            {
                var behaviours = other.attachedRigidbody.GetComponentsInParent<MonoBehaviour>();
                foreach (var behaviour in behaviours) if (behaviour is IDamageable damageable)  {
                    damageable.TakeHit(new HitInfo(definition.damage, body.linearVelocity.normalized, 0f, this));
                    break;
                }
                Destroy(gameObject);
            }
        }
    }
}
