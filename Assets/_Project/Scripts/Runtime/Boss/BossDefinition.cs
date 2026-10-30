using UnityEngine;

namespace Game.Boss
{
    [CreateAssetMenu(menuName = "Kanca/Boss/Definition", fileName = "BossDefinition")]
    public class BossDefinition : ScriptableObject
    {
        [Tooltip("Dövüş başlığında görünen ad.")] public string displayName = "Zincirli Bekçi";
        [Tooltip("Başlangıç canı. Ö: oynayarak ayarlanacak.")] public float maxHealth = 24f;
        [Tooltip("Dövüş başlangıç animasyonu süresi, s.")] public float introDuration = 1f;
        [Tooltip("Fazlar, artan sırayla.")] public BossPhase[] phases;
        [Tooltip("Arkadan kombo için gereken isabet sayısı.")] public int rearComboHits = 6;
        [Tooltip("Kombo isabetleri arasındaki en uzun süre, s.")] public float comboWindow = 2f;
        [Tooltip("Kombo sendelemesi, s.")] public float comboStagger = 1f;
        [Tooltip("Kalkan sökme sendelemesi, s.")] public float shieldStagger = 1.6f;
        [Tooltip("Kalkanın yuvaya dönme süresi, s.")] public float shieldRewind = 0.5f;
        [Tooltip("Gövde temas hasarı.")] public float contactDamage = 1f;
        [Tooltip("Yenilgi olayından oyun sonuna kadar geçen süre, s.")] public float victoryDelay = 2.5f;
        [Tooltip("Kirişin kancalanamaz kaldığı süre, s. Ö: oynayarak ayarlanacak.")] public float beamNonHookableDuration = 3f;
    }
}
