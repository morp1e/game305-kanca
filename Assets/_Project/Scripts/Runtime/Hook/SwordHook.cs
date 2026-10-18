using System;
using Game.Core;
using Game.Input;
using Game.Player;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Hook
{
    /// <summary>
    /// Kılıç-kanca durum makinesi. SÖZLEŞME İSKELETİ: gövde Lane B tarafından yazılacak.
    /// Public API ve serileştirilmiş alan adları değişmez; eklenebilir.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class SwordHook : MonoBehaviour
    {
        [SerializeField] SwordHookTuning tuning;
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerMotor body;
        /// <summary>Kılıç görseli. Oyuncunun çocuğu değil, sahnede bağımsız bir obje olmalı.</summary>
        [SerializeField] Transform sword;
        [SerializeField] Camera aimCamera;
        [SerializeField] FloatEvent traumaEvent;
        [SerializeField] Transform reticle;

        public HookState State { get; private set; } = HookState.InHand;
        /// <summary>Nişan yardımının şu an seçtiği anchor; yoksa null.</summary>
        public HookAnchor AimTarget { get; private set; }

        public event Action<HookState> StateChanged;

        /// <summary>Kılıcın saplı olduğu anchor (Stuck/Pulling); yoksa null.</summary>
        public HookAnchor ActiveAnchor => activeAnchor;

        readonly List<AimAssist.Candidate> aimCandidates = new List<AimAssist.Candidate>();
        readonly RaycastHit2D[] pullHits = new RaycastHit2D[8];
        HookAnchor activeAnchor;
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
            if (tuning == null || input == null || body == null || sword == null || aimCamera == null)
                Debug.LogError("[SwordHook] tuning, input, body, sword ve aimCamera atanmalı; kanca çalışmayacak.", this);
            groundFilter = new ContactFilter2D();
            groundFilter.SetLayerMask(LayerMask.GetMask("Ground"));
            groundFilter.useTriggers = false;
            if (sword != null) sword.position = HandPosition();
        }

        void Update()
        {
            UpdateAimTarget();
            UpdateReticle();
        }

        void FixedUpdate()
        {
            if (tuning == null || input == null || body == null || sword == null) return;
            var dt = Time.fixedDeltaTime;
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - dt);
            if (input.ConsumeRecall(tuning.inputBufferTime) && State != HookState.InHand && State != HookState.Returning)
                EnterReturning();

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
            sword.position = HandPosition();
            sword.right = Vector2.right * bodyFacing();
            if (cooldownRemaining <= 0f && input.ConsumeThrow(tuning.inputBufferTime)) ThrowSword();
        }

        void ThrowSword()
        {
            var origin = HandPosition();
            var target = AimTarget != null ? AimTarget.Point : input.PointerWorld(aimCamera);
            var delta = target - origin;
            if (delta.sqrMagnitude <= Mathf.Epsilon) delta = Vector2.right * bodyFacing();
            throwDirection = delta.normalized;
            flightDistance = 0f;
            sword.position = origin;
            sword.right = throwDirection;
            SetActiveAnchor(null);
            ChangeState(HookState.Thrown);
        }

        void TickThrown(float dt)
        {
            var distance = Mathf.Min(tuning.throwSpeed * dt, Mathf.Max(0f, tuning.maxRange - flightDistance));
            var mask = LayerMask.GetMask("Anchor", "Ground");
            var hit = Physics2D.CircleCast(sword.position, tuning.swordRadius, throwDirection, distance, mask);
            if (hit.collider != null)
            {
                sword.position = hit.point;
                var anchor = hit.collider.GetComponentInParent<HookAnchor>();
                if (anchor != null) Stick(anchor);
                else EnterReturning();
                return;
            }

            sword.position += (Vector3)(throwDirection * distance);
            sword.right = throwDirection;
            flightDistance += distance;
            if (flightDistance >= tuning.maxRange) EnterReturning();
        }

        void Stick(HookAnchor anchor)
        {
            activeAnchor = anchor;
            sword.position = anchor.Point;
            SetActiveAnchor(anchor);
            freezeRemaining = tuning.impactFreeze;
            body.ExternalControl = true;
            body.Velocity = Vector2.zero;
            traumaEvent?.Raise(tuning.traumaOnStick);
            ChangeState(HookState.Stuck);
        }

        void TickStuck(float dt)
        {
            if (activeAnchor == null) { EnterReturning(); return; }
            body.ExternalControl = true;
            body.Velocity = Vector2.zero;
            freezeRemaining -= dt;
            if (freezeRemaining <= 0f)
            {
                pullStartDistance = Vector2.Distance(body.Position, activeAnchor.Point);
                pullElapsed = 0f;
                pullDirection = ((Vector2)activeAnchor.Point - body.Position).normalized;
                ChangeState(HookState.Pulling);
            }
        }

        void TickPulling(float dt)
        {
            if (activeAnchor == null) { EnterReturning(); return; }
            body.ExternalControl = true;
            var offset = activeAnchor.Point - body.Position;
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
                var tangentVelocity = PullMath.ProjectOntoTangent(direction * speed, pullHits[0].normal);
                if (tangentVelocity.sqrMagnitude <= Mathf.Epsilon)
                {
                    EnterReturning();
                    return;
                }
                body.Velocity = tangentVelocity;
            }
            else
            {
                body.Velocity = direction * speed;
            }

            pullElapsed += dt;
            if (pullElapsed >= tuning.maxPullTime) ChangeState(HookState.Launch);
        }

        void TickLaunch()
        {
            // Varışta kalan mesafe çok kısa, oradan hesaplanan yön titrer; son çekiş yönü kullanılır.
            var direction = pullDirection.sqrMagnitude > 0f ? pullDirection : throwDirection;
            body.Velocity = PullMath.GetExitVelocity(direction, tuning.pullSpeed, tuning.exitSpeedMult, tuning.exitUpMult);
            body.ExternalControl = false;
            body.OpenLaunchJumpWindow(tuning.postLaunchJumpWindow);
            SetActiveAnchor(null);
            cooldownRemaining = tuning.hookCooldown;
            ChangeState(HookState.InHand);
        }

        void TickReturning(float dt)
        {
            var destination = HandPosition();
            var offset = destination - (Vector2)sword.position;
            var step = tuning.recallSpeed * dt;
            if (offset.magnitude <= Mathf.Max(step, tuning.arriveDistance))
            {
                sword.position = destination;
                SetActiveAnchor(null);
                body.ExternalControl = false;
                activeAnchor = null;
                cooldownRemaining = tuning.hookCooldown;
                ChangeState(HookState.InHand);
                return;
            }
            var direction = offset.normalized;
            sword.position += (Vector3)(direction * step);
            sword.right = direction;
        }

        /// <summary>Kılıcı anında ele alır, çekişi bırakır. Ölüm ve yeniden doğmada kullanılır.</summary>
        public void ResetToHand()
        {
            if (body != null) body.ExternalControl = false;
            SetActiveAnchor(null);
            activeAnchor = null;
            cooldownRemaining = 0f;
            if (sword != null) sword.position = HandPosition();
            ChangeState(HookState.InHand);
        }

        void EnterReturning()
        {
            // Hız sıfırlanmaz: çekiş iptal edilince oyuncu o anki ivmesini korur, uçuşta geri çağırmak oyuncuyu etkilemez.
            body.ExternalControl = false;
            SetActiveAnchor(null);
            ChangeState(HookState.Returning);
        }

        void ChangeState(HookState next)
        {
            if (State == next) return;
            State = next;
            StateChanged?.Invoke(next);
        }

        void UpdateAimTarget()
        {
            if (tuning == null || body == null || aimCamera == null) return;
            aimCandidates.Clear();
            var overlaps = Physics2D.OverlapCircleAll(body.Position, tuning.maxRange, LayerMask.GetMask("Anchor"));
            var pixelScale = Screen.height / 1080f;
            var snapRadius = tuning.snapRadiusPx * pixelScale;
            var groundMask = LayerMask.GetMask("Ground");
            for (var i = 0; i < overlaps.Length; i++)
            {
                var anchor = overlaps[i].GetComponentInParent<HookAnchor>();
                if (anchor == null) continue;
                var point = anchor.Point;
                var delta = point - body.Position;
                var hit = Physics2D.Raycast(body.Position, delta.normalized, delta.magnitude, groundMask);
                var screen = (Vector2)aimCamera.WorldToScreenPoint(point);
                aimCandidates.Add(new AimAssist.Candidate(anchor.GetInstanceID(), screen, hit.collider == null, delta.magnitude));
            }

            var currentId = AimTarget != null ? AimTarget.GetInstanceID() : -1;
            var selectedId = AimAssist.Select(aimCandidates, input != null ? input.PointerScreen : Vector2.zero,
                snapRadius, tuning.maxRange, tuning.switchMargin, currentId);
            HookAnchor selected = null;
            for (var i = 0; i < overlaps.Length; i++)
            {
                var candidate = overlaps[i].GetComponentInParent<HookAnchor>();
                if (candidate != null && candidate.GetInstanceID() == selectedId) { selected = candidate; break; }
            }
            if (selected != AimTarget)
            {
                if (AimTarget != null && AimTarget != activeAnchor) AimTarget.SetVisualState(AnchorVisualState.Idle);
                AimTarget = selected;
                if (AimTarget != null && AimTarget != activeAnchor) AimTarget.SetVisualState(AnchorVisualState.Targeted);
            }
        }

        void UpdateReticle()
        {
            if (reticle == null || input == null) return;
            if (AimTarget != null) reticle.position = AimTarget.Point;
            else if (aimCamera != null) reticle.position = input.PointerWorld(aimCamera);
        }

        void SetActiveAnchor(HookAnchor anchor)
        {
            if (activeAnchor != null)
                activeAnchor.SetVisualState(activeAnchor == AimTarget ? AnchorVisualState.Targeted : AnchorVisualState.Idle);
            activeAnchor = anchor;
            if (activeAnchor != null) activeAnchor.SetVisualState(AnchorVisualState.Active);
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
            if (AimTarget != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(body.Position, AimTarget.Point);
            }
            if (State == HookState.Pulling && activeAnchor != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(body.Position, activeAnchor.Point);
            }
        }
    }
}
