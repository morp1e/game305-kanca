using System.Collections;
using Game.Core;
using Game.Hook;
using UnityEngine;

namespace Game.Boss
{
    /// <summary>Arena girişini, kapıları ve boss dövüşünün yeniden başlatılmasını yönetir.</summary>
    public class BossArena : MonoBehaviour
    {
        [SerializeField] BossBrain boss;
        [SerializeField] BossDefinition definition;
        [SerializeField] Transform player;
        [SerializeField] Collider2D entryTrigger;
        [SerializeField] GameObject[] doors;
        [SerializeField] StringEvent fightStarted;
        [SerializeField] HealthEvent bossHealth;
        [SerializeField] GameEvent playerRespawn;
        [SerializeField] GameEvent bossDefeated;
        [SerializeField] Vector2 arenaSize = new Vector2(28f, 12f);
        [SerializeField] Collider2D[] beamColliders;
        bool started;

        void Awake()
        {
            if (boss != null) boss.SetInitialPosition();
            if (entryTrigger != null) entryTrigger.isTrigger = true;
        }

        void OnEnable()
        {
            playerRespawn?.Register(ResetFight);
            bossDefeated?.Register(OpenDoors);
        }

        void OnDisable()
        {
            playerRespawn?.Unregister(ResetFight);
            bossDefeated?.Unregister(OpenDoors);
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (started || other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
            Rigidbody2D candidate = other.attachedRigidbody;
            if (candidate == null) return;
            player = candidate.transform;
            StartFight();
        }

        public void StartFight()
        {
            if (started) return;
            started = true;
            SetDoors(true);
            if (definition != null) fightStarted?.Raise(definition.displayName);
            if (boss != null) boss.BeginFight();
            if (boss != null) bossHealth?.Raise(boss.Health, definition != null ? definition.maxHealth : boss.Health);
        }

        public void ResetFight()
        {
            started = false;
            SetDoors(false);
            StopAllCoroutines();
            if (beamColliders != null)
                foreach (var beam in beamColliders)
                {
                    if (beam == null) continue;
                    var marker = beam.GetComponent<NonHookable>();
                    if (marker != null) Destroy(marker);
                }
            foreach (var projectile in FindObjectsByType<BossProjectile>(FindObjectsSortMode.None)) Destroy(projectile.gameObject);
            if (boss != null) boss.ResetFight();
        }

        public void OpenDoors() => SetDoors(false);

        void SetDoors(bool closed)
        {
            if (doors == null) return;
            for (int i = 0; i < doors.Length; i++) if (doors[i] != null) doors[i].SetActive(closed);
        }

        /// <summary>
        /// Rastgele bir kirişi süre boyunca kancaya kapatır ve döner. Kirişler arena prefab'ının kendi objeleridir:
        /// bölüm zemini tek Tilemap collider'ı olduğu için haritadaki karoya NonHookable eklemek bütün bölümü kapatırdı.
        /// </summary>
        public Collider2D CollapseBeam(float duration)
        {
            if (beamColliders == null || beamColliders.Length == 0) return null;
            Collider2D beam = beamColliders[Random.Range(0, beamColliders.Length)];
            if (beam == null) return null;
            if (beam.GetComponent<NonHookable>() == null) StartCoroutine(DisableBeamHook(beam.gameObject, duration));
            return beam;
        }

        IEnumerator DisableBeamHook(GameObject beam, float duration)
        {
            beam.AddComponent<NonHookable>();
            var renderer = beam.GetComponent<SpriteRenderer>();
            Color baseColor = renderer != null ? renderer.color : Color.white;
            if (renderer != null) renderer.color = new Color(0.55f, 0.2f, 0.18f);
            yield return new WaitForSeconds(duration);
            if (beam == null) yield break;
            var marker = beam.GetComponent<NonHookable>();
            if (marker != null) Destroy(marker);
            if (renderer != null) renderer.color = baseColor;
        }

        public Vector2 ArenaSize => arenaSize;
        public Transform Player => player;
    }
}
