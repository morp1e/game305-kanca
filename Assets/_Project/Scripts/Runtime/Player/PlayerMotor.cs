using System.Collections.Generic;
using Game.Core;
using Game.Input;
using UnityEngine;

namespace Game.Player
{
    // Celeste Player.cs (MIT, (c) 2018 Noel Berry) uyarlaması; bkz. tools/reference/celeste/LICENSE
    /// <summary>
    /// Oyuncu hareketi. Gövde Dynamic: çarpışmayı ve zeminden ayırmayı Unity'nin fizik motoru çözer,
    /// biz yalnız hızı belirleriz (yerçekimi dahil, gravityScale 0). Kendi Kinematic çarpışma kodumuz
    /// temas payı yüzünden birkaç saniye sonra zeminin içinden geçiyordu (PlayerPhysicsTests).
    /// Çalışma sırası 0: GrappleHook (-10) Velocity'yi önce yazar, motor sonra işler.
    /// </summary>
    [DefaultExecutionOrder(0)]
    [RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
    public class PlayerMotor : MonoBehaviour, IPlayerBody
    {
        [SerializeField] PlayerMovementTuning tuning;
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerArmRig armRig;

        Rigidbody2D body;
        Collider2D bodyCollider;
        readonly List<RaycastHit2D> castHits = new List<RaycastHit2D>(8);
        ContactFilter2D solidFilter;
        ContactFilter2D groundFilter;
        readonly JumpGate jumpGate = new JumpGate();
        float variableJumpRemaining;
        float variableJumpSpeed;
        float fastFallCap;
        float wallSlideRemaining;
        float forcedMoveRemaining;
        float retainedWallSpeed;
        float wallSpeedRemaining;
        float launchMomentumRemaining;
        float lastWrittenHorizontalSpeed;
        float knockbackControlLockRemaining;

        public Vector2 Position => body != null ? body.position : (Vector2)transform.position;

        /// <summary>Tek doğru kaynak gövdenin hızıdır; duvar ve tavan onu fizik adımında keser.</summary>
        public Vector2 Velocity
        {
            get => body != null ? body.linearVelocity : Vector2.zero;
            set { if (body != null) body.linearVelocity = value; }
        }

        public bool IsGrounded { get; private set; }
        public Collider2D BodyCollider => bodyCollider != null ? bodyCollider : GetComponent<Collider2D>();
        public bool ExternalControl { get; set; }

        /// <summary>-1 sol, +1 sağ. Görsel ve kılıç yönü için.</summary>
        public int Facing => armRig != null ? armRig.Facing : movementFacing;
        int movementFacing = 1;

        void Awake()
        {
            if (tuning == null) Debug.LogError("[PlayerMotor] tuning atanmamış, oyuncu hareket etmeyecek.", this);
            if (input == null) Debug.LogError("[PlayerMotor] input atanmamış.", this);

            body = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<Collider2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            body.sleepMode = RigidbodySleepMode2D.NeverSleep;
            // Sürtünme olursa duvara yaslanınca karakter yapışır.
            bodyCollider.sharedMaterial = new PhysicsMaterial2D("PlayerNoFriction") { friction = 0f, bounciness = 0f };

            solidFilter = new ContactFilter2D();
            solidFilter.SetLayerMask(LayerMask.GetMask("Ground"));
            solidFilter.useTriggers = false;
            // Zemin = yukarı bakan yüzey. Duvara sürtünmek zeminde sayılmaz.
            groundFilter = solidFilter;
            groundFilter.SetNormalAngle(45f, 135f);
        }

        void FixedUpdate()
        {
            if (body == null || tuning == null || bodyCollider == null) return;
            float dt = Time.fixedDeltaTime;
            IsGrounded = CheckGrounded();
            if (IsGrounded) wallSlideRemaining = tuning.WallSlideTime;
            wallSpeedRemaining = Mathf.Max(0f, wallSpeedRemaining - dt);
            launchMomentumRemaining = Mathf.Max(0f, launchMomentumRemaining - dt);
            forcedMoveRemaining = Mathf.Max(0f, forcedMoveRemaining - dt);

            if (knockbackControlLockRemaining > 0f)
            {
                knockbackControlLockRemaining = Mathf.Max(0f, knockbackControlLockRemaining - dt);
                Vector2 knockbackVelocity = Velocity;
                if (!IsGrounded)
                    knockbackVelocity.y = MovementMath.StepGravity(knockbackVelocity.y, dt, false,
                        tuning.Gravity, tuning.HalfGravThreshold, tuning.MaxFallSpeed);
                else if (knockbackVelocity.y < 0f) knockbackVelocity.y = 0f;
                Velocity = knockbackVelocity;
                return;
            }

            if (ExternalControl)
            {
                // Hızı kanca belirler. Zıplama basışı tüketilmez: fırlama penceresi açılınca tampondan okunur.
                jumpGate.Tick(dt, false, false, tuning.CoyoteTime, tuning.JumpBufferTime);
                return;
            }

            float moveX = input != null ? input.MoveX : 0f;
            if (forcedMoveRemaining > 0f) moveX = forcedMoveRemaining > 0f ? forcedMoveDirection : moveX;
            if (moveX != 0f) movementFacing = moveX < 0f ? -1 : 1;
            bool jumpPressed = input != null && input.ConsumeJump(tuning.JumpBufferTime);
            jumpGate.Tick(dt, IsGrounded, jumpPressed, tuning.CoyoteTime, tuning.JumpBufferTime);

            Vector2 velocity = Velocity;
            if (wallSpeedRemaining > 0f && retainedWallSpeed != 0f &&
                Mathf.Sign(velocity.x) == Mathf.Sign(retainedWallSpeed) && !IsWallInDirection(Mathf.Sign(retainedWallSpeed)))
            {
                velocity.x = retainedWallSpeed;
                wallSpeedRemaining = 0f;
            }
            velocity.x = MovementMath.StepHorizontal(velocity.x, moveX, IsGrounded, dt,
                tuning.MaxRunSpeed, tuning.RunAccel, tuning.RunReduce, tuning.AirMult, tuning.OverSpeedDecel,
                launchMomentumRemaining > 0f);

            if (IsGrounded && velocity.y < 0f) velocity.y = 0f;
            if (jumpGate.TryConsumeJump(out bool fromLaunchWindow))
            {
                velocity = MovementMath.ApplyJump(velocity, fromLaunchWindow, tuning.JumpSpeed,
                    tuning.LaunchJumpBonus, tuning.JumpHBoost, moveX);
                variableJumpRemaining = tuning.VarJumpTime;
                variableJumpSpeed = velocity.y;
                IsGrounded = false;
            }

            if (!jumpGate.LaunchWindowActive && tuning.WallJumpEnabled && !IsGrounded && velocity.y <= 0f &&
                moveX != 0f && IsWallInDirection(moveX))
            {
                velocity.y = Mathf.Max(velocity.y, -MovementMath.WallSlideCap(tuning.MaxFallSpeed,
                    tuning.WallSlideStartMax, wallSlideRemaining, tuning.WallSlideTime));
                wallSlideRemaining = Mathf.Max(0f, wallSlideRemaining - dt);
            }

            if (jumpGate.LaunchWindowActive == false && tuning.WallJumpEnabled &&
                !IsGrounded && TryGetWallDirection(moveX, out int wallDirection))
            {
                if (jumpGate.TryConsumeWallJump())
                {
                    int away = -wallDirection;
                    velocity.x = tuning.WallJumpHSpeed * away;
                    velocity.y = tuning.JumpSpeed;
                    variableJumpRemaining = tuning.VarJumpTime;
                    variableJumpSpeed = velocity.y;
                    forcedMoveDirection = away;
                    forcedMoveRemaining = tuning.WallJumpForceTime;
                    wallSlideRemaining = tuning.WallSlideTime;
                    IsGrounded = false;
                }
            }

            bool jumpHeld = input != null && input.JumpHeld;
            // Yerdeyken yerçekimi uygulanmaz; uygulansa her adım zemine bastırıp temas titretir.
            if (!IsGrounded)
            {
                bool fastFall = input != null && input.DownHeld && velocity.y <= -tuning.MaxFallSpeed;
                fastFallCap = MovementMath.Approach(fastFallCap <= 0f ? tuning.MaxFallSpeed : fastFallCap,
                    fastFall ? tuning.FastMaxFallSpeed : tuning.MaxFallSpeed,
                    tuning.FastMaxAccel * dt);
                bool sliding = moveX != 0f && IsWallInDirection(moveX) && velocity.y <= 0f && !(input != null && input.DownHeld);
                float cap = sliding ? MovementMath.WallSlideCap(tuning.MaxFallSpeed, tuning.WallSlideStartMax,
                    wallSlideRemaining, tuning.WallSlideTime) : fastFallCap;
                velocity.y = MovementMath.StepGravity(velocity.y, dt, jumpHeld,
                    tuning.Gravity, tuning.HalfGravThreshold, cap);
            }
            if (variableJumpRemaining > 0f)
            {
                if (jumpHeld)
                {
                    velocity.y = MovementMath.ApplyVariableJump(velocity.y, true, variableJumpSpeed, variableJumpRemaining);
                    variableJumpRemaining = Mathf.Max(0f, variableJumpRemaining - dt);
                }
                else variableJumpRemaining = 0f;
            }

            if (velocity.y > 0f) TryCornerCorrection(velocity.y * dt);
            Velocity = velocity;
            lastWrittenHorizontalSpeed = velocity.x;
        }

        int forcedMoveDirection;

        bool IsWallInDirection(float direction)
        {
            if (direction == 0f) return false;
            return body.Cast(Vector2.right * Mathf.Sign(direction), solidFilter, castHits, tuning.WallJumpCheckDist) > 0;
        }

        bool TryGetWallDirection(float moveX, out int direction)
        {
            int preferred = moveX == 0f ? movementFacing : (moveX > 0f ? 1 : -1);
            if (IsWallInDirection(preferred)) { direction = preferred; return true; }
            if (IsWallInDirection(-preferred)) { direction = -preferred; return true; }
            direction = preferred;
            return false;
        }

        void OnCollisionEnter2D(Collision2D collision)
        {
            if (collision.contactCount == 0) return;
            ContactPoint2D contact = collision.GetContact(0);
            if (Mathf.Abs(contact.normal.x) > 0.5f && wallSpeedRemaining <= 0f)
            {
                retainedWallSpeed = lastWrittenHorizontalSpeed;
                wallSpeedRemaining = tuning != null ? tuning.WallSpeedRetentionTime : 0f;
            }
            if (contact.normal.y < -0.5f && variableJumpRemaining < tuning.VarJumpTime - tuning.CeilingVarJumpGrace)
                variableJumpRemaining = 0f;
        }

        public void OpenLaunchJumpWindow(float seconds)
        {
            jumpGate.OpenLaunchWindow(seconds);
            launchMomentumRemaining = Mathf.Max(launchMomentumRemaining, seconds);
        }

        public void ApplyKnockback(Vector2 velocity, float controlLockSeconds)
        {
            ExternalControl = false;
            Velocity = velocity;
            knockbackControlLockRemaining = Mathf.Max(knockbackControlLockRemaining, Mathf.Max(0f, controlLockSeconds));
            variableJumpRemaining = 0f;
            forcedMoveRemaining = 0f;
        }

        bool CheckGrounded()
        {
            // Yükselirken zemine değmek (platformun kenarından geçmek) iniş sayılmaz.
            if (body.linearVelocity.y > 0.01f) return false;
            if (body.IsTouching(groundFilter)) return true;
            int count = body.Cast(Vector2.down, groundFilter, castHits, tuning.GroundCheckDistance);
            return count > 0;
        }

        /// <summary>Kafa bir köşeye birkaç piksel takılacaksa gövdeyi yana kaydırır (Celeste).</summary>
        void TryCornerCorrection(float upwardDistance)
        {
            if (body.Cast(Vector2.up, solidFilter, castHits, upwardDistance) == 0) return;
            const float celesteUnitsPerTile = 8f;
            float step = 1f / celesteUnitsPerTile;
            int steps = Mathf.CeilToInt(tuning.CornerCorrectionPx);
            for (int i = 1; i <= steps; i++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    Vector2 offset = new Vector2(side * step * i, 0f);
                    if (body.Cast(offset.normalized, solidFilter, castHits, offset.magnitude) > 0) continue;
                    Vector2 shifted = body.position + offset;
                    if (bodyCollider.Cast(shifted, body.rotation, Vector2.up, solidFilter, castHits, upwardDistance, true) > 0) continue;
                    body.position = shifted;
                    return;
                }
            }
        }

        void OnDrawGizmos()
        {
            Collider2D col = BodyCollider;
            if (col == null) return;
            Gizmos.color = IsGrounded ? Color.green : Color.yellow;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(Position, Position + Velocity * (tuning != null ? tuning.VelocityGizmoSeconds : 0f));
        }
    }
}
