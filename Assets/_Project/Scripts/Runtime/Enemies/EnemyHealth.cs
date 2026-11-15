using Game.Combat;
using Game.Core;
using UnityEngine;
using Game.Visual;
namespace Game.Enemies
{
    /// <summary>Düşman canı, isabet parlaması ve yeniden doğma yönetimi.</summary>
    public class EnemyHealth : MonoBehaviour, IDamageable
    {
        [SerializeField] EnemyDefinition definition;
        [SerializeField] GameEvent playerRespawnEvent;
        [SerializeField] EnemyBrain brain;
        Rigidbody2D body;
        Collider2D[] colliders;
        SpriteRenderer[] renderers;
        Color[] baseColors;
        Vector3 spawnPosition;
        Quaternion spawnRotation;
        float health;
        CombatHitReaction reaction;
        bool dead;
        SpriteAnimPlayer visualPlayer;
        SpriteRenderer visualRenderer;
        EnemyAnimator enemyAnimator;
        public float Health => health;
        public float MaxHealth => definition != null ? definition.maxHealth : 0f;
        public bool IsDead => dead;
        public EnemyDefinition Definition => definition;
        void Awake()  {
            CacheComponents();
            ResetEnemy();
        }
        void OnEnable()  {
            if (playerRespawnEvent != null) playerRespawnEvent.Register(HandlePlayerRespawn);
        }
        void OnDisable()  {
            if (playerRespawnEvent != null) playerRespawnEvent.Unregister(HandlePlayerRespawn);
        }
        void CacheComponents()
        {
            body = GetComponent<Rigidbody2D>();
            colliders = GetComponentsInChildren<Collider2D>(true);
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
            baseColors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
            if (brain == null) brain = GetComponent<EnemyBrain>();
            enemyAnimator = GetComponent<EnemyAnimator>();
            reaction = GetComponent<CombatHitReaction>();
            visualPlayer = GetComponentInChildren<SpriteAnimPlayer>(true);
            visualRenderer = visualPlayer != null ? visualPlayer.GetComponent<SpriteRenderer>() : null;
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
        }
        public void TakeHit(HitInfo hit)
        {
            if (dead || definition == null || (brain != null && brain.BlocksHit(hit))) return;
            health = Mathf.Max(0f, health - Mathf.Max(0f, hit.Damage));
            // Geri itmeyi SwordAttack hitstop bittikten sonra dürtüyle uygular; burada ikinci kez itilmez.
            brain?.NotifyHit(hit);
            if (health <= 0f)  {
                Die();
                return;
            }
            reaction?.Play(hit.Hitstop);
        }
        void Die()
        {
            dead = true;
            SpriteAnimClip deathClip = definition != null && definition.visualSet != null ? definition.visualSet.Find("death") : null;
            bool hasDeathClip = deathClip != null && deathClip.frames != null && deathClip.frames.Length > 0;
            if (visualPlayer != null && hasDeathClip) visualPlayer.ClipFinished += HandleDeathClipFinished;
            if (brain != null) brain.SetDead();
            foreach (var col in colliders) col.enabled = false;
            foreach (var renderer in renderers)
                if (renderer != visualRenderer) renderer.enabled = false;
            if (!hasDeathClip)
            {
                if (definition == null || definition.deathHoldSeconds <= 0f) DisableRenderers();
                else StartCoroutine(DisableAfterDeathHold());
            }
            if (body != null)  {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }
        }
        System.Collections.IEnumerator DisableAfterDeathHold()
        {
            yield return new WaitForSeconds(Mathf.Max(0f, definition.deathHoldSeconds));
            DisableRenderers();
        }
        void HandleDeathClipFinished(string clipName)
        {
            if (clipName != "death" || !dead) return;
            DisableRenderers();
        }
        void DisableRenderers()
        {
            if (visualPlayer != null) visualPlayer.ClipFinished -= HandleDeathClipFinished;
            foreach (var renderer in renderers) renderer.enabled = false;
        }
        void HandlePlayerRespawn()  {
            if (definition != null && definition.resetOnPlayerRespawn) ResetEnemy();
        }
        void ResetEnemy()
        {
            if (definition == null) return;
            dead = false;
            StopAllCoroutines();
            if (visualPlayer != null) visualPlayer.ClipFinished -= HandleDeathClipFinished;
            health = definition.maxHealth;
            if (body != null)  {
                body.simulated = true;
                body.position = spawnPosition;
                body.linearVelocity = Vector2.zero;
            }
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
            foreach (var col in colliders) col.enabled = true;
            for (int i = 0; i < renderers.Length; i++)  {
                renderers[i].enabled = true;
                renderers[i].color = baseColors[i];
            }
            brain?.ResetBrain();
            enemyAnimator?.ResetToIdle();
        }
    }
}
