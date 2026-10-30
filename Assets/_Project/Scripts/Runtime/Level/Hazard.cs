using UnityEngine;
using Game.Player;
using Game.Combat;

namespace Game.Level
{
    /// <summary>Dokunanı öldüren alan (diken, çukur). Hazard katmanında trigger collider ister.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Hazard : MonoBehaviour
    {
        void Awake() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other) => TryKill(other);

        // Yeniden doğma noktası dikenin içindeyse Enter tekrar gelmez; Stay bunu yakalar.
        void OnTriggerStay2D(Collider2D other) => TryKill(other);

        static void TryKill(Collider2D other)
        {
            var body = other.attachedRigidbody;
            if (body == null) return;
            var health = body.GetComponentInParent<PlayerHealth>();
            if (health != null) { health.TakeHazardHit(); return; }
            var respawn = body.GetComponentInParent<RespawnController>();
            if (respawn != null) { respawn.Kill(); return; }
            if (body.gameObject.layer != LayerMask.NameToLayer("Enemy")) return;
            var damageables = body.GetComponentsInParent<MonoBehaviour>();
            foreach (var component in damageables)
            {
                if (component is IDamageable damageable)
                {
                    damageable.TakeHit(new HitInfo(999f, Vector2.zero, 0f, other));
                    return;
                }
            }
        }
    }
}
