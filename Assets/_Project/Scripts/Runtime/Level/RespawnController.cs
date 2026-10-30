using Game.Core;
using Game.Hook;
using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;

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
        [Tooltip("Ölümden sonra yeniden doğma süresi, saniye. Ö: oynayarak ayarlanacak.")]
        [SerializeField] float respawnDelay = 0.6f;
        [Tooltip("Bu yüksekliğin altına düşmek ölümü tetikler, u. Ö: bölüm tasarımına göre ayarlanacak.")]
        [SerializeField] float killHeight = -20f;
        [Tooltip("Ölüm ve yeniden doğma sırasında gizlenen görsel kök.")]
        [SerializeField] GameObject visual;

        public Vector2 CurrentCheckpoint { get; private set; }
        public bool IsDead { get; private set; }
        public bool IsReturning => returning;
        public event System.Action Respawned;
        Checkpoint activeCheckpoint;
        GrappleHook hook;
        float respawnAt;
        bool returning;
        bool bodyWasSimulated;

        void Awake()
        {
            if (motor == null) motor = GetComponent<PlayerMotor>();
            hook = GetComponent<GrappleHook>();
            if (visual == null && transform.Find("Visual") != null) visual = transform.Find("Visual").gameObject;
            CurrentCheckpoint = motor != null ? motor.Position : transform.position;
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame) Kill();
        }

        void FixedUpdate()
        {
            if (returning)
            {
                if (Time.time < respawnAt) return;
                CompleteReturn();
                return;
            }
            if (!IsDead)
            {
                if (motor != null && motor.Position.y < killHeight) Kill();
                return;
            }

            if (Time.time < respawnAt) return;
            Respawn();
        }

        public void Kill()
        {
            if (IsDead || returning) return;
            IsDead = true;
            respawnAt = Time.time + Mathf.Max(0f, respawnDelay);
            if (deathEvent != null) deathEvent.Raise();
            // Çekiş sürerken ölünürse önce kancayı ele al.
            if (hook != null) hook.ResetToHand();
            if (motor != null)
            {
                motor.ExternalControl = true;
                motor.Velocity = Vector2.zero;
            }
            if (visual != null) visual.SetActive(false);
        }

        public void ReturnTo(Vector2 position, float delay)
        {
            if (IsDead || returning) return;
            returning = true;
            respawnAt = Time.time + Mathf.Max(0f, delay);
            if (hook != null) hook.ResetToHand();
            if (motor != null)
            {
                var body = motor.GetComponent<Rigidbody2D>();
                if (body != null) { bodyWasSimulated = body.simulated; body.simulated = false; }
                motor.ExternalControl = true;
                motor.Velocity = Vector2.zero;
            }
            if (visual != null) visual.SetActive(false);
            returnPosition = position;
        }

        Vector2 returnPosition;

        void CompleteReturn()
        {
            if (motor != null)
            {
                var body = motor.GetComponent<Rigidbody2D>();
                if (body != null) { body.position = returnPosition; body.simulated = bodyWasSimulated; }
                motor.transform.position = returnPosition;
                motor.Velocity = Vector2.zero;
                motor.ExternalControl = false;
            }
            if (visual != null) visual.SetActive(true);
            returning = false;
        }

        public void SetCheckpoint(Vector2 position)
        {
            if (activeCheckpoint != null) activeCheckpoint.SetActive(false);
            CurrentCheckpoint = position;
            activeCheckpoint = null;
        }

        internal void SetCheckpoint(Checkpoint checkpoint, Vector2 position)
        {
            if (activeCheckpoint != null && activeCheckpoint != checkpoint)
                activeCheckpoint.SetActive(false);
            activeCheckpoint = checkpoint;
            CurrentCheckpoint = position;
            if (activeCheckpoint != null) activeCheckpoint.SetActive(true);
        }

        void Respawn()
        {
            if (motor != null)
            {
                var body = motor.GetComponent<Rigidbody2D>();
                if (body != null) body.position = CurrentCheckpoint;
                motor.transform.position = CurrentCheckpoint;
                motor.Velocity = Vector2.zero;
                motor.ExternalControl = false;
            }
            if (hook != null) hook.ResetToHand();
            if (visual != null) visual.SetActive(true);
            IsDead = false;
            if (respawnEvent != null) respawnEvent.Raise();
            Respawned?.Invoke();
        }
    }
}
