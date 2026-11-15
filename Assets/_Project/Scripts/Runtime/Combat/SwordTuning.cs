using UnityEngine;

namespace Game.Combat
{
    [CreateAssetMenu(menuName = "Kanca/Tuning/Sword", fileName = "SwordTuning")]
    public class SwordTuning : ScriptableObject
    {
        [Tooltip("Üç vuruşun zamanlama ve etki ayarları. Ö: oynayarak ayarlanacak.")] public ComboStep[] comboSteps = { new ComboStep(), new ComboStep { arcDegrees = 110f }, new ComboStep { windup = 0.05f, active = 0.16f, recovery = 0.24f, arcDegrees = 145f, damage = 2f, knockback = 11f, knockbackUpBias = 0.2f, hitstop = 0.1f, stun = 0.32f } };
        [Tooltip("Adımlar arası başlangıçtan başlangıca asgari süre, saniye. Ö: oynayarak ayarlanacak.")] public float comboStartInterval = 0.3f;
        [Tooltip("Üçüncü vuruştan sonra yeni zincirin geçerli olduğu süre, saniye. Ö: oynayarak ayarlanacak.")] public float finisherComboWindow = 0.45f;
        [Tooltip("Adım başına hasar kapsülünün uzunluğu, birim. Ö: oynayarak ayarlanacak.")] public float bladeLength = 1.4f;
        [Tooltip("Hasar kapsülünün yarıçapı, birim. Ö: oynayarak ayarlanacak.")] public float bladeRadius = 0.12f;
        [Tooltip("Süpürme hitbox'ındaki en büyük açı adımı, derece. Ö: oynayarak ayarlanacak.")] public float maxSweepStepDegrees = 4f;
        [Tooltip("Savuruşun görünür hazırlık geri çekilmesi, derece. Ö: oynayarak ayarlanacak.")] public float windupPullbackDegrees = 8f;
        [Tooltip("Kurbanın beyaz flaş süresi, saniye. Ö: oynayarak ayarlanacak.")] public float hitFlashDuration = 0.12f;
        [Tooltip("Hitstop sarsıntısının sprite üzerindeki en yüksek mesafesi, birim. Ö: oynayarak ayarlanacak.")] public float hitShakeDistance = 0.035f;
        [Tooltip("Saldırı basışının saklanacağı süre, saniye. Ö: oynayarak ayarlanacak.")] public float inputBuffer = 0.3f;
        [Tooltip("Nişan yardımının hedef arama menzili, birim. Ö: oynayarak ayarlanacak.")] public float aimAssistRange = 2.2f;
        [Tooltip("Nişan yardımı konisinin yarı açısı, derece; sıfır kapatır. Ö: oynayarak ayarlanacak.")] public float aimAssistDegrees = 12f;
        [Tooltip("Kılıç hitbox'ının taradığı katmanlar. Varsayılan Enemy ve Default. Ö: oynayarak ayarlanacak.")] public LayerMask hitLayers = (1 << 10) | 1;
        [Tooltip("Nişan yardımı görüş hattını kesen katmanlar. Varsayılan Ground, kancalanabilir katmanlar eklenebilir. Ö: oynayarak ayarlanacak.")] public LayerMask aimAssistBlockingLayers = 1 << 6;
        [Tooltip("Prosedürel hilalin rengi. Ö: oynayarak ayarlanacak.")] public Color smearColor = new Color(1f, 0.88f, 0.52f, 0.8f);
        [Tooltip("Prosedürel hilalin kalınlığı, birim. Ö: oynayarak ayarlanacak.")] public float smearThickness = 0.12f;
        [Tooltip("Hilalin aktif pencere sonundaki sönme süresi, saniye. Ö: oynayarak ayarlanacak.")] public float smearFadeSeconds = 0.05f;
        [Tooltip("Sprite savuruş animasyon karelerinin süresi, saniye. Ö: oynayarak ayarlanacak.")] public float spriteFrameSeconds = 0.035f;
        [Tooltip("Kılıç kıvılcığının ömrü, saniye. Ö: oynayarak ayarlanacak.")] public float sparkLifetime = 0.18f;
        [Tooltip("Kıvılcık parçacıklarının boyutu. Ö: oynayarak ayarlanacak.")] public float sparkSize = 0.08f;
        [Tooltip("Kıvılcık parçacıklarının başlangıç hızı. Ö: oynayarak ayarlanacak.")] public float sparkSpeed = 2f;
        [Tooltip("Kılıç isabet parçacıklarının URP 2D malzemesi. Ö: kurucu oluşturur.")] public Material sparkMaterial;
        [Tooltip("Normal isabetteki parçacık sayısı. Ö: oynayarak ayarlanacak.")] public int sparkCount = 7;
        [Tooltip("Bitiriş ve öldürme isabetindeki parçacık sayısı. Ö: oynayarak ayarlanacak.")] public int finisherSparkCount = 12;
        [Tooltip("Öldürücü vuruş sonrası yavaşlama süresi, saniye. Ö: oynayarak ayarlanacak.")] public float killSlowDuration = 0.25f;
        [Tooltip("Öldürücü vuruş sonrası zaman ölçeği. Ö: oynayarak ayarlanacak.")] [Range(0.05f, 1f)] public float killSlowScale = 0.35f;
        [Tooltip("Ortak hitstop sırasındaki zaman ölçeği. Ö: oynayarak ayarlanacak.")] [Range(0.01f, 0.2f)] public float hitstopScale = 0.05f;
        [Tooltip("Vuruştaki kamera travması, 0-1. Ö: oynayarak ayarlanacak.")] [Range(0f, 1f)] public float traumaOnHit = 0.2f;
        [Tooltip("Vuruş kamerası tepkisinin büyüklüğü, 0-1. Ö: oynayarak ayarlanacak.")] [Range(0f, 1f)] public float cameraKick = 0.15f;
        [Tooltip("Adım 1 savuruş sesi. Ö: oynayarak ayarlanacak.")] public AudioClip slash1Audio;
        [Tooltip("Adım 2 savuruş sesi. Ö: oynayarak ayarlanacak.")] public AudioClip slash2Audio;
        [Tooltip("Adım 3 savuruş sesi. Ö: oynayarak ayarlanacak.")] public AudioClip slash3Audio;
        [Tooltip("Normal isabet sesi. Ö: oynayarak ayarlanacak.")] public AudioClip hitAudio;
        [Tooltip("Bitiriş isabet sesi. Ö: oynayarak ayarlanacak.")] public AudioClip finisherHitAudio;
        [Tooltip("Öldürme sesi. Ö: oynayarak ayarlanacak.")] public AudioClip killAudio;
    }
}
