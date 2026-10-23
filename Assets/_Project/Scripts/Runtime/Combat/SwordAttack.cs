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
        float elapsed, cooldownRemaining, freezeRemaining, aimAngle;
        bool swinging;
        bool ownsBodyFreeze;
        bool bodyWasExternallyControlled;
        Vector2 velocityBeforeFreeze;
        ContactFilter2D enemyFilter;
        public bool IsSwinging => swinging;
        public event System.Action<bool> HitResolved;

        void Awake()
        {
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
                if (freezeRemaining <= 0f && ownsBodyFreeze)
                {
                    body.Velocity = velocityBeforeFreeze;
                    body.ExternalControl = bodyWasExternallyControlled;
                    ownsBodyFreeze = false;
                }
                return;
            }
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - dt);
            if (!swinging && cooldownRemaining <= 0f && input.ConsumeAttack(tuning.inputBuffer)) BeginSwing();
            if (!swinging) return;
            elapsed += dt;
            float normalized = Mathf.Clamp01(elapsed / Mathf.Max(tuning.attackDuration, Mathf.Epsilon));
            armRig?.SetSwordArmOverride(SwingAngle(aimAngle, tuning.swingArcDegrees, normalized, body.Facing));
            if (normalized >= tuning.activeStart && normalized <= tuning.activeEnd) CheckHit();
            if (elapsed >= tuning.attackDuration)
            {
                swinging = false;
                armRig?.SetSwordArmOverride(null);
                cooldownRemaining = tuning.cooldown;
                if (hitIds.Count == 0) voiceEvents?.Miss();
            }
        }

        void BeginSwing()
        {
            var delta = input.PointerWorld(aimCamera) - body.Position;
            aimAngle = delta.sqrMagnitude > Mathf.Epsilon ? Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg : (body.Facing > 0 ? 0f : 180f);
            elapsed = 0f; freezeRemaining = 0f; hitIds.Clear(); swinging = true;
        }

        void CheckHit()
        {
            float angle = SwingAngle(aimAngle, tuning.swingArcDegrees, Mathf.Clamp01(elapsed / Mathf.Max(tuning.attackDuration, Mathf.Epsilon)), body.Facing);
            Vector2 direction = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            Vector2 center = body.Position + direction * tuning.hitOffset.x + Vector2.Perpendicular(direction) * tuning.hitOffset.y;
            int count = Physics2D.OverlapBox(center, tuning.hitSize, angle, enemyFilter, hits);
            for (int i = 0; i < count; i++)
            {
                var behaviours = hits[i] != null ? hits[i].GetComponentsInParent<MonoBehaviour>() : System.Array.Empty<MonoBehaviour>();
                MonoBehaviour target = null;
                IDamageable damageable = null;
                for (int b = 0; b < behaviours.Length; b++)
                    if (behaviours[b] is IDamageable found) { target = behaviours[b]; damageable = found; break; }
                if (target == null || !hitIds.Add(target.GetInstanceID())) continue;
                Vector2 knockDirection = ((Vector2)target.transform.position - body.Position).normalized;
                damageable.TakeHit(new HitInfo(tuning.damage, knockDirection, tuning.knockback, this));
                var targetBody = target.GetComponentInParent<Rigidbody2D>();
                if (targetBody != null && targetBody.bodyType == RigidbodyType2D.Dynamic) targetBody.AddForce(knockDirection * tuning.knockback, ForceMode2D.Impulse);
                freezeRemaining = tuning.hitstop;
                if (tuning.hitstop > 0f && !body.ExternalControl)
                {
                    velocityBeforeFreeze = body.Velocity;
                    bodyWasExternallyControlled = body.ExternalControl;
                    body.ExternalControl = true;
                    body.Velocity = Vector2.zero;
                    ownsBodyFreeze = true;
                }
                traumaEvent?.Raise(tuning.traumaOnHit);
                voiceEvents?.Hit();
                HitResolved?.Invoke(true);
            }
        }

        public static float SwingAngle(float aimAngle, float arcDegrees, float progress, int facing)
        {
            float halfArc = arcDegrees * 0.5f;
            return facing < 0
                ? aimAngle - halfArc + arcDegrees * Mathf.Clamp01(progress)
                : aimAngle + halfArc - arcDegrees * Mathf.Clamp01(progress);
        }

        void OnDisable()
        {
            armRig?.SetSwordArmOverride(null);
            if (ownsBodyFreeze && body != null)
            {
                body.Velocity = velocityBeforeFreeze;
                body.ExternalControl = bodyWasExternallyControlled;
                ownsBodyFreeze = false;
            }
        }
        void OnDrawGizmosSelected()
        {
            if (tuning == null) return;
            Gizmos.color = Color.yellow;
            var dir = new Vector2(Mathf.Cos(aimAngle * Mathf.Deg2Rad), Mathf.Sin(aimAngle * Mathf.Deg2Rad));
            Gizmos.DrawWireCube((Vector2)transform.position + dir * tuning.hitOffset.x, tuning.hitSize);
        }
    }
}
