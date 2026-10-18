using Game.Hook;
using Game.Level;
using Game.Dialogue;
using Game.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.DebugTools
{
    /// <summary>Oyuncu ve kanca durumunu ekranda gösteren küçük deneme HUD'u.</summary>
    public class DebugHud : MonoBehaviour
    {
        [SerializeField] PlayerMotor motor;
        [SerializeField] SwordHook swordHook;
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
            string target = swordHook != null && swordHook.AimTarget != null ? swordHook.AimTarget.name : "yok";
            string state = swordHook != null ? swordHook.State.ToString() : "yok";
            string dead = respawn != null ? respawn.IsDead.ToString() : "yok";
            string line = swordVoice != null ? swordVoice.CurrentLine ?? "yok" : "yok";
            string content = $"FPS {fps:0}\nHız X {velocity.x:0.00}  Y {velocity.y:0.00}  |v| {velocity.magnitude:0.00}\n" +
                $"IsGrounded {motor.IsGrounded}  ExternalControl {motor.ExternalControl}\nSwordHook {state}  AimTarget {target}\nIsDead {dead}  SwordVoice {line}";
            GUI.Box(new Rect(12, 12, 420, 112), content);
        }
    }
}
