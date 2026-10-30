using UnityEngine;

namespace Game.Combat
{
    public class ContactDamage : MonoBehaviour
    {
        [SerializeField] float damage = 1f;
        [SerializeField] LayerMask targetLayers;
        public bool Suppressed { get; set; }

        void Awake()
        {
            if (targetLayers.value == 0) targetLayers = LayerMask.GetMask("Player");
        }

        void OnCollisionEnter2D(Collision2D collision) => TryDamage(collision.collider);
        void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.collider);
        void OnTriggerEnter2D(Collider2D other) => TryDamage(other);
        void OnTriggerStay2D(Collider2D other) => TryDamage(other);

        void TryDamage(Collider2D target)
        {
            if (Suppressed || target == null || (targetLayers.value & (1 << target.gameObject.layer)) == 0) return;
            Rigidbody2D targetBody = target.attachedRigidbody;
            if (targetBody == null || targetBody.gameObject == gameObject || targetBody.transform.IsChildOf(transform)) return;
            Vector2 source = transform.position;
            Vector2 destination = targetBody.position;
            var components = targetBody.GetComponentsInParent<MonoBehaviour>();
            for (int i = 0; i < components.Length; i++)
                if (components[i] is IDamageable damageable)
                {
                    damageable.TakeHit(new HitInfo(damage, (destination - source).normalized, 0f, this));
                    return;
                }
        }
    }
}
