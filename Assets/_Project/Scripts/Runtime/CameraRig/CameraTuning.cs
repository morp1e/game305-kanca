using UnityEngine;

namespace Game.CameraRig
{
    /// <summary>Kamera ve sarsıntı ayarları. Alanlar Lane D (kamera) tarafından doldurulacak.</summary>
    [CreateAssetMenu(menuName = "Kanca/Tuning/Camera", fileName = "CameraTuning")]
    public class CameraTuning : ScriptableObject
    {
        [Tooltip("Travma saniyede ne kadar azalır. Kaynak: Ö.")]
        [SerializeField, Min(0f)] float traumaDecay = 1f;
        [Tooltip("Travma sarsıntı eğrisinin üssü. Kaynak: E.")]
        [SerializeField, Min(0.01f)] float shakeExponent = 2f;
        [Tooltip("En yüksek konum sarsıntısı, unit. Kaynak: Ö.")]
        [SerializeField, Min(0f)] float maxShakeOffset = 0.5f;
        [Tooltip("En yüksek açı sarsıntısı, derece. Kaynak: E/Ö.")]
        [SerializeField, Min(0f)] float maxShakeAngle = 3f;
        [Tooltip("Perlin gürültüsünün frekansı, Hz. Kaynak: Ö.")]
        [SerializeField, Min(0f)] float noiseFrequency = 25f;
        [Tooltip("Yönlü vuruşun süresi, saniye. Kaynak: C.")]
        [SerializeField, Min(0.001f)] float directionalKickDuration = 0.2f;
        [Tooltip("Yönlü vuruşun en yüksek konumu, unit. Kaynak: Ö.")]
        [SerializeField, Min(0f)] float directionalKickStrength = 0.15f;
        [Tooltip("Sarsıntı ofsetini 1/16 unit ızgarasına oturtur. Kaynak: Ö.")]
        [SerializeField] bool snapShakeToPixels = true;

        public float TraumaDecay => traumaDecay;
        public float ShakeExponent => shakeExponent;
        public float MaxShakeOffset => maxShakeOffset;
        public float MaxShakeAngle => maxShakeAngle;
        public float NoiseFrequency => noiseFrequency;
        public float DirectionalKickDuration => directionalKickDuration;
        public float DirectionalKickStrength => directionalKickStrength;
        public bool SnapShakeToPixels => snapShakeToPixels;
    }
}
