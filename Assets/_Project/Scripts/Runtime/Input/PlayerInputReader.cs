using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input
{
    /// <summary>
    /// Klavye + fare girdisi. Update'te okur, basışları zaman damgasıyla tamponlar;
    /// FixedUpdate'teki sistemler Consume* ile tüketir. Tuş atamaları bu dosyadadır.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class PlayerInputReader : MonoBehaviour
    {
        InputAction move, jump, throwHook, attack, point;

        double jumpPressedAt = double.NegativeInfinity;
        double throwPressedAt = double.NegativeInfinity;
        double attackPressedAt = double.NegativeInfinity;

        /// <summary>-1..1 yatay girdi.</summary>
        public float MoveX { get; private set; }
        public bool JumpHeld { get; private set; }
        /// <summary>İmlecin ekran konumu (piksel).</summary>
        public Vector2 PointerScreen { get; private set; }

        /// <summary>Basışlar ve tüketim aynı saatle ölçülür; Update ile FixedUpdate arasında kayma olmaz.</summary>
        static double Now => Time.realtimeSinceStartupAsDouble;

        void Awake()
        {
            move = new InputAction("Move", InputActionType.Value);
            move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/a").With("Positive", "<Keyboard>/d");
            move.AddCompositeBinding("1DAxis").With("Negative", "<Keyboard>/leftArrow").With("Positive", "<Keyboard>/rightArrow");

            jump = new InputAction("Jump", InputActionType.Button);
            jump.AddBinding("<Keyboard>/space");
            jump.AddBinding("<Keyboard>/w");

            throwHook = new InputAction("ThrowHook", InputActionType.Button, "<Mouse>/rightButton");
            attack = new InputAction("Attack", InputActionType.Button, "<Mouse>/leftButton");
            point = new InputAction("Point", InputActionType.Value, "<Pointer>/position");
        }

        void OnEnable()
        {
            move.Enable(); jump.Enable(); throwHook.Enable(); attack.Enable(); point.Enable();
        }

        void OnDisable()
        {
            move.Disable(); jump.Disable(); throwHook.Disable(); attack.Disable(); point.Disable();
        }

        void OnDestroy()
        {
            move.Dispose(); jump.Dispose(); throwHook.Dispose(); attack.Dispose(); point.Dispose();
        }

        void Update()
        {
            MoveX = Mathf.Clamp(move.ReadValue<float>(), -1f, 1f);
            JumpHeld = jump.IsPressed();
            PointerScreen = point.ReadValue<Vector2>();

            if (jump.WasPressedThisFrame()) jumpPressedAt = Now;
            if (throwHook.WasPressedThisFrame()) throwPressedAt = Now;
            if (attack.WasPressedThisFrame()) attackPressedAt = Now;
        }

        /// <summary>Son bufferSeconds içinde zıplamaya basıldıysa true döner ve basışı harcar.</summary>
        public bool ConsumeJump(float bufferSeconds) => Consume(ref jumpPressedAt, bufferSeconds);
        public bool ConsumeThrow(float bufferSeconds) => Consume(ref throwPressedAt, bufferSeconds);
        public bool ConsumeAttack(float bufferSeconds) => Consume(ref attackPressedAt, bufferSeconds);

        /// <summary>Harcamadan bakar.</summary>
        public bool HasBufferedJump(float bufferSeconds) => Now - jumpPressedAt <= bufferSeconds;

        public Vector2 PointerWorld(Camera cam)
        {
            if (cam == null) return Vector2.zero;
            var p = new Vector3(PointerScreen.x, PointerScreen.y, -cam.transform.position.z);
            return cam.ScreenToWorldPoint(p);
        }

        static bool Consume(ref double pressedAt, float bufferSeconds)
        {
            if (Now - pressedAt > bufferSeconds) return false;
            pressedAt = double.NegativeInfinity;
            return true;
        }
    }
}
