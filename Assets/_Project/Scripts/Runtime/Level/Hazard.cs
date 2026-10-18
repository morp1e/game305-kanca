using UnityEngine;

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
            var respawn = body.GetComponentInParent<RespawnController>();
            if (respawn != null) respawn.Kill();
        }
    }
}
