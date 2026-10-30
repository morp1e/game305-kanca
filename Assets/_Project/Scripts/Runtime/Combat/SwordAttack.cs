using System.Collections.Generic;
using Game.Core;
using Game.Dialogue;
using Game.Input;
using Game.Player;
using UnityEngine;

namespace Game.Combat
{
    public class SwordAttack : MonoBehaviour
    {
        [SerializeField] SwordTuning tuning;
        [SerializeField] PlayerInputReader input;
        [SerializeField] PlayerArmRig armRig;
        [SerializeField] PlayerMotor body;
        [SerializeField] Camera aimCamera;
        [SerializeField] FloatEvent traumaEvent;
        [SerializeField] SwordVoiceEvents voiceEvents;
        readonly HashSet<int> hitIds = new HashSet<int>();
        readonly Collider2D[] hits = new Collider2D[32];
        readonly List<FrozenTarget> frozenTargets = new List<FrozenTarget>(4);
        float elapsed, freezeRemaining, aimAngle, lastSwingStart, lastSwingEnd, previousElapsed;
        double recoveryStartedAt = double.NegativeInfinity;
        int swingDirection = 1;
        bool swinging, ownsBodyFreeze, bodyWasExternallyControlled, hasPreviousSwing;
        bool bodyWasSimulated;
        Vector2 velocityBeforeFreeze;
        Rigidbody2D playerRigidbody;
        ContactFilter2D enemyFilter;
        public bool IsSwinging => swinging;
        public event System.Action<bool> HitResolved;

        sealed class FrozenTarget
        {
            public Rigidbody2D body;
            public Vector2 velocity;
            public float angularVelocity;
            public bool wasSimulated;
            public Vector2 knockback;
        }

        void Awake()
        {
            if (body != null) playerRigidbody = body.GetComponent<Rigidbody2D>();
            enemyFilter = new ContactFilter2D();
            enemyFilter.SetLayerMask(LayerMask.GetMask("Enemy"));
            enemyFilter.useTriggers = true;
        }

        void FixedUpdate()
        {
            if (tuning == null || input == null || body == null) return;
            float dt = Time.fixedDeltaTime;
            if (freezeRemaining > 0f)
            {
                freezeRemaining = Mathf.Max(0f, freezeRemaining - dt);
                if (freezeRemaining <= 0f) RestoreAfterHitstop();
                else return;
            }

            if (!swinging)
            {
                if ((!hasPreviousSwing || CanStartSwing(Time.time, lastSwingStart, tuning.swingInterval)) && input.ConsumeAttack(tuning.inputBuffer)) BeginSwing();
                return;
            }

            previousElapsed = elapsed;
            elapsed += dt;
            float windup = Mathf.Max(0f, tuning.windupSeconds);
            float active = Mathf.Max(0.001f, tuning.activeSeconds);
            float recovery = Mathf.Max(0f, tuning.recoverySeconds);
            float end = windup + active + recovery;
            int phase = PhaseFromTime(elapsed, windup, active, recovery);
            if (phase == 2 && recoveryStartedAt == double.NegativeInfinity) recoveryStartedAt = Time.realtimeSinceStartupAsDouble;
            if (phase == 2 && input.ConsumeJumpForAction(tuning.inputBuffer, recoveryStartedAt)) elapsed = Mathf.Max(elapsed, end);
            if (elapsed < windup + active)
            {
                float angle = AngleAtTime(aimAngle, tuning.swingArcDegrees, elapsed, windup, active, swingDirection, tuning.windupPullbackDegrees);
                armRig?.SetSwordArmOverride(angle);
                SweepHit(previousElapsed, elapsed, windup, active);
            }
            else if (elapsed < end)
            {
                armRig?.SetSwordArmOverride(SwingAngle(aimAngle, tuning.swingArcDegrees, 1f, swingDirection));
            }
            if (elapsed >= end) EndSwing();
        }

        void BeginSwing()
        {
            Vector2 pivot = PivotPosition();
            Vector2 delta = input.PointerWorld(aimCamera) - pivot;
            aimAngle = delta.sqrMagnitude > Mathf.Epsilon ? Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg : (body.Facing > 0 ? 0f : 180f);
            float sincePreviousEnd = Time.time - lastSwingEnd;
            bool alternate = hasPreviousSwing && sincePreviousEnd >= 0f && sincePreviousEnd <= tuning.comboWindow;
            swingDirection = ComboDirection(delta.x, alternate, swingDirection);
            elapsed = previousElapsed = 0f;
            recoveryStartedAt = double.NegativeInfinity;
            hitIds.Clear();
            swinging = true;
            lastSwingStart = Time.time;
            hasPreviousSwing = true;
            armRig?.SetSwordArmOverride(AngleAtTime(aimAngle, tuning.swingArcDegrees, 0f, tuning.windupSeconds, tuning.activeSeconds, swingDirection, tuning.windupPullbackDegrees));
        }

        void EndSwing()
        {
            swinging = false;
            lastSwingEnd = Time.time;
            armRig?.SetSwordArmOverride(null);
            if (hitIds.Count == 0) voiceEvents?.Miss();
        }

        void SweepHit(float fromTime, float toTime, float windup, float active)
        {
            float activeStart = windup;
            float activeEnd = windup + active;
            float from = Mathf.Max(fromTime, activeStart);
            float to = Mathf.Min(toTime, activeEnd);
            if (to < from) return;
            float fromProgress = (from - activeStart) / active;
            float toProgress = (to - activeStart) / active;
            List<float> angles = SweepSampleAngles(aimAngle, tuning.swingArcDegrees, fromProgress, toProgress, swingDirection, tuning.maxSweepStepDegrees);
            for (int i = 0; i < angles.Count; i++) CheckBladeAt(angles[i]);
        }

        void CheckBladeAt(float angle)
        {
            Vector2 start, tip;
            BladeEndpoints(PivotPosition(), angle, tuning.bladeLength, out start, out tip);
            Vector2 center = (start + tip) * 0.5f;
            Vector2 size = new Vector2(tuning.bladeLength + tuning.bladeRadius * 2f, tuning.bladeRadius * 2f);
            int count = Physics2D.OverlapCapsule(center, size, CapsuleDirection2D.Horizontal, angle, enemyFilter, hits);
            for (int i = 0; i < count; i++)
            {
                var behaviours = hits[i] != null ? hits[i].GetComponentsInParent<MonoBehaviour>() : System.Array.Empty<MonoBehaviour>();
                MonoBehaviour target = null;
                IDamageable damageable = null;
                for (int b = 0; b < behaviours.Length; b++)
                    if (behaviours[b] is IDamageable found) { target = behaviours[b]; damageable = found; break; }
                if (target == null || !hitIds.Add(target.GetInstanceID())) continue;
                Vector2 knockDirection = (new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad)) + Vector2.up * tuning.knockbackUpBias).normalized;
                damageable.TakeHit(new HitInfo(tuning.damage, knockDirection, tuning.knockback, this));
                var targetBody = target.GetComponentInParent<Rigidbody2D>();
                FreezeTarget(targetBody, knockDirection * tuning.knockback);
                traumaEvent?.Raise(tuning.traumaOnHit);
                voiceEvents?.Hit();
                HitResolved?.Invoke(true);
            }
        }

        void FreezeTarget(Rigidbody2D targetBody, Vector2 knockback)
        {
            if (tuning.hitstop > 0f)
            {
                freezeRemaining = Mathf.Max(freezeRemaining, tuning.hitstop);
                if (!ownsBodyFreeze)
                {
                    velocityBeforeFreeze = body.Velocity;
                    bodyWasExternallyControlled = body.ExternalControl;
                    if (playerRigidbody != null) bodyWasSimulated = playerRigidbody.simulated;
                    body.ExternalControl = true;
                    body.Velocity = Vector2.zero;
                    if (playerRigidbody != null) playerRigidbody.simulated = false;
                    ownsBodyFreeze = true;
                }
            }
            if (targetBody == null) return;
            for (int i = 0; i < frozenTargets.Count; i++)
                if (frozenTargets[i].body == targetBody) { frozenTargets[i].knockback = knockback; return; }
            var frozen = new FrozenTarget { body = targetBody, velocity = targetBody.linearVelocity, angularVelocity = targetBody.angularVelocity, wasSimulated = targetBody.simulated, knockback = knockback };
            frozenTargets.Add(frozen);
            if (tuning.hitstop > 0f) { targetBody.linearVelocity = Vector2.zero; targetBody.angularVelocity = 0f; targetBody.simulated = false; }
            else ApplyKnockback(frozen);
        }

        void RestoreAfterHitstop()
        {
            if (ownsBodyFreeze && body != null)
            {
                if (playerRigidbody != null) playerRigidbody.simulated = bodyWasSimulated;
                body.Velocity = velocityBeforeFreeze;
                body.ExternalControl = bodyWasExternallyControlled;
                ownsBodyFreeze = false;
            }
            for (int i = 0; i < frozenTargets.Count; i++)
            {
                var target = frozenTargets[i];
                if (target.body == null) continue;
                target.body.simulated = target.wasSimulated;
                target.body.linearVelocity = target.velocity;
                target.body.angularVelocity = target.angularVelocity;
                ApplyKnockback(target);
            }
            frozenTargets.Clear();
        }

        static void ApplyKnockback(FrozenTarget target)
        {
            if (target.body != null && target.body.bodyType == RigidbodyType2D.Dynamic) target.body.AddForce(target.knockback, ForceMode2D.Impulse);
        }

        Vector2 PivotPosition() => armRig != null && armRig.SwordArmPivot != null ? (Vector2)armRig.SwordArmPivot.position : body.Position;
        public static int PhaseFromTime(float time, float windup, float active, float recovery)
        {
            if (time < windup) return 0;
            if (time < windup + active) return 1;
            if (time < windup + active + recovery) return 2;
            return 3;
        }

        public static float SwingAngle(float aimAngle, float arcDegrees, float progress, int direction)
        {
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
            float half = arcDegrees * 0.5f;
            return direction >= 0 ? aimAngle + half - arcDegrees * eased : aimAngle - half + arcDegrees * eased;
        }

        public static float AngleAtTime(float aim, float arc, float time, float windup, float active, int direction, float pullback)
        {
            if (time < windup) return SwingAngle(aim, arc, 0f, direction) + direction * pullback * (1f - Mathf.SmoothStep(0f, 1f, windup <= 0f ? 1f : Mathf.Clamp01(time / windup)));
            return SwingAngle(aim, arc, (time - windup) / Mathf.Max(active, Mathf.Epsilon), direction);
        }

        public static void BladeEndpoints(Vector2 pivot, float angle, float length, out Vector2 start, out Vector2 tip)
        {
            start = pivot;
            tip = pivot + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * length;
        }

        public static List<float> SweepSampleAngles(float aim, float arc, float fromProgress, float toProgress, int direction, float maxStepDegrees)
        {
            float span = Mathf.Abs(toProgress - fromProgress) * arc;
            int steps = Mathf.Max(1, Mathf.CeilToInt(span * 1.5f / Mathf.Max(0.1f, maxStepDegrees)));
            var result = new List<float>(steps + 1);
            for (int i = 0; i <= steps; i++) result.Add(SwingAngle(aim, arc, Mathf.Lerp(fromProgress, toProgress, i / (float)steps), direction));
            return result;
        }

        public static int ComboDirection(float mouseDeltaX, bool alternate, int previousDirection)
        {
            int aimed = mouseDeltaX >= 0f ? 1 : -1;
            return alternate ? -previousDirection : aimed;
        }

        public static bool CanStartSwing(float now, float previousStart, float interval) => now - previousStart >= interval - 0.0001f;

        void OnDisable()
        {
            armRig?.SetSwordArmOverride(null);
            if (ownsBodyFreeze && body != null) { if (playerRigidbody != null) playerRigidbody.simulated = bodyWasSimulated; body.Velocity = velocityBeforeFreeze; body.ExternalControl = bodyWasExternallyControlled; ownsBodyFreeze = false; }
            for (int i = 0; i < frozenTargets.Count; i++)
            {
                var target = frozenTargets[i];
                if (target.body != null) { target.body.simulated = target.wasSimulated; target.body.linearVelocity = target.velocity; target.body.angularVelocity = target.angularVelocity; ApplyKnockback(target); }
            }
            frozenTargets.Clear();
        }

        void OnDrawGizmosSelected()
        {
            if (tuning == null) return;
            float angle = swinging ? AngleAtTime(aimAngle, tuning.swingArcDegrees, elapsed, tuning.windupSeconds, tuning.activeSeconds, swingDirection, tuning.windupPullbackDegrees) : aimAngle;
            Vector2 start, tip;
            BladeEndpoints(armRig != null && armRig.SwordArmPivot != null ? (Vector2)armRig.SwordArmPivot.position : (Vector2)transform.position, angle, tuning.bladeLength, out start, out tip);
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(start, tip);
            Gizmos.DrawWireSphere(start, tuning.bladeRadius);
            Gizmos.DrawWireSphere(tip, tuning.bladeRadius);
        }
    }
}
