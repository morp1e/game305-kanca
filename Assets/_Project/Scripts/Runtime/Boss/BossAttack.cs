using System;
using UnityEngine;

namespace Game.Boss
{
    public enum BossMoveType { Stationary, Charge, Slam, Whip, BeamCollapse }

    [Serializable]
    public struct BossHitbox
    {
        public Vector2 offset;
        public Vector2 size;
        public float activeFrom;
        public float activeTo;
        public float damage;
    }

    [CreateAssetMenu(menuName = "Kanca/Boss/Attack", fileName = "BossAttack_")]
    public class BossAttack : ScriptableObject
    {
        [Tooltip("Saldırı kimliği.")] public string id;
        [Tooltip("Uyarı süresi, s. Ö: oynayarak ayarlanacak.")] public float telegraph = 0.8f;
        [Tooltip("Etkin süre, s. Ö: oynayarak ayarlanacak.")] public float active = 0.5f;
        [Tooltip("Toparlanma süresi, s. Ö: oynayarak ayarlanacak.")] public float recovery = 0.6f;
        [Tooltip("Toparlanma boyunca kalkanı gevşet.")] public bool loosensShield;
        [Tooltip("Hareket türü.")] public BossMoveType movement;
        [Tooltip("Hücum hızı, u/s. Ö: oynayarak ayarlanacak.")] public float moveSpeed = 16f;
        [Tooltip("Saldırı hitbox'ları.")] public BossHitbox[] hitboxes = Array.Empty<BossHitbox>();
        [Tooltip("Etkin sırada üretilecek prefab.")] public GameObject spawnPrefab;
        [Tooltip("Üretilen nesnenin hızı, u/s.")] public float spawnSpeed = 10f;
        [Tooltip("Üretilen nesnenin verdiği hasar. Ö: oynayarak ayarlanacak.")] public float spawnDamage = 1f;
        [Tooltip("Üretilen nesnenin yaşam süresi, s. Ö: oynayarak ayarlanacak.")] public float spawnLifetime = 4f;
        [Tooltip("Arena etkisinin türü.")] public string arenaEffect;
    }
}
