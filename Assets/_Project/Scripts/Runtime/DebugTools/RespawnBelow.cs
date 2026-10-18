using Game.Player;
using UnityEngine;

namespace Game.DebugTools
{
    /// <summary>Deneme sahnesi için: oyuncu belli yüksekliğin altına düşerse ya da R'ye basılırsa başlangıç noktasına döner.</summary>
    public class RespawnBelow : MonoBehaviour
    {
        [SerializeField] PlayerMotor motor;
        [SerializeField] float killHeight = -10f;

        Vector2 spawn;
        bool resetRequested;

        void Awake() => spawn = transform.position;

        void Update()
        {
            var keyboard = UnityEngine.InputSystem.Keyboard.current;
            if (keyboard != null && keyboard.rKey.wasPressedThisFrame) resetRequested = true;
        }

        void FixedUpdate()
        {
            if (!resetRequested && transform.position.y > killHeight) return;
            resetRequested = false;
            var body = GetComponent<Rigidbody2D>();
            if (body != null) body.position = spawn;
            transform.position = spawn;
            if (motor != null) { motor.Velocity = Vector2.zero; motor.ExternalControl = false; }
        }
    }
}
