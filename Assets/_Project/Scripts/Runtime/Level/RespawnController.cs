using Game.Core;
using Game.Player;
using UnityEngine;

namespace Game.Level
{
    /// <summary>
    /// Oyuncunun ölüm ve yeniden doğma akışı. Oyuncu objesinde durur.
    /// SÖZLEŞME İSKELETİ: gövde Lane 1 tarafından yazılacak. Public API ve alan adları (motor, deathEvent, respawnEvent) değişmez.
    /// </summary>
    public class RespawnController : MonoBehaviour
    {
        [SerializeField] PlayerMotor motor;
        [SerializeField] GameEvent deathEvent;
        [SerializeField] GameEvent respawnEvent;

        public Vector2 CurrentCheckpoint { get; private set; }
        public bool IsDead { get; private set; }

        public void Kill() { }
        public void SetCheckpoint(Vector2 position) { }
    }
}
