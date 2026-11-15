using System.Collections;
using UnityEngine;

namespace Game.Combat
{
    public class TrainingDummy : MonoBehaviour, IDamageable
    {
        [SerializeField] TrainingDummyTuning tuning;
        Rigidbody2D body;
        float health;
        CombatHitReaction reaction;
        public float Health => health;

        // Start: testler bileşeni AddComponent ile ekleyip ayarı sonra atar; Awake o ana kadar çalışmış olur.
        void Start()
        {
            if (tuning == null) { Debug.LogError("[TrainingDummy] TrainingDummyTuning atanmalı.", this); enabled = false; return; }
            health = tuning.maxHealth; body = GetComponent<Rigidbody2D>(); reaction = GetComponent<CombatHitReaction>();
        }
        public void TakeHit(HitInfo hit)
        {
            if (health <= 0f) return;
            if (hit.StunDuration > 0f && body != null) body.linearVelocity = Vector2.zero;
            health = Mathf.Max(0f, health - hit.Damage);
            reaction?.Play(hit.Hitstop, tuning.flashDuration, tuning.hitColor);
            if (health <= 0f) StartCoroutine(Respawn());
        }

        IEnumerator Respawn()
        { yield return new WaitForSeconds(tuning.respawnDelay); health = tuning.maxHealth; if (body != null) body.linearVelocity = Vector2.zero; }

    }
}
