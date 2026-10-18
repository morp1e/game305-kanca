using System.Collections.Generic;
using Game.Core;
using Game.Input;
using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// Kinematik gövde hareketi ve oyuncu zıplamasını yürütür.
    /// Çalışma sırası 0: SwordHook (-10) Velocity'yi önce yazar, motor sonra hareket ettirir.
    /// </summary>
    [DefaultExecutionOrder(0)]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerMotor : MonoBehaviour, IPlayerBody
    {
        [SerializeField] PlayerMovementTuning tuning;
        [SerializeField] PlayerInputReader input;

        Rigidbody2D body;
        Collider2D bodyCollider;
        readonly List<RaycastHit2D> castHits = new List<RaycastHit2D>(8);
        ContactFilter2D movementFilter;
        ContactFilter2D groundFilter;
        readonly JumpGate jumpGate = new JumpGate();
        float variableJumpRemaining;
        Vector2 gizmoGroundEnd;

        public Vector2 Position => body != null ? body.position : (Vector2)transform.position;
        public Vector2 Velocity { get; set; }
        public bool IsGrounded { get; private set; }
        public Collider2D BodyCollider => bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
        public bool ExternalControl { get; set; }

        /// <summary>-1 sol, +1 sağ. Görsel ve kılıç yönü için.</summary>
        public int Facing { get; private set; } = 1;

        void Awake()
        {
            if (tuning == null) Debug.LogError("[PlayerMotor] tuning atanmamış, oyuncu hareket etmeyecek.", this);
            if (input == null) Debug.LogError("[PlayerMotor] input atanmamış.", this);
            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.gravityScale = 0f;
            movementFilter = new ContactFilter2D();
            movementFilter.SetLayerMask(LayerMask.GetMask("Ground"));
            movementFilter.useTriggers = false;
            groundFilter = new ContactFilter2D();
            groundFilter.SetLayerMask(LayerMask.GetMask("Ground"));
            groundFilter.useTriggers = false;
        }

        void FixedUpdate()
        {
            if (body == null || tuning == null || bodyCollider == null) return;
            float dt = Time.fixedDeltaTime;
            IsGrounded = CheckGrounded(body.position);

            if (ExternalControl)
            {
                // Zıplama basışı tüketilmez: çekiş bitip fırlama penceresi açılınca tampondan okunur.
                jumpGate.Tick(dt, false, false, tuning.CoyoteTime, tuning.JumpBufferTime);
                Vector2 externalEnd = MoveWithCollisions(body.position, Velocity * dt);
                body.MovePosition(externalEnd);
                IsGrounded = CheckGrounded(externalEnd);
                return;
            }

            float moveX = input != null ? input.MoveX : 0f;
            if (moveX != 0f) Facing = moveX < 0f ? -1 : 1;
            bool jumpPressed = input != null && input.ConsumeJump(tuning.JumpBufferTime);
            jumpGate.Tick(dt, IsGrounded, jumpPressed, tuning.CoyoteTime, tuning.JumpBufferTime);

            Vector2 velocity = Velocity;
            velocity.x = MovementMath.StepHorizontal(velocity.x, moveX, IsGrounded, dt,
                tuning.MaxRunSpeed, tuning.RunAccel, tuning.RunReduce, tuning.AirMult, tuning.OverSpeedDecel);

            if (IsGrounded && velocity.y < 0f) velocity.y = 0f;
            if (jumpGate.TryConsumeJump(out bool fromLaunchWindow))
            {
                velocity = MovementMath.ApplyJump(velocity, fromLaunchWindow, tuning.JumpSpeed,
                    tuning.LaunchJumpBonus, tuning.JumpHBoost, moveX);
                variableJumpRemaining = tuning.VarJumpTime;
            }

            bool jumpHeld = input != null && input.JumpHeld;
            velocity.y = MovementMath.StepGravity(velocity.y, dt, jumpHeld,
                tuning.Gravity, tuning.HalfGravThreshold, tuning.MaxFallSpeed);
            if (variableJumpRemaining > 0f)
            {
                velocity.y = MovementMath.CutVariableJump(velocity.y, jumpHeld, variableJumpRemaining);
                if (jumpHeld && velocity.y > 0f) velocity.y = Mathf.Max(velocity.y, tuning.JumpSpeed);
                variableJumpRemaining = Mathf.Max(0f, variableJumpRemaining - dt);
            }

            Velocity = velocity;
            Vector2 end = MoveWithCollisions(body.position, Velocity * dt);
            body.MovePosition(end);
            IsGrounded = CheckGrounded(end);
            if (IsGrounded && Velocity.y < 0f) Velocity = new Vector2(Velocity.x, 0f);
        }

        public void OpenLaunchJumpWindow(float seconds)
        {
            jumpGate.OpenLaunchWindow(seconds);
        }

        // Tüm sorgular hesaplanan konumdan yapılır, gövde adım sonunda tek MovePosition ile taşınır.
        // body.position'a doğrudan yazmak ışınlama sayılır ve interpolation'ı bozar.
        int CastFrom(Vector2 position, Vector2 direction, float distance) =>
            bodyCollider.Cast(position, body.rotation, direction, movementFilter, castHits, distance, true);

        bool CheckGrounded(Vector2 position)
        {
            int count = bodyCollider.Cast(position, body.rotation, Vector2.down, groundFilter, castHits,
                tuning.GroundCheckDistance, true);
            gizmoGroundEnd = position + Vector2.down * tuning.GroundCheckDistance;
            for (int i = 0; i < count; i++)
                if (castHits[i].collider != null && castHits[i].distance <= tuning.GroundCheckDistance)
                    return true;
            return false;
        }

        Vector2 MoveWithCollisions(Vector2 position, Vector2 displacement)
        {
            Vector2 remaining = displacement;
            float minimumDistance = Mathf.Max(tuning.MinimumMoveDistance, 0f);
            for (int i = 0; i < tuning.SlideIterations && remaining.sqrMagnitude > minimumDistance * minimumDistance; i++)
            {
                float distance = remaining.magnitude;
                Vector2 direction = remaining / distance;
                RaycastHit2D hit = ClosestHit(CastFrom(position, direction, distance));
                if (hit.collider == null || hit.distance >= distance)
                    return position + remaining;

                float travel = Mathf.Max(0f, hit.distance - tuning.CollisionSkin);
                position += direction * travel;
                remaining *= distance > 0f ? (distance - travel) / distance : 0f;

                if (hit.normal.y < -tuning.CeilingNormalThreshold && remaining.y > 0f
                    && TryCornerCorrection(ref position, remaining.y))
                    continue;

                float intoSurface = Vector2.Dot(remaining, hit.normal);
                if (intoSurface < 0f) remaining -= hit.normal * intoSurface;
                float velocityIntoSurface = Vector2.Dot(Velocity, hit.normal);
                if (velocityIntoSurface < 0f) Velocity -= hit.normal * velocityIntoSurface;
            }
            return position;
        }

        RaycastHit2D ClosestHit(int count)
        {
            RaycastHit2D closest = default;
            for (int i = 0; i < count; i++)
                if (castHits[i].collider != null && (closest.collider == null || castHits[i].distance < closest.distance))
                    closest = castHits[i];
            return closest;
        }

        bool TryCornerCorrection(ref Vector2 position, float upwardDistance)
        {
            float step = 1f / Mathf.Max(tuning.PixelsPerUnit, 1f);
            int steps = Mathf.CeilToInt(tuning.CornerCorrectionPx);
            for (int i = 1; i <= steps; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 offset = Vector2.right * (side * step * i);
                    if (ClosestHit(CastFrom(position, offset.normalized, offset.magnitude)).collider != null) continue;
                    Vector2 shifted = position + offset;
                    if (ClosestHit(CastFrom(shifted, Vector2.up, upwardDistance)).collider == null)
                    {
                        position = shifted;
                        return true;
                    }
                }
            }
            return false;
        }

        void OnDrawGizmos()
        {
            Collider2D col = BodyCollider;
            if (col == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawLine(Position, gizmoGroundEnd);
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(Position, Position + Velocity * (tuning != null ? tuning.VelocityGizmoSeconds : 0f));
        }
    }
}
