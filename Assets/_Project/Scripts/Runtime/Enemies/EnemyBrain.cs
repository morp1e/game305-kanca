using Game.Combat;
using Game.Hook;
using Game.Player;
using System;
using UnityEngine;
namespace Game.Enemies
{
    /// <summary>Düşmanların ortak hedef bulma, durum, kanca ve fizik altyapısı.</summary>
    [RequireComponent(typeof(Rigidbody2D), typeof(EnemyHealth), typeof(HookPullable))]
    [RequireComponent(typeof(ContactDamage))]
    public abstract class EnemyBrain : MonoBehaviour
    {
        [SerializeField] EnemyDefinition definition;
        [SerializeField] PlayerHealth player;
        [SerializeField] LayerMask groundLayers;
        [SerializeField] SpriteRenderer bodyRenderer;
        [SerializeField] Game.Visual.SpriteAnimPlayer visualPlayer;
        protected Rigidbody2D Body  {
            get;
            private set;
        }
        protected EnemyDefinition Tuning => definition;
        public bool HasTelegraphSuperArmor => State == EnemyState.Telegraph;
        public event Action<EnemyState, EnemyState> StateChanged;
        public EnemyState CurrentState => State;
        public EnemyDefinition Definition => definition;
        public float StunDuration => hurtRemaining;
        public virtual float RecoveryDuration => definition != null ? definition.recoverDuration : 0f;
        protected PlayerHealth Target => player;
        protected HookPullable Pullable  {
            get;
            private set;
        }
        protected ContactDamage Contact  {
            get;
            private set;
        }
        protected EnemyState State  {
            get;
            private set;
        }
        protected float StateTime  {
            get;
            private set;
        }
        protected Vector2 Home  {
            get;
            private set;
        }
        protected int Facing { get; set; } = 1;
        bool telegraphVisual;
        Color baseRendererColor;
        protected bool HasTarget => player != null && (player.GetComponent<Game.Level.RespawnController>() == null || !player.GetComponent<Game.Level.RespawnController>().IsDead);
        protected Vector2 TargetPosition => player != null ? player.transform.position : transform.position;
        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody2D>();
            Pullable = GetComponent<HookPullable>();
            Contact = GetComponent<ContactDamage>();
            if (visualPlayer == null) visualPlayer = GetComponentInChildren<Game.Visual.SpriteAnimPlayer>();
            if (bodyRenderer == null && visualPlayer != null) bodyRenderer = visualPlayer.GetComponent<SpriteRenderer>();
            if (bodyRenderer == null) bodyRenderer = GetComponentInChildren<SpriteRenderer>();
            if (bodyRenderer != null) baseRendererColor = bodyRenderer.color;
            if (player == null) player = FindFirstObjectByType<PlayerHealth>();
            if (groundLayers.value == 0) groundLayers = LayerMask.GetMask("Ground");
            Home = transform.position;
            State = EnemyState.Patrol;
            Pullable.Hooked += HandleHooked;
            Pullable.Released += HandleReleased;
            if (Tuning != null) ConfigureBody();
        }
        protected virtual void OnDestroy()  {
            if (Pullable != null)  {
                Pullable.Hooked -= HandleHooked;
                Pullable.Released -= HandleReleased;
            }
        }
        protected virtual void FixedUpdate()
        {
            if (State == EnemyState.Dead) return;
            if (Pullable.IsHooked && Pullable.WeightClass == HookWeightClass.Light)  {
                ChangeState(EnemyState.Pulled);
                Contact.Suppressed = true;
                return;
            }
            if (State == EnemyState.Pulled) ChangeState(EnemyState.Patrol);
            StateTime += Time.fixedDeltaTime;
            if (State == EnemyState.Stunned || State == EnemyState.Hurt)
            {
                // Kılıcın geri itmesi sıfırlanmaz, söner; uçan düşmanda dikey hız da söner.
                Vector2 velocity = Body.linearVelocity;
                velocity.x = Mathf.MoveTowards(velocity.x, 0f, Tuning.hurtDeceleration * Time.fixedDeltaTime);
                if (Tuning.gravityScale <= 0f) velocity.y = Mathf.MoveTowards(velocity.y, 0f, Tuning.hurtDeceleration * Time.fixedDeltaTime);
                Body.linearVelocity = velocity;
                if (StateTime >= hurtRemaining) ChangeState(HasTarget ? EnemyState.Chase : EnemyState.Patrol);
                return;
            }
            if (State == EnemyState.Patrol && CanSeePlayer()) ChangeState(EnemyState.Notice);
            else if (HasTarget && Vector2.Distance(transform.position, TargetPosition) > Tuning.loseTargetRadius && State != EnemyState.Attack && State != EnemyState.Telegraph) ChangeState(EnemyState.Patrol);
            TickState();
            UpdateVisual();
        }
        float hurtRemaining;
        protected abstract void TickState();
        protected bool CanSeePlayer()
        {
            if (!HasTarget || Vector2.Distance(transform.position, TargetPosition) > Tuning.detectionRadius) return false;
            Vector2 delta = TargetPosition - (Vector2)transform.position;
            RaycastHit2D hit = Physics2D.Raycast(transform.position, delta.normalized, delta.magnitude, groundLayers);
            return hit.collider == null;
        }
        protected void ChangeState(EnemyState state)  {
            EnemyState previous = State;
            if (previous == state) return;
            State = state;
            StateTime = 0f;
            Contact.Suppressed = state == EnemyState.Hurt || state == EnemyState.Stunned || state == EnemyState.Pulled || state == EnemyState.Dead;
            OnStateChanged(state);
            StateChanged?.Invoke(previous, state);
        }
        protected virtual void OnStateChanged(EnemyState state)  {
        }
        void HandleHooked()  {
            if (Pullable.WeightClass == HookWeightClass.Light) ChangeState(EnemyState.Pulled);
        }
        void HandleReleased()  {
            if (State == EnemyState.Pulled) ChangeState(EnemyState.Patrol);
        }
        void UpdateVisual()
        {
            if (bodyRenderer == null) return;
            if (visualPlayer != null) visualPlayer.SetFlip(Facing < 0);
            if (State == EnemyState.Telegraph)
            {
                float strength = Tuning == null || Tuning.visualSet != null ? (Tuning != null ? Tuning.telegraphTintStrength : 0f) : 1f;
                bodyRenderer.color = Color.Lerp(baseRendererColor, Tuning != null ? Tuning.telegraphColor : Color.yellow, Mathf.PingPong(Time.time * 12f, 1f) * strength);
                telegraphVisual = true;
            }
            else
            {
                if (telegraphVisual)
                {
                    bodyRenderer.color = baseRendererColor;
                    telegraphVisual = false;
                }
            }
        }
        void ConfigureBody()  {
            Body.bodyType = RigidbodyType2D.Dynamic;
            Body.gravityScale = Tuning.gravityScale;
            Body.constraints = RigidbodyConstraints2D.FreezeRotation;
            // Geri itmeyi kılıç dürtüyle verir; direnç kütleyle uygulanır (1 = neredeyse kıpırdamaz, oyuncu da itemez).
            Body.mass = Mathf.Lerp(1f, 50f, Mathf.Clamp01(Tuning.knockbackResistance));
        }
        public bool BlocksHit(HitInfo hit)
        {
            if (Tuning == null || hit.Damage <= 0f || Tuning.shieldArcDegrees <= 0f || State == EnemyState.Recover) return false;
            Vector2 attacker = hit.Source is Component component ? component.transform.position : (Vector2)transform.position - hit.Direction;
            bool blocked = ShieldMath.IsBlocked(Vector2.right * Facing, transform.position, attacker, Tuning.shieldArcDegrees);
            if (blocked && bodyRenderer != null) StartCoroutine(ShieldFlash());
            return blocked;
        }
        System.Collections.IEnumerator ShieldFlash()
        {
            Color original = bodyRenderer.color;
            bodyRenderer.color = Color.white;
            yield return new WaitForSeconds(.08f);
            if (bodyRenderer != null) bodyRenderer.color = original;
        }
        public void NotifyHit(HitInfo hit)
        {
            if (State == EnemyState.Dead || State == EnemyState.Telegraph) return;
            if (hit.Damage > 0f)
            {
                hurtRemaining = hit.StunDuration > 0f ? hit.StunDuration : Tuning.hurtStunDuration;
                ChangeState(EnemyState.Hurt);
                return;
            }
            if (hit.StunDuration <= 0f) return;
            hurtRemaining = hit.StunDuration;
            ChangeState(EnemyState.Stunned);
        }
        public void SetDead()  {
            ChangeState(EnemyState.Dead);
            Body.linearVelocity = Vector2.zero;
            enabled = false;
        }
        public void ResetBrain()  {
            enabled = true;
            Home = transform.position;
            Facing = 1;
            hurtRemaining = 0f;
            ChangeState(EnemyState.Patrol);
        }
        protected bool GroundOrWallAhead()
        {
            Collider2D collider = GetComponent<Collider2D>();
            Bounds bounds = collider != null ? collider.bounds : new Bounds(transform.position, Vector3.one);
            Vector2 origin = new Vector2(bounds.center.x, bounds.center.y);
            Vector2 edgeOrigin = new Vector2(bounds.center.x + Facing * (bounds.extents.x + .05f), bounds.min.y + .05f);
            bool wall = Physics2D.Raycast(origin, Vector2.right * Facing, .15f, groundLayers).collider != null;
            bool ground = Physics2D.Raycast(edgeOrigin, Vector2.down, .15f, groundLayers).collider != null;
            bool atPatrolLimit = Mathf.Abs(transform.position.x - Home.x) >= Tuning.patrolRange;
            if (EnemyMath.ShouldTurnAtWall(wall) || EnemyMath.ShouldTurnAtEdge(ground) || atPatrolLimit)  {
                Facing *= -1;
                return false;
            }
            return true;
        }
        protected void SetHorizontal(float speed)  {
            Body.linearVelocity = new Vector2(Facing * speed, Body.linearVelocity.y);
        }
        protected void FaceTarget()  {
            if (HasTarget) Facing = TargetPosition.x >= transform.position.x ? 1 : -1;
        }
    }
}
