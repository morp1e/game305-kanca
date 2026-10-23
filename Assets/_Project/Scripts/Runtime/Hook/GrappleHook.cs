using System;
using Game.Core;
using Game.Input;
using Game.Player;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Hook
{
    /// <summary>Sol eldeki kancanın uçuş, saplanma, çekiş ve geri çağırma durum makinesi.</summary>
    [DefaultExecutionOrder(-10)]
    public class GrappleHook : MonoBehaviour
    {
        [SerializeField] GrappleHookTuning tuning;
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerMotor body;
        [SerializeField, FormerlySerializedAs("sword")] Transform hookVisual;
        [SerializeField] Camera aimCamera;
        [SerializeField] FloatEvent traumaEvent;
        [SerializeField] Transform reticle;

        public HookState State { get; private set; } = HookState.InHand;
        public Vector2 AimPoint => input != null && aimCamera != null ? input.PointerWorld(aimCamera) : (Vector2)transform.position;
        public Vector2 ActivePoint { get; private set; }
        public Vector2 ActiveNormal { get; private set; }
        public event Action<HookState> StateChanged;

        readonly RaycastHit2D[] pullHits = new RaycastHit2D[8];
        Collider2D activeSurface;
        Vector2 throwDirection;
        Vector2 pullDirection;
        float flightDistance;
        float pullStartDistance;
        float pullElapsed;
        float freezeRemaining;
        float cooldownRemaining;
        ContactFilter2D groundFilter;

        void Awake()
        {
            if (tuning == null || input == null || body == null || hookVisual == null || aimCamera == null)
                Debug.LogError("[GrappleHook] tuning, input, body, hookVisual ve aimCamera atanmalı; kanca çalışmayacak.", this);
            groundFilter = new ContactFilter2D();
            groundFilter.SetLayerMask(LayerMask.GetMask("Ground"));
            groundFilter.useTriggers = false;
            if (hookVisual != null) hookVisual.position = HandPosition();
        }

        void Update()
        {
            UpdateReticle();
        }

        void FixedUpdate()
        {
            if (tuning == null || input == null || body == null || hookVisual == null) return;
            var dt = Time.fixedDeltaTime;
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - dt);
            if (input.ConsumeThrow(tuning.inputBufferTime))
            {
                if (State == HookState.InHand && cooldownRemaining <= 0f) FireHook();
                else if (State == HookState.Thrown || State == HookState.Stuck || State == HookState.Pulling) EnterReturning();
            }

            switch (State)
            {
                case HookState.InHand: TickInHand(); break;
                case HookState.Thrown: TickThrown(dt); break;
                case HookState.Stuck: TickStuck(dt); break;
                case HookState.Pulling: TickPulling(dt); break;
                case HookState.Launch: TickLaunch(); break;
                case HookState.Returning: TickReturning(dt); break;
            }
        }

        void TickInHand()
        {
            hookVisual.position = HandPosition();
            hookVisual.right = Vector2.right * bodyFacing();
        }

        void FireHook()
        {
            var origin = HandPosition();
            var delta = input.PointerWorld(aimCamera) - origin;
            if (delta.sqrMagnitude <= Mathf.Epsilon) delta = Vector2.right * bodyFacing();
            throwDirection = delta.normalized;
            flightDistance = 0f;
            hookVisual.position = origin;
            hookVisual.right = throwDirection;
            activeSurface = null;
            ChangeState(HookState.Thrown);
        }

        void TickThrown(float dt)
        {
            var distance = Mathf.Min(tuning.throwSpeed * dt, Mathf.Max(0f, tuning.maxRange - flightDistance));
            var hit = Physics2D.CircleCast(hookVisual.position, tuning.hookRadius, throwDirection, distance, tuning.hookableLayers);
            if (hit.collider != null)
            {
                hookVisual.position = hit.point;
                if (hit.collider.GetComponentInParent<NonHookable>() != null) EnterReturning();
                else Stick(hit);
                return;
            }

            hookVisual.position += (Vector3)(throwDirection * distance);
            hookVisual.right = throwDirection;
            flightDistance += distance;
            if (flightDistance >= tuning.maxRange) EnterReturning();
        }

        void Stick(RaycastHit2D hit)
        {
            activeSurface = hit.collider;
            ActivePoint = hit.point;
            ActiveNormal = hit.normal;
            hookVisual.position = ActivePoint;
            freezeRemaining = tuning.impactFreeze;
            body.ExternalControl = true;
            body.Velocity = Vector2.zero;
            traumaEvent?.Raise(tuning.traumaOnStick);
            ChangeState(HookState.Stuck);
        }

        void TickStuck(float dt)
        {
            if (activeSurface == null) { EnterReturning(); return; }
            body.ExternalControl = true;
            body.Velocity = Vector2.zero;
            freezeRemaining -= dt;
            if (freezeRemaining <= 0f)
            {
                pullStartDistance = Vector2.Distance(body.Position, ActivePoint);
                pullElapsed = 0f;
                pullDirection = (ActivePoint - body.Position).normalized;
                ChangeState(HookState.Pulling);
            }
        }

        void TickPulling(float dt)
        {
            if (activeSurface == null) { ChangeState(HookState.Launch); return; }
            body.ExternalControl = true;
            var offset = ActivePoint - body.Position;
            var remaining = offset.magnitude;
            if (remaining <= tuning.arriveDistance || pullElapsed >= tuning.maxPullTime)
            {
                ChangeState(HookState.Launch);
                return;
            }

            var direction = offset / remaining;
            pullDirection = direction;
            var progress = pullStartDistance <= Mathf.Epsilon ? 1f : 1f - remaining / pullStartDistance;
            var curveValue = tuning.pullSpeedCurve != null ? tuning.pullSpeedCurve.Evaluate(Mathf.Clamp01(progress)) : 1f;
            var speed = PullMath.GetPullSpeed(tuning.pullSpeed, curveValue, body.Velocity, direction, tuning.keepFasterIncomingSpeed);
            var castDistance = speed * dt;
            var hitCount = body.BodyCollider.Cast(direction, groundFilter, pullHits, castDistance);
            if (hitCount > 0)
            {
                var surfaceHit = false;
                for (var i = 0; i < hitCount; i++)
                    if (pullHits[i].collider == activeSurface) { surfaceHit = true; break; }
                if (surfaceHit)
                {
                    ChangeState(HookState.Launch);
                    return;
                }

                var tangentVelocity = PullMath.ProjectOntoTangent(direction * speed, pullHits[0].normal);
                if (tangentVelocity.sqrMagnitude <= Mathf.Epsilon)
                {
                    ChangeState(HookState.Launch);
                    return;
                }
                body.Velocity = tangentVelocity;
            }
            else body.Velocity = direction * speed;

            pullElapsed += dt;
            if (pullElapsed >= tuning.maxPullTime) ChangeState(HookState.Launch);
        }

        void TickLaunch()
        {
            var direction = pullDirection.sqrMagnitude > 0f ? pullDirection : throwDirection;
            body.Velocity = PullMath.GetExitVelocity(direction, tuning.pullSpeed, tuning.exitSpeedMult, tuning.exitUpMult);
            body.ExternalControl = false;
            body.OpenLaunchJumpWindow(tuning.postLaunchJumpWindow);
            activeSurface = null;
            cooldownRemaining = tuning.hookCooldown;
            ChangeState(HookState.InHand);
        }

        void TickReturning(float dt)
        {
            var destination = HandPosition();
            var offset = destination - (Vector2)hookVisual.position;
            var step = tuning.recallSpeed * dt;
            if (offset.magnitude <= Mathf.Max(step, tuning.arriveDistance))
            {
                hookVisual.position = destination;
                body.ExternalControl = false;
                activeSurface = null;
                cooldownRemaining = tuning.hookCooldown;
                ChangeState(HookState.InHand);
                return;
            }
            var direction = offset.normalized;
            hookVisual.position += (Vector3)(direction * step);
            hookVisual.right = direction;
        }

        /// <summary>Kancayı anında ele alır, çekişi bırakır. Ölüm ve yeniden doğmada kullanılır.</summary>
        public void ResetToHand()
        {
            if (body != null) body.ExternalControl = false;
            activeSurface = null;
            cooldownRemaining = 0f;
            if (hookVisual != null) hookVisual.position = HandPosition();
            ChangeState(HookState.InHand);
        }

        void EnterReturning()
        {
            body.ExternalControl = false;
            activeSurface = null;
            ChangeState(HookState.Returning);
        }

        void ChangeState(HookState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }

        void UpdateReticle()
        {
            if (reticle == null || input == null || aimCamera == null) return;
            reticle.position = AimPoint;
        }

        Vector2 HandPosition()
        {
            if (body == null || tuning == null) return transform.position;
            return body.Position + new Vector2(tuning.throwOriginOffset.x * bodyFacing(), tuning.throwOriginOffset.y);
        }

        int bodyFacing() => body != null ? body.Facing : 1;

        void OnDrawGizmos()
        {
            if (tuning == null || body == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(body.Position, tuning.maxRange);
            if (State == HookState.Pulling)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(body.Position, ActivePoint);
            }
        }
    }
}
