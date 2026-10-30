using Game.Hook;
using Game.Level;
using Game.Dialogue;
using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace Game.DebugTools
{
    /// <summary>Oyuncu ve kanca durumunu ekranda gösteren küçük deneme HUD'u.</summary>
    public class DebugHud : MonoBehaviour
    {
        [SerializeField] PlayerMotor motor;
        [SerializeField, FormerlySerializedAs("swordHook")] GrappleHook grappleHook;
        [SerializeField] RespawnController respawn;
        [SerializeField] SwordVoice swordVoice;

        bool visible;
        float fps;

        void Awake()
        {
#if UNITY_EDITOR
            visible = true;
#else
            visible = false;
#endif
        }

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame) visible = !visible;
            float instant = Time.unscaledDeltaTime > 0f ? 1f / Time.unscaledDeltaTime : 0f;
            fps = Mathf.Lerp(fps, instant, 1f - Mathf.Exp(-Time.unscaledDeltaTime * 8f));
        }

        void OnGUI()
        {
            if (!visible || motor == null) return;
            Vector2 velocity = motor.Velocity;
            string state = grappleHook != null ? grappleHook.State.ToString() : "yok";
            string dead = respawn != null ? respawn.IsDead.ToString() : "yok";
            string line = swordVoice != null ? swordVoice.CurrentLine ?? "yok" : "yok";
            string content = $"FPS {fps:0}\nHız X {velocity.x:0.00}  Y {velocity.y:0.00}  |v| {velocity.magnitude:0.00}\n" +
                $"IsGrounded {motor.IsGrounded}  ExternalControl {motor.ExternalControl}\nGrappleHook {state}\nIsDead {dead}  SwordVoice {line}";
            // Sağ üstte: sol üst oyuncunun can maskelerine ayrıldı (GameHud).
            GUI.Box(new Rect(Screen.width - 432, 12, 420, 112), content);
        }
    }
}
