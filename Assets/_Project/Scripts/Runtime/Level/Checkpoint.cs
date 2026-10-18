using UnityEngine;

namespace Game.Level
{
    /// <summary>Oyuncu içinden geçince yeniden doğma noktası olur. Trigger collider ister. SÖZLEŞME İSKELETİ (Lane 1). Alan: spawnOffset.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] Vector2 spawnOffset = new Vector2(0f, 1f);
        [Tooltip("Etkin checkpoint rengi.")]
        [SerializeField] Color activeColor = Color.green;
        [Tooltip("Etkin olmayan checkpoint rengi.")]
        [SerializeField] Color inactiveColor = Color.white;

        SpriteRenderer spriteRenderer;
        void Awake()
        {
            GetComponent<Collider2D>().isTrigger = true;
            spriteRenderer = GetComponent<SpriteRenderer>();
            SetActive(false);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var body = other.attachedRigidbody;
            if (body == null) return;
            var respawn = body.GetComponent<Game.Level.RespawnController>();
            if (respawn == null) respawn = body.GetComponentInParent<Game.Level.RespawnController>();
            respawn?.SetCheckpoint(this, transform.position + (Vector3)spawnOffset);
        }

        internal void SetActive(bool active)
        {
            if (spriteRenderer != null) spriteRenderer.color = active ? activeColor : inactiveColor;
        }
    }
}
