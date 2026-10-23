using UnityEngine;

namespace Game.Player
{
    /// <summary>Oyuncu gövde ve kol görsellerinin ayarları.</summary>
    [CreateAssetMenu(menuName = "Kanca/Tuning/Player Rig", fileName = "PlayerRigTuning")]
    public class PlayerRigTuning : ScriptableObject
    {
        [Tooltip("Ö: Gövde merkezine göre ön omuz ofseti, sağa bakarken.")] public Vector2 frontShoulderOffset = new Vector2(0.12f, 0.35f);
        [Tooltip("Ö: Gövde merkezine göre arka omuz ofseti, sağa bakarken.")] public Vector2 backShoulderOffset = new Vector2(-0.12f, 0.2f);
        [Tooltip("Ö: Omuzdan el ucuna kol uzunluğu, unit.")] public float armLength = 0.75f;
        [Tooltip("Ö: Kol açısının yumuşama hızı, 1/s. Sıfır anlık döner.")] public float armRotationSmoothing = 0f;
        [Tooltip("Ö: Kol açısı adımı. Sıfır serbest dönüş, 22.5 = 16 yön, 45 = 8 yön.")] public float angleSnapDegrees = 0f;
        [Tooltip("Ö: Gövde merkezi çevresinde bakış yönünün değişmediği yarıçap, unit.")] public float facingDeadZone = 0.15f;
        [Tooltip("Ö: Bu açı içinde kılıç kolunun dinlenme açısı, derece.")] public float swordRestAngle = 0f;
    }
}
