using System;
using System.Collections;
using Game.Combat;
using Game.Core;
using Game.Hook;
using Game.Player;
using UnityEngine;

namespace Game.Boss
{
    public enum BossState { Dormant, Intro, Idle, Select, Telegraph, Active, Recover, Staggered, ShieldRewind, PhaseTransition, Dead }

    /// <summary>Zincirli Bekçi'nin faz, saldırı, hasar ve durum makinesini yürütür.</summary>
    public class BossBrain : MonoBehaviour, IDamageable
    {
        [SerializeField] BossDefinition definition;
        [SerializeField] Rigidbody2D body;
        [SerializeField] Collider2D bodyCollider;
        [SerializeField] BossShield shield;
        [SerializeField] BossArena arena;
        [SerializeField] ContactDamage contactDamage;
        [SerializeField] SpriteRenderer visual;
        [SerializeField] Transform player;
        [SerializeField] HealthEvent healthEvent;
        [SerializeField] FloatEvent traumaEvent;
        [SerializeField] GameEvent defeatedEvent;
        [SerializeField] GameEvent gameWonEvent;
        [SerializeField] int randomSeed;
        BossState state = BossState.Dormant;
        BossAttackEntry chosen;
        System.Random random;
        float health;
        float timer;
        float stateElapsed;
        float idleDuration;
        float startX;
        int phaseIndex;
        int facing = 1;
        int previousCount;
        int rearHits;
        float lastRearHit = -100f;
        string previousAttack;
        bool transitionQueued;
        bool highWhipNext;
        bool active;
        readonly Collider2D[] overlaps = new Collider2D[32];
        ContactFilter2D playerFilter;
        public BossState State => state;
        public float Health => health;
        public int Facing => facing;
        public Vector2 BodyPosition => body != null ? body.position : transform.position;
        public float ShieldRewindDuration => definition != null ? definition.shieldRewind : 0.5f;
        public bool IsInvulnerable => state == BossState.PhaseTransition || state == BossState.Dead || state == BossState.Dormant || state == BossState.Intro;

        void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (bodyCollider == null) bodyCollider = GetComponent<Collider2D>();
            if (shield == null) shield = GetComponentInChildren<BossShield>();
            if (contactDamage == null) contactDamage = GetComponent<ContactDamage>();
            if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
            random = new System.Random(randomSeed);
            playerFilter = new ContactFilter2D();
            playerFilter.SetLayerMask(LayerMask.GetMask("Player"));
            playerFilter.useTriggers = false;
            if (body != null) { body.bodyType = RigidbodyType2D.Kinematic; body.gravityScale = 0f; }
            if (definition != null) health = definition.maxHealth;
            RefreshHealth();
            SetState(BossState.Dormant);
        }

        void Update()
        {
            if (!active || state == BossState.Dead) return;
            timer += Time.deltaTime;
            stateElapsed += Time.deltaTime;
            switch (state)
            {
                case BossState.Intro: if (timer >= definition.introDuration) SetState(BossState.Idle); break;
                case BossState.Idle: if (timer >= idleDuration) SetState(BossState.Select); break;
                case BossState.Select: SelectAttack(); break;
                case BossState.Telegraph:
                    if (stateElapsed >= TelegraphDuration()) SetState(BossState.Active);
                    break;
                case BossState.Active: TickAttack(); break;
                case BossState.Recover:
                    if (stateElapsed >= chosen.attack.recovery) FinishAttack();
                    break;
                case BossState.Staggered:
                    if (timer >= idleDuration)
                    {
                        // Sendelemede can eşiği geçildiyse kalkan zaten kırılacak: geri sarmadan faz geçişine gir.
                        if (transitionQueued) BeginTransition();
                        else if (shield != null && !shield.IsAttached && !shield.IsBroken)
                        {
                            SetState(BossState.ShieldRewind);
                            shield.BeginRewind();
                        }
                        else SetIdle();
                    }
                    break;
                case BossState.ShieldRewind:
                    if (shield == null || shield.IsAttached)
                    {
                        if (transitionQueued) BeginTransition();
                        else SetIdle();
                    }
                    break;
                case BossState.PhaseTransition:
                    if (timer >= definition.phases[phaseIndex].transitionDuration) FinishTransition();
                    break;
            }
        }

        void FixedUpdate()
        {
            if (state != BossState.Active || chosen.attack == null || body == null) return;
            BossAttack attack = chosen.attack;
            if (attack.movement == BossMoveType.Charge)
            {
                Vector2 direction = Vector2.right * facing;
                float distance = attack.moveSpeed * Time.fixedDeltaTime;
                var results = new RaycastHit2D[4];
                var filter = new ContactFilter2D();
                filter.SetLayerMask(LayerMask.GetMask("Ground"));
                filter.useTriggers = false;
                if (body.Cast(direction, filter, results, distance) > 0) { FinishCharge(); return; }
                body.MovePosition(body.position + direction * distance);
            }
        }

        public void BeginFight()
        {
            if (definition == null || definition.phases == null || definition.phases.Length == 0) return;
            active = true;
            SetState(BossState.Intro);
        }

        public void ResetFight()
        {
            StopAllCoroutines();
            active = false;
            state = BossState.Dormant;
            health = definition != null ? definition.maxHealth : 0f;
            phaseIndex = 0;
            transitionQueued = false;
            rearHits = 0;
            previousAttack = null;
            previousCount = 0;
            if (body != null) { body.linearVelocity = Vector2.zero; body.position = new Vector2(startX, body.position.y); }
            if (shield != null) shield.ResetShield(definition != null && definition.phases.Length > 0 && definition.phases[0].hasShield);
            if (contactDamage != null) contactDamage.Suppressed = false;
            RefreshHealth();
        }

        public void SetInitialPosition()
        {
            if (body != null) startX = body.position.x;
        }

        public void TakeHit(HitInfo hit)
        {
            if (IsInvulnerable || health <= 0f) return;
            bool fromFront = HitFromFront(hit.Source);
            if (fromFront && phaseIndex == 0 && shield != null && shield.IsAttached)
            {
                if (visual != null) visual.color = Color.white;
                return;
            }
            // Kancanın varış vuruşu: kalkan kancalandığı an yuvasından ayrılmıştır (IsAttached false), gevşek olması yeter.
            // Sökme ve sendeleme BossShield.Detach -> OnShieldRemoved yolundan gelir; Released bildirimi de aynı yola düşer.
            if (hit.StunDuration > 0f && shield != null && shield.IsLoose) shield.NotifyPullComplete();
            if (!fromFront)
            {
                if (Time.time - lastRearHit <= definition.comboWindow) rearHits++;
                else rearHits = 1;
                lastRearHit = Time.time;
                if (rearHits >= definition.rearComboHits && state != BossState.Staggered)
                {
                    rearHits = 0;
                    timer = 0f;
                    idleDuration = definition.comboStagger;
                    SetState(BossState.Staggered);
                }
            }
            if (state != BossState.Staggered || hit.StunDuration <= 0f)
            {
                health = Mathf.Max(0f, health - hit.Damage);
                RefreshHealth();
                QueuePhaseIfNeeded();
                if (health <= 0f) Die();
            }
        }

        bool HitFromFront(UnityEngine.Object source)
        {
            if (source is Component component) return BossCombatMath.IsFromFront(component.transform.position.x, transform.position.x, facing);
            return true;
        }

        void QueuePhaseIfNeeded()
        {
            if (phaseIndex + 1 < definition.phases.Length &&
                BossCombatMath.ShouldQueuePhase(health, definition.phases[phaseIndex + 1].enterAtHealth, phaseIndex, definition.phases.Length))
                transitionQueued = true;
        }

        void SelectAttack()
        {
            BossPhase phase = definition.phases[phaseIndex];
            if (player == null) { var found = FindFirstObjectByType<PlayerHealth>(); if (found != null) player = found.transform; }
            float range = player != null ? Vector2.Distance(player.position, transform.position) : 0f;
            if (player != null) facing = player.position.x >= transform.position.x ? 1 : -1;
            chosen = BossAttackSelector.Select(phase.attacks, range, previousAttack, previousCount, random);
            if (chosen.attack == null) { SetIdle(); return; }
            if (chosen.attack.id == previousAttack) previousCount++; else { previousAttack = chosen.attack.id; previousCount = 1; }
            if (chosen.attack.movement == BossMoveType.Whip) highWhipNext = !highWhipNext;
            SetState(BossState.Telegraph);
        }

        float TelegraphDuration()
        {
            BossPhase phase = definition.phases[phaseIndex];
            return BossCombatMath.Telegraph(chosen.attack.telegraph, phase.telegraphScale, phase.minTelegraph);
        }

        void TickAttack()
        {
            BossAttack attack = chosen.attack;
            if (stateElapsed >= attack.active)
            {
                if (attack.movement == BossMoveType.Charge) FinishCharge();
                else
                {
                    if (attack.spawnPrefab != null) SpawnAttackObject(attack);
                    SetState(BossState.Recover);
                    if (attack.loosensShield) shield?.SetLoose(true);
                }
            }
            else ApplyHitboxes(attack, stateElapsed);
        }

        void ApplyHitboxes(BossAttack attack, float elapsed)
        {
            for (int i = 0; i < attack.hitboxes.Length; i++)
            {
                BossHitbox box = attack.hitboxes[i];
                if (elapsed < box.activeFrom || elapsed > box.activeTo) continue;
                float offsetY = attack.movement == BossMoveType.Whip ? (highWhipNext ? 2.5f : 0.5f) : box.offset.y;
                Vector2 offset = new Vector2(box.offset.x * facing, offsetY);
                int count = Physics2D.OverlapBox(BodyPosition + offset, box.size, 0f, playerFilter, overlaps);
                for (int j = 0; j < count; j++)
                {
                    Rigidbody2D targetBody = overlaps[j] != null ? overlaps[j].attachedRigidbody : null;
                    if (targetBody == null) continue;
                    MonoBehaviour[] components = targetBody.GetComponentsInParent<MonoBehaviour>();
                    for (int k = 0; k < components.Length; k++)
                        if (components[k] is IDamageable damageable)
                        {
                            damageable.TakeHit(new HitInfo(box.damage, Vector2.right * facing, 0f, this));
                            break;
                        }
                }
            }
        }

        void FinishCharge()
        {
            if (contactDamage != null) contactDamage.Suppressed = true;
            if (visual != null) visual.color = Color.white;
            traumaEvent?.Raise(0.3f);
            stateElapsed = 0f;
            SetState(BossState.Recover);
            if (chosen.attack.loosensShield) shield?.SetLoose(true);
        }

        void SpawnAttackObject(BossAttack attack)
        {
            Vector2 origin = BodyPosition + Vector2.up;
            if (attack.movement == BossMoveType.Slam)
            {
                // Şok dalgası zemin boyunca gider: alt kenarı zeminin hemen üstünde, boss'un iki yanından çıkar.
                float floorY = bodyCollider != null ? bodyCollider.bounds.min.y : BodyPosition.y - 1.5f;
                float halfWidth = bodyCollider != null ? bodyCollider.bounds.extents.x : 1.5f;
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    Vector2 start = new Vector2(BodyPosition.x + direction * halfWidth, floorY + 0.55f);
                    var projectile = Instantiate(attack.spawnPrefab, start, Quaternion.identity);
                    var mover = projectile.GetComponent<BossProjectile>();
                    if (mover != null) mover.Initialize(Vector2.right * direction, attack.spawnSpeed, attack.spawnDamage, attack.spawnLifetime);
                }
            }
            else if (attack.movement == BossMoveType.BeamCollapse)
            {
                Collider2D beam = arena != null ? arena.CollapseBeam(definition.beamNonHookableDuration) : null;
                // Moloz çöken kirişin altından, kiriş boyunca eşit aralıkla düşer.
                Bounds beamBounds = beam != null ? beam.bounds : new Bounds(origin + Vector2.up * 4f, new Vector3(6f, 1f, 0f));
                for (int i = 0; i < 4; i++)
                {
                    float t = (i + 0.5f) / 4f;
                    Vector2 dropPoint = new Vector2(Mathf.Lerp(beamBounds.min.x, beamBounds.max.x, t), beamBounds.min.y - 0.5f);
                    var debris = Instantiate(attack.spawnPrefab, dropPoint, Quaternion.identity);
                    var mover = debris.GetComponent<BossProjectile>();
                    if (mover != null) mover.Initialize(Vector2.down, attack.spawnSpeed, attack.spawnDamage, attack.spawnLifetime);
                }
            }
            else
            {
                var projectile = Instantiate(attack.spawnPrefab, origin, Quaternion.identity);
                var mover = projectile.GetComponent<BossProjectile>();
                if (mover != null) mover.Initialize(Vector2.down, attack.spawnSpeed, attack.spawnDamage, attack.spawnLifetime);
            }
        }

        void FinishAttack()
        {
            if (contactDamage != null) contactDamage.Suppressed = false;
            if (transitionQueued) BeginTransition(); else SetIdle();
        }

        void SetIdle()
        {
            BossPhase phase = definition.phases[phaseIndex];
            idleDuration = (float)(phase.idleMin + random.NextDouble() * Math.Max(0f, phase.idleMax - phase.idleMin));
            SetState(BossState.Idle);
        }

        void BeginTransition()
        {
            transitionQueued = false;
            phaseIndex++;
            if (contactDamage != null) contactDamage.Suppressed = true;
            shield?.Break();
            traumaEvent?.Raise(0.5f);
            SetState(BossState.PhaseTransition);
        }

        void FinishTransition()
        {
            if (contactDamage != null) contactDamage.Suppressed = false;
            SetIdle();
        }

        public void OnShieldRemoved()
        {
            if (state != BossState.Dead)
            {
                timer = 0f;
                idleDuration = definition.shieldStagger;
                SetState(BossState.Staggered);
            }
        }

        void Die()
        {
            active = false;
            if (contactDamage != null) contactDamage.Suppressed = true;
            SetState(BossState.Dead);
            defeatedEvent?.Raise();
            StartCoroutine(WinAfterDelay());
        }

        IEnumerator WinAfterDelay()
        {
            yield return new WaitForSeconds(definition.victoryDelay);
            gameWonEvent?.Raise();
        }

        void RefreshHealth() { healthEvent?.Raise(health, definition != null ? definition.maxHealth : 0f); }

        void SetState(BossState next)
        {
            state = next;
            timer = 0f;
            stateElapsed = 0f;
            if (visual != null) visual.color = next == BossState.Telegraph ? Color.red : Color.gray;
            if (contactDamage != null)
                contactDamage.Suppressed = next == BossState.Recover || next == BossState.Staggered ||
                    next == BossState.ShieldRewind || next == BossState.PhaseTransition || next == BossState.Dead ||
                    next == BossState.Dormant || next == BossState.Intro;
        }

        void OnEnable()
        {
            if (body != null) startX = body.position.x;
        }
    }
}
