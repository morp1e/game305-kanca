using System;
using UnityEngine;

namespace Game.Boss
{
    [Serializable]
    public struct BossAttackEntry
    {
        public BossAttack attack;
        public float weight;
        public float minRange;
        public float maxRange;
    }

    [CreateAssetMenu(menuName = "Kanca/Boss/Phase", fileName = "BossPhase_")]
    public class BossPhase : ScriptableObject
    {
        [Tooltip("Bu canın altında faz kuyruğa alınır.")] public float enterAtHealth = 10f;
        [Tooltip("Faz geçişi, s. Ö: oynayarak ayarlanacak.")] public float transitionDuration = 2f;
        [Tooltip("Telegraph çarpanı.")] public float telegraphScale = 0.8f;
        [Tooltip("En kısa telegraph, s. Ö: oynayarak ayarlanacak.")] public float minTelegraph = 0.45f;
        [Tooltip("Saldırılar arasındaki en kısa süre, s.")] public float idleMin = 0.3f;
        [Tooltip("Saldırılar arasındaki en uzun süre, s.")] public float idleMax = 0.6f;
        [Tooltip("Bu fazda kalkan bulunur.")] public bool hasShield;
        [Tooltip("Ağırlıklı ve menzile göre süzülen saldırılar.")] public BossAttackEntry[] attacks = Array.Empty<BossAttackEntry>();
    }
}
