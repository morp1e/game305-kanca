using Game.Player;
using UnityEngine;

namespace Game.Level
{
    [RequireComponent(typeof(Collider2D))]
    public class HazardRespawnZone : MonoBehaviour
    {
        [SerializeField] Transform respawnPoint;
        [Tooltip("RespawnPoint atanmamışsa bölge merkezine eklenecek dönüş ofseti. Ö: oynayarak ayarlanacak.")]
        [SerializeField] Vector2 spawnOffset = new Vector2(0f, 1f);

        void Awake() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            Rigidbody2D body = other.attachedRigidbody;
            if (body == null) return;
            var health = body.GetComponent<PlayerHealth>();
            if (health == null) health = body.GetComponentInParent<PlayerHealth>();
            if (health == null) return;
            Vector2 position = respawnPoint != null ? (Vector2)respawnPoint.position : (Vector2)transform.position + spawnOffset;
            health.SetHazardReturnPoint(position);
        }
    }
}
