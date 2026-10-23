using System.Collections;
using Game.Hook;
using UnityEngine;

namespace Game.Combat
{
    public class TrainingDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] TrainingDummyTuning tuning;
        SpriteRenderer spriteRenderer;
        Rigidbody2D body;
        Color baseColor;
        float health;
        Coroutine restore;
        public float Health => health;

        // Start: testler bileşeni AddComponent ile ekleyip ayarı sonra atar; Awake o ana kadar çalışmış olur.
        void Start()
        {
            if (tuning == null) { Debug.LogError("[TrainingDummy] TrainingDummyTuning atanmalı.", this); enabled = false; return; }
            health = tuning.maxHealth; spriteRenderer = GetComponentInChildren<SpriteRenderer>(); body = GetComponent<Rigidbody2D>();
            if (spriteRenderer != null) baseColor = spriteRenderer.color;
        }
        public void TakeHit(HitInfo hit)
        {
            if (health <= 0f) return;
            if (hit.StunDuration > 0f && body != null) body.linearVelocity = Vector2.zero;
            health = Mathf.Max(0f, health - hit.Damage);
            if (restore != null) StopCoroutine(restore);
            restore = StartCoroutine(FlashAndRestore(health <= 0f));
        }
        IEnumerator FlashAndRestore(bool depleted)
        {
            if (spriteRenderer != null) spriteRenderer.color = tuning.hitColor;
            yield return new WaitForSeconds(tuning.flashDuration);
            if (spriteRenderer != null) spriteRenderer.color = baseColor;
            if (depleted)
            {
                yield return new WaitForSeconds(tuning.respawnDelay);
                health = tuning.maxHealth;
                if (body != null) body.linearVelocity = Vector2.zero;
            }
            restore = null;
        }

    }
}
