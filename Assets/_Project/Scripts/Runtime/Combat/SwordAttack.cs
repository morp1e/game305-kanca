using System;
using System.Collections.Generic;
using Game.Core;
using Game.Dialogue;
using Game.Input;
using Game.Hook;
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
        [SerializeField] Vector2Event kickEvent;
        [SerializeField] SwordVoiceEvents voiceEvents;
        readonly HashSet<int> hitIds = new HashSet<int>();
        readonly Collider2D[] hits = new Collider2D[32];
        readonly List<PendingKnockback> pending = new List<PendingKnockback>(4);
        readonly ComboSequence combo = new ComboSequence();
        ContactFilter2D hitFilter;
        HitstopService hitstop;
        GrappleHook hook;
        float elapsed, aimAngle, previousElapsed;
        int stepIndex, swingDirection;
        double recoveryStartedAt = double.NegativeInfinity;
        bool swinging, waitingForHitstop, hookCancelPending;
        public bool IsSwinging => swinging;
        public bool IsSlashActive => swinging && elapsed >= CurrentTuning.windup &&
            elapsed < CurrentTuning.windup + CurrentTuning.active;
        public int CurrentStep => stepIndex;
        public event Action<int, Vector2, float, float, float, float, float> SlashStarted;
        public event Action<Vector2, Vector2, bool> HitOccurred;

        sealed class PendingKnockback { public Rigidbody2D body; public Vector2 impulse; }

        void Awake()
        {
            hitFilter = new ContactFilter2D(); hitFilter.SetLayerMask(tuning != null ? tuning.hitLayers.value : ~0); hitFilter.useTriggers = true;
            hitstop = HitstopService.Ensure(gameObject); hitstop.Ended += ApplyPendingKnockback;
            hook = GetComponent<GrappleHook>();
            if (hook != null) hook.StateChanged += OnHookStateChanged;
        }

        void OnDestroy() { if (hitstop != null) hitstop.Ended -= ApplyPendingKnockback; if (hook != null) hook.StateChanged -= OnHookStateChanged; }

        void FixedUpdate()
        {
            if (tuning == null || input == null || body == null || tuning.comboSteps == null || tuning.comboSteps.Length == 0) return;
            if (!swinging)
            {
                if (combo.CanStart(Time.time, tuning.comboStartInterval, tuning.finisherComboWindow) && input.HasBufferedAttack(tuning.inputBuffer) && input.ConsumeAttack(tuning.inputBuffer)) TryBeginSwing();
                return;
            }

            ComboStep step = CurrentTuning;
            previousElapsed = elapsed; elapsed += Time.fixedDeltaTime;
            int phase = ComboSequence.Phase(elapsed, step);
            if (phase == 2 && recoveryStartedAt == double.NegativeInfinity) recoveryStartedAt = Time.realtimeSinceStartupAsDouble;
            if (phase >= 2 && hookCancelPending) { hookCancelPending = false; EndSwing(); return; }
            if (phase == 2 && input.HasJumpActionAfter(recoveryStartedAt, tuning.inputBuffer))
            {
                input.ConsumeJumpAction();
                EndSwing();
                return;
            }

            if (elapsed < step.windup + step.active)
            {
                float angle = AngleAtTime(aimAngle, step.arcDegrees, elapsed, step.windup, step.active, swingDirection, tuning.windupPullbackDegrees);
                armRig?.SetSwordArmOverride(angle);
                SweepHit(previousElapsed, elapsed, step);
            }
            else if (elapsed < step.windup + step.active + step.recovery)
                armRig?.SetSwordArmOverride(SwingAngle(aimAngle, step.arcDegrees, 1f, swingDirection));
            if (elapsed >= step.windup + step.active + step.recovery) EndSwing();
        }

        void TryBeginSwing()
        {
            int step = combo.Start(Time.time, tuning.comboSteps, tuning.comboStartInterval, tuning.finisherComboWindow);
            if (step < 0) return;
            BeginSwing(step);
        }

        void OnHookStateChanged(HookState state)
        {
            if (state != HookState.Thrown || !swinging) return;
            if (ComboSequence.Phase(elapsed, CurrentTuning) >= 2 || recoveryStartedAt != double.NegativeInfinity) EndSwing();
            else hookCancelPending = true;
        }

        ComboStep CurrentTuning => tuning.comboSteps[Mathf.Clamp(stepIndex, 0, tuning.comboSteps.Length - 1)];

        void BeginSwing(int step)
        {
            stepIndex = step;
            Vector2 pivot = PivotPosition();
            Vector2 delta = input.PointerWorld(aimCamera) - pivot;
            Vector2 aim = delta.sqrMagnitude > Mathf.Epsilon ? delta.normalized : Vector2.right * body.Facing;
            int blockers = tuning.aimAssistBlockingLayers.value | (hook != null ? hook.HookableLayers.value : 0);
            aim = ApplyAimAssist(pivot, aim, tuning.aimAssistDegrees, tuning.aimAssistRange, tuning.hitLayers.value, blockers);
            aimAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
            swingDirection = step == 1 ? -1 : 1;
            elapsed = previousElapsed = 0f; recoveryStartedAt = double.NegativeInfinity; hookCancelPending = false; hitIds.Clear(); swinging = true;
            ComboStep settings = CurrentTuning;
            armRig?.SetSwordArmOverride(AngleAtTime(aimAngle, settings.arcDegrees, 0f, settings.windup, settings.active, swingDirection, tuning.windupPullbackDegrees));
            float handTipDistance = armRig != null && armRig.SwordHandTip != null
                ? Vector2.Distance(pivot, armRig.SwordHandTip.position)
                : 0.12f * tuning.bladeLength;
            SlashStarted?.Invoke(step, pivot, aimAngle, settings.arcDegrees, settings.windup,
                settings.active, handTipDistance);
            PlaySlashAudio(step);
        }

        void EndSwing()
        {
            swinging = false; armRig?.SetSwordArmOverride(null);
            if (hitIds.Count == 0) voiceEvents?.Miss();
        }

        void SweepHit(float fromTime, float toTime, ComboStep step)
        {
            float from = Mathf.Max(fromTime, step.windup), to = Mathf.Min(toTime, step.windup + step.active);
            if (to < from) return;
            float fromProgress = (from - step.windup) / Mathf.Max(step.active, Mathf.Epsilon);
            float toProgress = (to - step.windup) / Mathf.Max(step.active, Mathf.Epsilon);
            List<float> angles = SweepSampleAngles(aimAngle, step.arcDegrees, fromProgress, toProgress, swingDirection, tuning.maxSweepStepDegrees);
            for (int i = 0; i < angles.Count; i++) CheckBladeAt(angles[i], step);
        }

        void CheckBladeAt(float angle, ComboStep step)
        {
            BladeEndpoints(PivotPosition(), angle, tuning.bladeLength, out Vector2 start, out Vector2 tip);
            Vector2 center = (start + tip) * 0.5f;
            Vector2 size = new Vector2(tuning.bladeLength + tuning.bladeRadius * 2f, tuning.bladeRadius * 2f);
            int count = Physics2D.OverlapCapsule(center, size, CapsuleDirection2D.Horizontal, angle, hitFilter, hits);
            for (int i = 0; i < count; i++)
            {
                Collider2D collider = hits[i]; if (collider == null) continue;
                MonoBehaviour[] behaviours = collider.GetComponentsInParent<MonoBehaviour>();
                MonoBehaviour target = null; IDamageable damageable = null;
                for (int b = 0; b < behaviours.Length; b++) if (behaviours[b] is IDamageable found) { target = behaviours[b]; damageable = found; break; }
                if (target == null || !hitIds.Add(target.GetInstanceID())) continue;
                Vector2 hitPoint = collider.ClosestPoint(tip);
                Vector2 direction = (new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad)) + Vector2.up * step.knockbackUpBias).normalized;
                float before = GetHealth(target);
                damageable.TakeHit(new HitInfo(step.damage, direction, 0f, this, step.stun, step.hitstop));
                bool killed = IsDead(target, before);
                var targetBrain = target.GetComponentInParent<Game.Enemies.EnemyBrain>();
                bool hasSuperArmor = targetBrain != null && targetBrain.HasTelegraphSuperArmor;
                Rigidbody2D targetBody = target.GetComponentInParent<Rigidbody2D>();
                if (!hasSuperArmor && targetBody != null && targetBody.bodyType == RigidbodyType2D.Dynamic)
                    pending.Add(new PendingKnockback { body = targetBody, impulse = direction * step.knockback });
                if (step.hitstop > 0f && hitstop != null)
                {
                    bool accepted = hitstop.Request(step.hitstop, killed ? tuning.killSlowDuration : 0f, tuning.killSlowScale, tuning.hitstopScale);
                    if (accepted) waitingForHitstop = true;
                    else ApplyPendingKnockback();
                }
                else ApplyPendingKnockback();
                traumaEvent?.Raise(tuning.traumaOnHit); kickEvent?.Raise(direction * tuning.cameraKick);
                voiceEvents?.Hit(); PlayHitAudio(stepIndex, killed);
                HitOccurred?.Invoke(hitPoint, direction, killed);
            }
        }

        void ApplyPendingKnockback()
        {
            for (int i = 0; i < pending.Count; i++) if (pending[i].body != null && pending[i].body.simulated)
                pending[i].body.AddForce(pending[i].impulse, ForceMode2D.Impulse);
            pending.Clear(); waitingForHitstop = false;
        }

        static float GetHealth(MonoBehaviour target)
        {
            if (target is Game.Enemies.EnemyHealth enemy) return enemy.Health;
            if (target is TrainingDummy dummy) return dummy.Health;
            var boss = target.GetComponentInParent<Game.Boss.BossBrain>();
            if (boss != null) return boss.Health;
            return float.NaN;
        }
        static bool IsDead(MonoBehaviour target, float before)
        {
            if (target is Game.Enemies.EnemyHealth enemy) return enemy.IsDead;
            if (target is TrainingDummy dummy) return dummy.Health <= 0f;
            var boss = target.GetComponentInParent<Game.Boss.BossBrain>();
            if (boss != null) return boss.State == Game.Boss.BossState.Dead;
            float after = GetHealth(target); return !float.IsNaN(before) && before > 0f && after <= 0f;
        }

        void PlaySlashAudio(int step)
        {
            AudioClip clip = step == 0 ? tuning.slash1Audio : step == 1 ? tuning.slash2Audio : tuning.slash3Audio;
            if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position);
        }
        void PlayHitAudio(int step, bool killed)
        {
            AudioClip clip = killed ? tuning.killAudio : step == 2 ? tuning.finisherHitAudio : tuning.hitAudio;
            if (clip != null) AudioSource.PlayClipAtPoint(clip, transform.position);
        }

        Vector2 PivotPosition() => armRig != null && armRig.SwordArmPivot != null ? (Vector2)armRig.SwordArmPivot.position : body.Position;

        public Vector2 SlashTrailPosition
        {
            get
            {
                if (armRig != null && armRig.SwordHandTip != null)
                    return armRig.SwordHandTip.position;
                return PivotPosition();
            }
        }
        public static Vector2 ApplyAimAssist(Vector2 origin, Vector2 aim, float coneDegrees, float range, int targetLayers = ~0, int blockingLayers = 0)
        {
            if (coneDegrees <= 0f || range <= 0f) return aim.normalized;
            Collider2D[] nearby = Physics2D.OverlapCircleAll(origin, range);
            float bestDistance = float.PositiveInfinity; Vector2 best = aim.normalized;
            for (int i = 0; i < nearby.Length; i++)
            {
                MonoBehaviour[] components = nearby[i].GetComponentsInParent<MonoBehaviour>(); bool damageable = false;
                if ((targetLayers & (1 << nearby[i].gameObject.layer)) == 0) continue;
                for (int j = 0; j < components.Length; j++) if (components[j] is IDamageable) { damageable = true; break; }
                if (!damageable) continue;
                Vector2 delta = (Vector2)nearby[i].bounds.center - origin; float distance = delta.magnitude;
                if (distance <= Mathf.Epsilon || distance >= bestDistance || Vector2.Angle(aim, delta) > coneDegrees) continue;
                if (blockingLayers != 0 && Physics2D.Linecast(origin, nearby[i].bounds.center, blockingLayers).collider != null) continue;
                bestDistance = distance; best = delta / distance;
            }
            return best;
        }

        public static int PhaseFromTime(float time, float windup, float active, float recovery) =>
            time < windup ? 0 : time < windup + active ? 1 : time < windup + active + recovery ? 2 : 3;
        public static float SwingAngle(float aimAngle, float arcDegrees, float progress, int direction)
        {
            float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress)); float half = arcDegrees * 0.5f;
            return direction >= 0 ? aimAngle + half - arcDegrees * eased : aimAngle - half + arcDegrees * eased;
        }
        public static float AngleAtTime(float aim, float arc, float time, float windup, float active, int direction, float pullback)
        {
            if (time < windup) return SwingAngle(aim, arc, 0f, direction) + direction * pullback * (1f - Mathf.SmoothStep(0f, 1f, windup <= 0f ? 1f : Mathf.Clamp01(time / windup)));
            return SwingAngle(aim, arc, (time - windup) / Mathf.Max(active, Mathf.Epsilon), direction);
        }
        public static void BladeEndpoints(Vector2 pivot, float angle, float length, out Vector2 start, out Vector2 tip)
        { start = pivot; tip = pivot + new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad)) * length; }
        public static List<float> SweepSampleAngles(float aim, float arc, float fromProgress, float toProgress, int direction, float maxStepDegrees)
        {
            float span = Mathf.Abs(toProgress - fromProgress) * arc; int steps = Mathf.Max(1, Mathf.CeilToInt(span * 1.5f / Mathf.Max(0.1f, maxStepDegrees)));
            var result = new List<float>(steps + 1);
            for (int i = 0; i <= steps; i++) result.Add(SwingAngle(aim, arc, Mathf.Lerp(fromProgress, toProgress, i / (float)steps), direction));
            return result;
        }
        void OnDisable() { armRig?.SetSwordArmOverride(null); if (waitingForHitstop && hitstop != null && !hitstop.IsActive) ApplyPendingKnockback(); }
    }
}
