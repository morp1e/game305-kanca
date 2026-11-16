using System.Collections;
using System;
using Game.Combat;
using Game.Core;
using Game.Hook;
using Game.Level;
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(PlayerMotor))]
    public class PlayerHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] PlayerHealthTuning tuning;
        [SerializeField] HealthEvent healthEvent;
        [SerializeField] GameEvent damagedEvent;
        [SerializeField] FloatEvent traumaEvent;
        [SerializeField] GameObject visual;

        PlayerMotor motor;
        RespawnController respawn;
        PlayerAnimDriver deathAnimDriver;
        GrappleHook hook;
        Rigidbody2D body;
        float currentHealth;
        float invulnerableUntil;
        float hazardReturnRemaining;
        float hitstopRemaining;
        bool hazardReturnActive;
        bool hitstopActive;
        bool bodyWasSimulated;
        bool externalBeforeHitstop;
        bool hasHazardPoint;
        bool initialized;
        bool deathNotified;
        Vector2 hazardPoint;
        Vector2 pendingKnockback;
        Coroutine blinkRoutine;
        SpriteRenderer[] blinkRenderers = Array.Empty<SpriteRenderer>();
        bool[] blinkRendererWasEnabled = Array.Empty<bool>();
        Coroutine deathRoutine;
        Transform heavyPullTarget;
        float heavyPullGraceUntil;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => tuning != null ? tuning.maxHealth : currentHealth;
        public bool IsInvulnerable => PlayerHealthMath.IsInvulnerable(Time.time, invulnerableUntil);
        public float HurtControlLockSeconds => Mathf.Max(0f, tuning != null ? tuning.knockbackControlLockSeconds : 0f);
        public event Action Damaged;
        public event Action Died;

        void Awake()
        {
            Initialize();
        }

        void Initialize()
        {
            motor = GetComponent<PlayerMotor>();
            respawn = GetComponent<RespawnController>();
            deathAnimDriver = GetComponentInChildren<PlayerAnimDriver>(true);
            hook = GetComponent<GrappleHook>();
            body = GetComponent<Rigidbody2D>();
            if (visual == null) visual = transform.Find("Visual") != null ? transform.Find("Visual").gameObject : null;
            currentHealth = tuning != null ? Mathf.Max(0f, tuning.maxHealth) : 1f;
            initialized = tuning != null;
        }

        void OnEnable()
        {
            if (respawn != null) respawn.Respawned += HandleRespawn;
            if (hook != null) hook.StateChanged += HandleHookState;
        }

        void Start()
        {
            if (!initialized) Initialize();
            PublishHealth();
        }

        void Update()
        {
            if (respawn != null && respawn.IsDead) NotifyDied();
            else if (respawn != null && !respawn.IsDead && currentHealth > 0f) deathNotified = false;
        }

        void OnDisable()
        {
            if (respawn != null) respawn.Respawned -= HandleRespawn;
            if (hook != null) hook.StateChanged -= HandleHookState;
            if (blinkRoutine != null) StopCoroutine(blinkRoutine);
            if (deathRoutine != null) StopCoroutine(deathRoutine);
            SetVisualVisible(true);
            RestoreHitstop(false);
        }

        void FixedUpdate()
        {
            if (hazardReturnActive)
            {
                hazardReturnRemaining = Mathf.Max(0f, hazardReturnRemaining - Time.fixedDeltaTime);
                if (hazardReturnRemaining <= 0f)
                {
                    hazardReturnActive = false;
                    invulnerableUntil = Time.time + Mathf.Max(0f, tuning != null ? tuning.invulnerabilitySeconds : 0f);
                    StartBlink();
                }
            }
            if (hitstopActive)
            {
                hitstopRemaining = Mathf.Max(0f, hitstopRemaining - Time.fixedDeltaTime);
                if (hitstopRemaining <= 0f)
                {
                    RestoreHitstop(true);
                    motor?.ApplyKnockback(pendingKnockback, tuning != null ? tuning.knockbackControlLockSeconds : 0f);
                }
            }
        }

        public void TakeHit(HitInfo hit)
        {
            if (currentHealth <= 0f || IsInvulnerable || hit.Damage <= 0f || (respawn != null && respawn.IsDead) || hazardReturnActive) return;
            if (IsDamageFromActiveHeavyPullable(hit.Source)) return;

            currentHealth = PlayerHealthMath.ClampHealth(PlayerHealthMath.ApplyDamage(currentHealth, hit.Damage), MaxHealth);
            PublishHealth();
            Damaged?.Invoke();
            damagedEvent?.Raise();
            traumaEvent?.Raise(tuning != null ? tuning.traumaOnDamage : 0f);
            if (currentHealth <= 0f)
            {
                BeginLethalDeath();
                return;
            }

            Vector2 direction = PlayerHealthMath.KnockbackDirection(hit.Direction.x, motor != null ? motor.Facing : 1);
            Vector2 velocity = Vector2.Scale(direction, tuning != null ? tuning.knockbackSpeed : Vector2.zero);
            if (hook != null && hook.State != HookState.InHand) hook.ResetToHand();
            BeginHitstop(velocity);
            invulnerableUntil = Time.time + Mathf.Max(0f, tuning != null ? tuning.invulnerabilitySeconds : 0f);
            StartBlink();
        }

        public void TakeHazardHit()
        {
            if (hazardReturnActive || (respawn != null && (respawn.IsDead || respawn.IsReturning || respawn.IsDeathAnimationPending))) return;
            HazardPolicy policy = tuning != null ? tuning.hazardPolicy : HazardPolicy.InstantDeath;
            if (policy == HazardPolicy.InstantDeath) { BeginLethalDeath(); return; }

            currentHealth = PlayerHealthMath.ClampHealth(PlayerHealthMath.ApplyDamage(currentHealth, tuning != null ? tuning.hazardDamage : 1f), MaxHealth);
            PublishHealth();
            Damaged?.Invoke();
            damagedEvent?.Raise();
            if (currentHealth <= 0f) { BeginLethalDeath(); return; }

            if (hook != null && hook.State != HookState.InHand) hook.ResetToHand();
            Vector2 destination = hasHazardPoint ? hazardPoint : respawn != null ? respawn.CurrentCheckpoint : transform.position;
            float delay = Mathf.Max(0f, tuning != null ? tuning.hazardReturnDelay : 0f);
            hazardReturnActive = true;
            hazardReturnRemaining = delay;
            respawn?.ReturnTo(destination, delay);
            if (delay <= 0f)
            {
                hazardReturnActive = false;
                invulnerableUntil = Time.time + Mathf.Max(0f, tuning != null ? tuning.invulnerabilitySeconds : 0f);
                StartBlink();
            }
        }

        public void SetHazardReturnPoint(Vector2 position)
        {
            hazardPoint = position;
            hasHazardPoint = true;
        }

        void NotifyDied()
        {
            if (deathNotified) return;
            deathNotified = true;
            Died?.Invoke();
        }

        void BeginLethalDeath()
        {
            NotifyDied();
            if (respawn == null) return;
            float duration = deathAnimDriver != null
                ? deathAnimDriver.PlayDeath(tuning != null ? tuning.deathAnimationMaxDuration : 0.65f)
                : 0f;
            if (duration <= 0f)
            {
                respawn.Kill();
                return;
            }
            respawn.BeginDeathAnimation();
            if (deathRoutine != null) StopCoroutine(deathRoutine);
            deathRoutine = StartCoroutine(CompleteDeathAnimation(duration));
        }

        IEnumerator CompleteDeathAnimation(float duration)
        {
            yield return new WaitForSeconds(duration);
            deathRoutine = null;
            respawn?.CompleteDeathAnimation();
        }

        bool IsDamageFromActiveHeavyPullable(UnityEngine.Object source)
        {
            if (!(source is ContactDamage contact)) return false;
            // Çekiş sürerken ve varıştan hemen sonra: oyuncu kendini hedefe çekip ona çarpınca hasar almasın.
            if (hook != null && hook.State == HookState.Pulling && hook.ActivePullable != null && IsRelated(contact.transform, hook.ActivePullable.transform)) return true;
            return heavyPullTarget != null && Time.time < heavyPullGraceUntil && IsRelated(contact.transform, heavyPullTarget);
        }

        static bool IsRelated(Transform a, Transform b) => a == b || a.IsChildOf(b) || b.IsChildOf(a) || a.parent == b;

        void HandleHookState(HookState state)
        {
            // Pulling -> Launch geçişinde aktif hedef henüz temizlenmemiştir.
            if (state != HookState.Launch || hook.ActivePullable == null) return;
            heavyPullTarget = hook.ActivePullable.transform;
            heavyPullGraceUntil = Time.time + Mathf.Max(0f, tuning != null ? tuning.heavyPullContactGrace : 0f);
        }

        void BeginHitstop(Vector2 knockback)
        {
            pendingKnockback = knockback;
            float duration = Mathf.Max(0f, tuning != null ? tuning.damageHitstop : 0f);
            if (duration <= 0f || body == null)
            {
                motor?.ApplyKnockback(knockback, tuning != null ? tuning.knockbackControlLockSeconds : 0f);
                return;
            }
            if (!hitstopActive)
            {
                bodyWasSimulated = body.simulated;
                externalBeforeHitstop = motor != null && motor.ExternalControl;
                body.simulated = false;
                if (motor != null) motor.ExternalControl = true;
                hitstopActive = true;
            }
            hitstopRemaining = Mathf.Max(hitstopRemaining, duration);
        }

        void RestoreHitstop(bool applyVelocity)
        {
            if (!hitstopActive) return;
            if (body != null) body.simulated = bodyWasSimulated;
            if (motor != null)
            {
                motor.ExternalControl = externalBeforeHitstop;
                if (!applyVelocity && body != null) motor.Velocity = Vector2.zero;
            }
            hitstopActive = false;
        }

        void HandleRespawn()
        {
            if (tuning != null && tuning.refillOnRespawn)
            {
                currentHealth = Mathf.Max(0f, tuning.maxHealth);
                PublishHealth();
            }
            deathAnimDriver?.ResetAfterRespawn();
        }

        void PublishHealth() => healthEvent?.Raise(currentHealth, MaxHealth);

        void StartBlink()
        {
            if (blinkRoutine != null) StopCoroutine(blinkRoutine);
            CacheBlinkRenderers();
            if (isActiveAndEnabled) blinkRoutine = StartCoroutine(BlinkWhileInvulnerable());
        }

        IEnumerator BlinkWhileInvulnerable()
        {
            bool visible = true;
            while (IsInvulnerable)
            {
                visible = !visible;
                SetVisualVisible(visible);
                yield return new WaitForSeconds(Mathf.Max(0.01f, tuning != null ? tuning.blinkInterval : 0.08f));
            }
            RestoreVisualVisibility();
            blinkRoutine = null;
        }

        void CacheBlinkRenderers()
        {
            if (visual == null)
            {
                blinkRenderers = Array.Empty<SpriteRenderer>();
                blinkRendererWasEnabled = Array.Empty<bool>();
                return;
            }

            blinkRenderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
            blinkRendererWasEnabled = new bool[blinkRenderers.Length];
            for (int i = 0; i < blinkRenderers.Length; i++)
                blinkRendererWasEnabled[i] = blinkRenderers[i] != null && blinkRenderers[i].enabled;
        }

        void SetVisualVisible(bool visible)
        {
            for (int i = 0; i < blinkRenderers.Length; i++)
                if (blinkRenderers[i] != null && blinkRendererWasEnabled[i])
                    blinkRenderers[i].enabled = visible;
        }

        void RestoreVisualVisibility()
        {
            for (int i = 0; i < blinkRenderers.Length; i++)
                if (blinkRenderers[i] != null)
                    blinkRenderers[i].enabled = blinkRendererWasEnabled[i];
        }
    }
}
