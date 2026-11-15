using UnityEngine;

namespace Game.Combat
{
    [System.Serializable]
    public class ComboStep
    {
        [Tooltip("Hazırlık süresi, saniye. Ö: oynayarak ayarlanacak.")] public float windup = 0.04f;
        [Tooltip("Aktif hasar penceresi, saniye. Ö: oynayarak ayarlanacak.")] public float active = 0.12f;
        [Tooltip("Toparlanma süresi, saniye. Ö: oynayarak ayarlanacak.")] public float recovery = 0.14f;
        [Tooltip("Savuruş yayı, derece. Ö: oynayarak ayarlanacak.")] public float arcDegrees = 105f;
        [Tooltip("Vuruş hasarı. Ö: oynayarak ayarlanacak.")] public float damage = 1f;
        [Tooltip("Geri itme dürtüsü. Ö: oynayarak ayarlanacak.")] public float knockback = 7f;
        [Tooltip("Geri itme yönüne eklenecek yukarı bileşen. Ö: oynayarak ayarlanacak.")] public float knockbackUpBias = 0.15f;
        [Tooltip("Vuruştaki ortak donma süresi, saniye. Ö: oynayarak ayarlanacak.")] public float hitstop = 0.055f;
        [Tooltip("Kurbanın sersemletme süresi, saniye. Ö: oynayarak ayarlanacak.")] public float stun = 0.18f;
    }
}
