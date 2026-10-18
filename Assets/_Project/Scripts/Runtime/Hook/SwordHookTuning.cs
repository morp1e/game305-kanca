using UnityEngine;

namespace Game.Hook
{
    /// <summary>Kanca uçuşu ve çekişi için ayarlar.</summary>
    [CreateAssetMenu(menuName = "Kanca/Tuning/Sword Hook", fileName = "SwordHookTuning")]
    public class SwordHookTuning : ScriptableObject
    {
        [Tooltip("Fırlatma hızı, u/s. Ö: oynayarak ayarlanacak.")]
        public float throwSpeed = 40f;
        [Tooltip("En uzun fırlatma menzili, u. Ö: oynayarak ayarlanacak.")]
        public float maxRange = 10f;
        [Tooltip("Kılıç CircleCast yarıçapı, u. Ö: oynayarak ayarlanacak.")]
        public float swordRadius = 0.15f;
        [Tooltip("Çekiş hızı, u/s. C: Celeste 240 px/s ÷ 8.")]
        public float pullSpeed = 30f;
        [Tooltip("Normalize çekiş ilerlemesine göre hız. Ö: düz 1.")]
        public AnimationCurve pullSpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [Tooltip("Azami çekiş süresi, s. Ö: oynayarak ayarlanacak.")]
        public float maxPullTime = 0.6f;
        [Tooltip("Varış eşiği, u. Ö: oynayarak ayarlanacak.")]
        public float arriveDistance = 0.5f;
        [Tooltip("Varış fırlatma hızı çarpanı. C: Celeste 160 / 240.")]
        public float exitSpeedMult = 0.667f;
        [Tooltip("Yukarı fırlatma çarpanı. C: Celeste EndDashUpMult.")]
        public float exitUpMult = 0.75f;
        [Tooltip("Çekiş yönündeki mevcut yüksek hızı koru. C: Celeste.")]
        public bool keepFasterIncomingSpeed = true;
        [Tooltip("Fırlama sonrası zıplama penceresi, s. C: Celeste.")]
        public float postLaunchJumpWindow = 0.1f;
        [Tooltip("Yeni fırlatma için bekleme, s. C: Celeste.")]
        public float hookCooldown = 0.2f;
        [Tooltip("Geri çağrılan kılıç hızı, u/s. Ö: oynayarak ayarlanacak.")]
        public float recallSpeed = 50f;
        [Tooltip("Saplanma anındaki yerel donma süresi, s. C: Celeste.")]
        public float impactFreeze = 0.05f;
        [Tooltip("Çekiş sarsıntı süresi, s. C: Celeste.")]
        public float pullShakeDuration = 0.2f;
        [Tooltip("Saplanma kamera travması, 0-1. E: Eiserloh GDC 2016.")]
        public float traumaOnStick = 0.2f;
        [Tooltip("Büyük çarpışma kamera travması, 0-1. E: Eiserloh GDC 2016.")]
        public float traumaOnBigImpact = 0.5f;
        [Tooltip("Sarsıntı eğrisi üssü. E: Eiserloh GDC 2016.")]
        public float shakeExponent = 2f;
        [Tooltip("Travma azalma hızı, /s. Ö: oynayarak ayarlanacak.")]
        public float traumaDecay = 1f;
        [Tooltip("Nişan yakalama yarıçapı, ekran px. Ö: 1080p referans.")]
        public float snapRadiusPx = 64f;
        [Tooltip("Hedef değiştirme skor farkı. Ö: oynayarak ayarlanacak.")]
        public float switchMargin = 0.15f;
        [Tooltip("Fırlatma başlangıcının oyuncu merkezine göre ofseti. Ö: oynayarak ayarlanacak.")]
        public Vector2 throwOriginOffset = new Vector2(0.5f, 0f);
        [Tooltip("Fırlatma girdisi tamponu, s. Ö: oynayarak ayarlanacak.")]
        public float inputBufferTime = 0.1f;
    }
}
