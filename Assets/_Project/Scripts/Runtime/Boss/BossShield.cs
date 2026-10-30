using System.Collections;
using Game.Combat;
using Game.Hook;
using UnityEngine;

namespace Game.Boss
{
    /// <summary>Bekçinin kancalanabilir kalkanını, ön blok ve geri sarma davranışıyla yönetir.</summary>
    public class BossShield : MonoBehaviour, IDamageable
    {
        [SerializeField] BossBrain boss;
        [SerializeField] Rigidbody2D body;
        [SerializeField] HookPullable pullable;
        [SerializeField] SpriteRenderer visual;
        [SerializeField] Vector2 socketOffset = new Vector2(2f, 0f);
        NonHookable nonHookable;
        bool detached;
        bool broken;
        bool rewindRunning;
        public bool IsAttached => !detached && !broken;
        public bool IsLoose { get; private set; }
        public bool IsBroken => broken;
        public Vector2 Position => body != null ? body.position : transform.position;

        public void TakeHit(HitInfo hit)
        {
            boss?.TakeHit(hit);
        }

        void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody2D>();
            if (pullable == null) pullable = GetComponent<HookPullable>();
            nonHookable = GetComponent<NonHookable>();
            if (pullable != null) pullable.Hooked += OnHooked;
            if (pullable != null) pullable.Released += OnReleased;
        }

        void FixedUpdate()
        {
            if (IsAttached && boss != null && body != null)
            {
                Vector2 offset = new Vector2(socketOffset.x * boss.Facing, socketOffset.y);
                body.MovePosition(boss.BodyPosition + offset);
            }
        }

        public void SetLoose(bool loose)
        {
            if (broken || detached) return;
            IsLoose = loose;
            if (loose && nonHookable != null) Destroy(nonHookable);
            if (visual != null) visual.color = loose ? Color.yellow : new Color(.72f, .75f, .8f);
            if (!loose && nonHookable == null) nonHookable = gameObject.AddComponent<NonHookable>();
        }

        public void ResetShield(bool enabled)
        {
            StopAllCoroutines();
            rewindRunning = false;
            broken = !enabled;
            detached = false;
            IsLoose = false;
            if (pullable != null) pullable.enabled = enabled;
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.linearVelocity = Vector2.zero;
                body.simulated = enabled;
            }
            if (enabled && nonHookable == null) nonHookable = gameObject.AddComponent<NonHookable>();
            if (nonHookable != null && !enabled) Destroy(nonHookable);
            if (visual != null) visual.enabled = enabled;
            if (enabled && boss != null) transform.position = boss.BodyPosition + new Vector2(socketOffset.x * boss.Facing, socketOffset.y);
        }

        public void Break()
        {
            broken = true;
            detached = false;
            if (pullable != null) pullable.enabled = false;
            if (body != null) body.simulated = false;
            if (visual != null) visual.enabled = false;
            if (nonHookable != null) Destroy(nonHookable);
        }

        void OnHooked()
        {
            if (!broken && IsLoose) detached = true;
        }

        void OnReleased()
        {
            if (!broken && IsLoose) Detach();
        }

        public void NotifyPullComplete()
        {
            if (!broken && IsLoose && detached) Detach();
        }

        void Detach()
        {
            detached = true;
            IsLoose = false;
            if (nonHookable != null) Destroy(nonHookable);
            boss?.OnShieldRemoved();
            if (pullable != null) pullable.enabled = false;
            // Sökülen kalkan yere düşer; geri sarılırken yeniden Kinematic olur.
            if (body != null)
            {
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = 3f;
                body.freezeRotation = true;
            }
        }

        public void BeginRewind()
        {
            if (broken || !detached || rewindRunning) return;
            rewindRunning = true;
            StartCoroutine(Rewind());
        }

        IEnumerator Rewind()
        {
            // Zincir kalkanı süre boyunca yuvasına çeker.
            float duration = boss != null ? boss.ShieldRewindDuration : 0.5f;
            if (body != null)
            {
                body.simulated = true;
                body.bodyType = RigidbodyType2D.Kinematic;
                body.linearVelocity = Vector2.zero;
            }
            Vector2 from = Position;
            float elapsed = 0f;
            while (elapsed < duration && !broken)
            {
                elapsed += Time.fixedDeltaTime;
                Vector2 socket = boss != null ? boss.BodyPosition + new Vector2(socketOffset.x * boss.Facing, socketOffset.y) : from;
                Vector2 point = Vector2.Lerp(from, socket, Mathf.SmoothStep(0f, 1f, elapsed / duration));
                if (body != null) body.MovePosition(point);
                else transform.position = point;
                yield return new WaitForFixedUpdate();
            }
            if (broken) yield break;
            if (pullable != null) pullable.enabled = true;
            detached = false;
            SetLoose(false);
            rewindRunning = false;
        }

        void OnDisable()
        {
            if (pullable != null) { pullable.Hooked -= OnHooked; pullable.Released -= OnReleased; }
        }
    }
}
