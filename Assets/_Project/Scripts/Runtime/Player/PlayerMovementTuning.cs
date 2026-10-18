using UnityEngine;

namespace Game.Player
{
    /// <summary>Hareket ayarları. Alanlar Lane A (hareket) tarafından doldurulacak. 1 unit = 1 kare (16 px).</summary>
    [CreateAssetMenu(menuName = "Kanca/Tuning/Player Movement", fileName = "PlayerMovementTuning")]
    public class PlayerMovementTuning : ScriptableObject
    {
        [Header("Yatay hareket")]
        [Tooltip("Koşu hızı, u/s. C: Celeste 90 px/s, 8 px/u.")] [SerializeField] float maxRunSpeed = 11.25f;
        [Tooltip("Zeminde hızlanma, u/s². C: Celeste 1000 px/s², 8 px/u.")] [SerializeField] float runAccel = 125f;
        [Tooltip("Zeminde yavaşlama, u/s². C: Celeste 400 px/s², 8 px/u.")] [SerializeField] float runReduce = 50f;
        [Tooltip("Havadaki yatay kontrol çarpanı. C: Celeste.")] [SerializeField] float airMult = 0.65f;
        [Tooltip("Koşu hızını aşan aynı yönlü hızın yavaşlaması, u/s². Ö: oynayarak ayarlanacak.")] [SerializeField] float overSpeedDecel = 20f;
        [Tooltip("Zıplarken yatay ek hız, u/s. C: Celeste 40 px/s, 8 px/u.")] [SerializeField] float jumpHBoost = 5f;

        [Header("Dikey hareket")]
        [Tooltip("Yerçekimi, u/s². C: Celeste 900 px/s², 8 px/u.")] [SerializeField] float gravity = 112.5f;
        [Tooltip("Düşüş hızı üst sınırı, u/s. C: Celeste 160 px/s, 8 px/u.")] [SerializeField] float maxFallSpeed = 20f;
        [Tooltip("Tepe noktasındaki yarım yerçekimi eşiği, u/s. C: Celeste 40 px/s, 8 px/u.")] [SerializeField] float halfGravThreshold = 5f;
        [Tooltip("Zıplama hızı, u/s. C: Celeste 105 px/s, 8 px/u.")] [SerializeField] float jumpSpeed = 13.1f;
        [Tooltip("Basılı tutulan değişken zıplama süresi, saniye. C: Celeste.")] [SerializeField] float varJumpTime = 0.2f;
        [Tooltip("Zıplama sonrası yerde affetme süresi, saniye. C: Celeste JumpGraceTime.")] [SerializeField] float coyoteTime = 0.1f;
        [Tooltip("Zıplama tamponu, saniye. Ö: oynayarak ayarlanacak.")] [SerializeField] float jumpBufferTime = 0.1f;
        [Tooltip("Kanca fırlatmasından gelen zıplama bonusu, u/s. Ö: oynayarak ayarlanacak.")] [SerializeField] float launchJumpBonus = 2f;

        [Header("Çarpışma")]
        [Tooltip("Zemin için aşağı sorgu mesafesi, u. Ö: oynayarak ayarlanacak.")] [SerializeField] float groundCheckDistance = 0.08f;
        [Tooltip("Köşe düzeltme mesafesi, piksel. C: Celeste.")] [SerializeField] float cornerCorrectionPx = 4f;
        [Tooltip("Bir unit içindeki piksel sayısı. Ö: proje ölçeği.")] [SerializeField] float pixelsPerUnit = 16f;
        [Tooltip("Süpürme çarpışma payı, u. Ö: oynayarak ayarlanacak.")] [SerializeField] float collisionSkin = 0.01f;
        [Tooltip("Çarpışma normalinin tavan sayılması için gereken aşağı yön bileşeni. Ö: oynayarak ayarlanacak.")] [SerializeField] float ceilingNormalThreshold = 0.5f;
        [Tooltip("Hareket Cast'ini atlamak için gereken en küçük mesafe, u. Ö: oynayarak ayarlanacak.")] [SerializeField] float minimumMoveDistance = 0.001f;
        [Tooltip("Hız gizmosunun gösterdiği süre, saniye. Ö: oynayarak ayarlanacak.")] [SerializeField] float velocityGizmoSeconds = 0.1f;
        [Tooltip("Bir adımda izin verilen çarpışma-kayma yinelemesi. Ö: oynayarak ayarlanacak.")] [SerializeField] int slideIterations = 4;

        public float MaxRunSpeed => maxRunSpeed;
        public float RunAccel => runAccel;
        public float RunReduce => runReduce;
        public float AirMult => airMult;
        public float OverSpeedDecel => overSpeedDecel;
        public float JumpHBoost => jumpHBoost;
        public float Gravity => gravity;
        public float MaxFallSpeed => maxFallSpeed;
        public float HalfGravThreshold => halfGravThreshold;
        public float JumpSpeed => jumpSpeed;
        public float VarJumpTime => varJumpTime;
        public float CoyoteTime => coyoteTime;
        public float JumpBufferTime => jumpBufferTime;
        public float LaunchJumpBonus => launchJumpBonus;
        public float GroundCheckDistance => groundCheckDistance;
        public float CornerCorrectionPx => cornerCorrectionPx;
        public float PixelsPerUnit => pixelsPerUnit;
        public float CollisionSkin => collisionSkin;
        public float CeilingNormalThreshold => ceilingNormalThreshold;
        public float MinimumMoveDistance => minimumMoveDistance;
        public float VelocityGizmoSeconds => velocityGizmoSeconds;
        public int SlideIterations => slideIterations;
    }
}
