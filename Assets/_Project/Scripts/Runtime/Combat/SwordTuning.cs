using UnityEngine;

namespace Game.Combat
{
    [CreateAssetMenu(menuName = "Kanca/Tuning/Sword", fileName = "SwordTuning")]
    public class SwordTuning : ScriptableObject
    {
        [Header("Zamanlama")]
        [Tooltip("Savuruş hazırlığı, s. Hollow Knight nail savuruşu yaklaşık 0,35 s. Ö: oynayarak ayarlanacak.")] public float windupSeconds = 0.05f;
        [Tooltip("Hasar veren savuruş süresi, s. Hollow Knight nail savuruşu yaklaşık 0,35 s. Ö: oynayarak ayarlanacak.")] public float activeSeconds = 0.14f;
        [Tooltip("Savuruş sonrası toparlanma, s. Ö: oynayarak ayarlanacak.")] public float recoverySeconds = 0.06f;
        [Tooltip("Savuruş başlangıçları arasındaki en kısa süre, s. Hollow Knight nail aralığı yaklaşık 0,41 s. Ö: oynayarak ayarlanacak.")] public float swingInterval = 0.4f;
        [Tooltip("Önceki savuruş bitince ters yönlü kombonun geçerli olduğu süre, s. Ö: oynayarak ayarlanacak.")] public float comboWindow = 0.35f;
        [Header("Kılıç")]
        [Tooltip("Savuruş yayı, derece. Ö: oynayarak ayarlanacak.")] public float swingArcDegrees = 100f;
        [Tooltip("Hazırlıkta kılıcın başlangıç açısının gerisine çekilme miktarı, derece. Ö: oynayarak ayarlanacak.")] public float windupPullbackDegrees = 8f;
        [Tooltip("Kılıç ucuna kadar pivot uzunluğu, birim. Ö: oynayarak ayarlanacak.")] public float bladeLength = 1.4f;
        [Tooltip("Kılıç hitbox kalınlığının yarıçapı, birim. Ö: oynayarak ayarlanacak.")] public float bladeRadius = 0.12f;
        [Tooltip("Süpürme sırasında ara hitbox kontrolü için en büyük açı adımı, derece. Ö: oynayarak ayarlanacak.")] public float maxSweepStepDegrees = 4f;
        [Header("Etki")]
        [Tooltip("Vuruş hasarı. Ö: oynayarak ayarlanacak.")] public float damage = 1f;
        [Tooltip("Geri itme dürtüsü. Ö: oynayarak ayarlanacak.")] public float knockback = 8f;
        [Tooltip("Geri itme yönüne eklenecek yukarı bileşen. Ö: oynayarak ayarlanacak.")] public float knockbackUpBias = 0.15f;
        [Tooltip("Vuruştaki oyuncu ve hedef donma süresi, s. Ö: oynayarak ayarlanacak.")] public float hitstop = 0.07f;
        [Tooltip("İsabette kamera travması, 0-1. Ö: oynayarak ayarlanacak.")] [Range(0f, 1f)] public float traumaOnHit = 0.2f;
        [Header("Girdi")]
        [Tooltip("Saldırı basışının saklanacağı süre, s. Ö: oynayarak ayarlanacak.")] public float inputBuffer = 0.3f;
    }
}
