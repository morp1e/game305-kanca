using Game.Core;
using Game.Input;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Kinematic oyuncu kontrolcüsü. SÖZLEŞME İSKELETİ: gövde Lane A tarafından yazılacak.
    /// Public API ve serileştirilmiş alan adları (tuning, input) değişmez; eklenebilir.
    /// Çalışma sırası 0: SwordHook (-10) Velocity'yi önce yazar, motor sonra hareket ettirir.
    /// </summary>
    [DefaultExecutionOrder(0)]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerMotor : MonoBehaviour, IPlayerBody
    {
        [SerializeField] PlayerMovementTuning tuning;
        [SerializeField] PlayerInputReader input;

        public Vector2 Position => transform.position;
        public Vector2 Velocity { get; set; }
        public bool IsGrounded { get; private set; }
        public Collider2D BodyCollider => GetComponent<Collider2D>();
        public bool ExternalControl { get; set; }

        /// <summary>-1 sol, +1 sağ. Görsel ve kılıç yönü için.</summary>
        public int Facing { get; private set; } = 1;

        public void OpenLaunchJumpWindow(float seconds) { }
    }
}
