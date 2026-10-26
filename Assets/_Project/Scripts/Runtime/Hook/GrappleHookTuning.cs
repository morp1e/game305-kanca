using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Hook
{
    /// <summary>Kanca uçuşu ve çekişi için ayarlar.</summary>
    [CreateAssetMenu(menuName = "Kanca/Tuning/Grapple Hook", fileName = "GrappleHookTuning")]
    public class GrappleHookTuning : ScriptableObject
    {
        [Header("Fırlatma")]
        [Tooltip("Fırlatma hızı, u/s. Ö: oynayarak ayarlanacak.")] [Min(0f)]
        public float throwSpeed = 40f;
        [Tooltip("En uzun fırlatma menzili, u. Ö: oynayarak ayarlanacak.")]
        public float maxRange = 10f;
        [Tooltip("Kanca CircleCast yarıçapı, u. Ö: oynayarak ayarlanacak.")]
        [FormerlySerializedAs("swordRadius")]
        public float hookRadius = 0.15f;
        [Tooltip("Fırlatma başlangıcının oyuncu merkezine göre ofseti. Ö: oynayarak ayarlanacak.")]
        public Vector2 throwOriginOffset = new Vector2(0.5f, 0f);
        [Tooltip("Fırlatma girdisi tamponu, s. Ö: oynayarak ayarlanacak.")]
        public float inputBufferTime = 0.1f;

        [Header("Tutunma/katmanlar")]
        [Tooltip("Kancanın tutunabildiği katmanlar. Ö: Ground.")]
        // GetMask ScriptableObject kurucusunda çağrılamaz; 1 << 6 = Ground (TagManager).
        public LayerMask hookableLayers = 1 << 6;
        [Tooltip("Kancayla çekilebilen Enemy (10) ve Default (0) katmanları.")]
        // 1 << 10 = Enemy, 1 << 0 = Default. GetMask asset alanı başlatıcısında çağrılamaz.
        public LayerMask pullableLayers = (1 << 10) | (1 << 0);
        [Header("Çekiş")]
        [Tooltip("Çekiş hızı, u/s. C: Celeste 240 px/s ÷ 8.")] [Min(0f)]
        public float pullSpeed = 30f;
        [Tooltip("Normalize çekiş ilerlemesine göre hız. Ö: düz 1.")]
        public AnimationCurve pullSpeedCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
        [Tooltip("Azami çekiş süresi, s. Ö: oynayarak ayarlanacak.")]
        public float maxPullTime = 0.6f;
        [Header("Varış ve fırlama")]
        [Tooltip("Varış eşiği, u. Ö: oynayarak ayarlanacak.")] [Min(0f)]
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
        [Tooltip("Geri çağrılan kanca hızı, u/s. Ö: oynayarak ayarlanacak.")] [Min(0f)]
        public float recallSpeed = 50f;

        [Header("Hafif hedef çekme")]
        [Tooltip("Hafif hedefe çekiş hızı, u/s. Ö: oyuncu çekişiyle aynı, oynayarak ayarlanacak.")] [Min(0f)] public float lightPullSpeed = 30f;
        [Tooltip("Hafif hedef varış mesafesi, u. Ö: oynayarak ayarlanacak.")] [Min(0f)] public float pullableArriveDistance = 0.5f;
        [Tooltip("Hafif hedefe çekiş azami süresi, s. Ö: oynayarak ayarlanacak.")] [Min(0f)] public float pullableMaxTime = 2f;
        [Tooltip("Hafif hedef varışındaki bırakma hızı, u/s. Ö: oynayarak ayarlanacak.")] [Min(0f)] public float pullableReleaseSpeed = 1f;
        [Tooltip("Hafif hedef varışındaki sıfır hasarlı sersemletme süresi, s. Ö: oynayarak ayarlanacak.")] [Min(0f)] public float pullableStunDuration = 0.2f;

        [Header("His/sarsıntı")]
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
    }
}
