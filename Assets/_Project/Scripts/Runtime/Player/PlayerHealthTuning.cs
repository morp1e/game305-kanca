using Game.Level;
using UnityEngine;

namespace Game.Player
{
    [CreateAssetMenu(menuName = "Kanca/Tuning/Player Health", fileName = "PlayerHealthTuning")]
    public class PlayerHealthTuning : ScriptableObject
    {
        [Header("Can ve hasar")]
        [Tooltip("Oyuncunun en yüksek canı. Ö: oynayarak ayarlanacak.")] public float maxHealth = 5f;
        [Tooltip("Hasar sonrası dokunulmazlık süresi, s. Hollow Knight 1,3 s. Ö: oynayarak ayarlanacak.")] public float invulnerabilitySeconds = 1.3f;
        [Tooltip("Hasar sonrası geri itme hızı, u/s. Yatay bileşen kaynaktan uzağa uygulanır. Ö: oynayarak ayarlanacak.")] public Vector2 knockbackSpeed = new Vector2(9f, 7f);
        [Tooltip("Geri itme sırasında yatay girdi ve zıplamanın kilitleneceği süre, s. Hollow Knight 0,2 s. Ö: oynayarak ayarlanacak.")] public float knockbackControlLockSeconds = 0.2f;
        [Tooltip("Kancayla ağır hedefe çekilip varınca o hedefin temas hasarından muaf kalınan süre, s. Ö: oynayarak ayarlanacak.")] public float heavyPullContactGrace = 0.3f;
        [Tooltip("Hasar anında oyuncu gövdesinin yerel donma süresi, s. Ö: oynayarak ayarlanacak.")] public float damageHitstop = 0.08f;
        [Tooltip("Hasar anındaki kamera travması, 0-1. Ö: oynayarak ayarlanacak.")] [Range(0f, 1f)] public float traumaOnDamage = 0.35f;
        [Tooltip("Dokunulmazlık sırasında görselin yanıp sönme aralığı, s. Ö: oynayarak ayarlanacak.")] public float blinkInterval = 0.08f;
        [Header("Dikenler")]
        [Tooltip("Diken teması politikası. Ö: oynayarak ayarlanacak.")] public HazardPolicy hazardPolicy = HazardPolicy.DamageAndReturn;
        [Tooltip("Diken temasında kaybedilecek can. Ö: oynayarak ayarlanacak.")] public float hazardDamage = 1f;
        [Tooltip("Diken temasından güvenli noktaya dönüş gecikmesi, s. Ö: oynayarak ayarlanacak.")] public float hazardReturnDelay = 0.35f;
        [Tooltip("Ölüm sonrası yeniden doğunca canı doldur. Ö: oynayarak ayarlanacak.")] public bool refillOnRespawn = true;
    }
}
