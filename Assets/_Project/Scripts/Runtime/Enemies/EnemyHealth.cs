using System.Collections;
using Game.Combat;
using Game.Core;
using UnityEngine;
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
        Coroutine flashRoutine;
        bool dead;
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
            StartFlash();
        }
        void Die()
        {
            dead = true;
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            if (brain != null) brain.SetDead();
            foreach (var col in colliders) col.enabled = false;
            foreach (var renderer in renderers) renderer.enabled = false;
            if (body != null)  {
                body.linearVelocity = Vector2.zero;
                body.simulated = false;
            }
        }
        void HandlePlayerRespawn()  {
            if (definition != null && definition.resetOnPlayerRespawn) ResetEnemy();
        }
        void ResetEnemy()
        {
            if (definition == null) return;
            dead = false;
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
        }
        void StartFlash()
        {
            if (flashRoutine != null) StopCoroutine(flashRoutine);
            flashRoutine = StartCoroutine(Flash());
        }
        IEnumerator Flash()
        {
            Color color = definition.hitFlashColor;
            foreach (var renderer in renderers) renderer.color = color;
            yield return new WaitForSeconds(definition.hitFlashDuration);
            for (int i = 0; i < renderers.Length; i++) renderers[i].color = baseColors[i];
            flashRoutine = null;
        }
    }
}
