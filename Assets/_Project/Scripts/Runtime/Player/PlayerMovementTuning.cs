using UnityEngine;

namespace Game.Player
{
    // Celeste Player.cs (MIT, (c) 2018 Noel Berry) uyarlaması; bkz. tools/reference/celeste/LICENSE
    /// <summary>Hareket ayarları. Alanlar Lane A (hareket) tarafından doldurulacak. 1 unit = 1 kare (16 px).</summary>
    [CreateAssetMenu(menuName = "Kanca/Tuning/Player Movement", fileName = "PlayerMovementTuning")]
    public class PlayerMovementTuning : ScriptableObject
    {
        [Header("Yatay hareket")]
        [Tooltip("Koşu hızı, u/s. C: Celeste MaxRun 90 px/s, 8 px/u.")] [SerializeField] float maxRunSpeed = 11.25f;
        [Tooltip("Zeminde hızlanma, u/s². C: Celeste RunAccel 1000 px/s², 8 px/u.")] [SerializeField] float runAccel = 125f;
        [Tooltip("Maksimum koşu hızına dönüş, u/s². C: Celeste RunReduce 400 px/s², 8 px/u.")] [SerializeField] float runReduce = 50f;
        [Tooltip("Havadaki yatay kontrol çarpanı. C: Celeste AirMult 0.65, 8 px/u.")] [SerializeField] float airMult = 0.65f;
        [Tooltip("Kanca fırlatma sonrası girdisiz aşırı hızın yavaşlaması, u/s². Ö: Celeste sapması.")] [SerializeField] float overSpeedDecel = 20f;
        [Tooltip("Zıplarken yatay ek hız, u/s. C: Celeste JumpHBoost 40 px/s, 8 px/u.")] [SerializeField] float jumpHBoost = 5f;

        [Header("Dikey hareket")]
        [Tooltip("Yerçekimi, u/s². C: Celeste Gravity 900 px/s², 8 px/u.")] [SerializeField] float gravity = 112.5f;
        [Tooltip("Düşüş hızı üst sınırı, u/s. C: Celeste MaxFall 160 px/s, 8 px/u.")] [SerializeField] float maxFallSpeed = 20f;
        [Tooltip("Tepe noktasındaki yarım yerçekimi eşiği, u/s. C: Celeste HalfGravThreshold 40 px/s, 8 px/u.")] [SerializeField] float halfGravThreshold = 5f;
        [Tooltip("Zıplama hızı, u/s. C: Celeste JumpSpeed 105 px/s, 8 px/u.")] [SerializeField] float jumpSpeed = 13.125f;
        [Tooltip("Değişken zıplama süresi, saniye. C: Celeste VarJumpTime 0.2 s, 8 px/u.")] [SerializeField] float varJumpTime = 0.2f;
        [Tooltip("Zıplama sonrası yerde affetme süresi, saniye. C: Celeste JumpGraceTime 0.1 s, 8 px/u.")] [SerializeField] float coyoteTime = 0.1f;
        [Tooltip("Zıplama tamponu, saniye. C: Celeste input buffer 0.08 s, 8 px/u.")] [SerializeField] float jumpBufferTime = 0.08f;
        [Tooltip("Kanca fırlatmasından gelen zıplama bonusu, u/s. Ö: oynayarak ayarlanacak.")] [SerializeField] float launchJumpBonus = 2f;

        [Header("Duvar hareketi")]
        [SerializeField] bool wallJumpEnabled = true;
        [Tooltip("Hızlı düşüş üst sınırı, u/s. C: Celeste FastMaxFall 240 px/s, 8 px/u.")] [SerializeField] float fastMaxFallSpeed = 30f;
        [Tooltip("Hızlı düşüş ivmesi, u/s². C: Celeste FastMaxAccel 300 px/s², 8 px/u.")] [SerializeField] float fastMaxAccel = 37.5f;
        [Tooltip("Duvar kayması başlangıç hızı, u/s. C: Celeste WallSlideStartMax 20 px/s, 8 px/u.")] [SerializeField] float wallSlideStartMax = 2.5f;
        [Tooltip("Duvar kayması hızı geçiş süresi, saniye. C: Celeste WallSlideTime 1.2 px/s, 8 px/u.")] [SerializeField] float wallSlideTime = 1.2f;
        [Tooltip("Duvar zıplama yatay hızı, u/s. C: Celeste WallJumpHSpeed 130 px/s, 8 px/u.")] [SerializeField] float wallJumpHSpeed = 16.25f;
        [Tooltip("Duvar zıplamasında yön kilidi, saniye. C: Celeste WallJumpForceTime 0.16 s, 8 px/u.")] [SerializeField] float wallJumpForceTime = 0.16f;
        [Tooltip("Duvar kontrol mesafesi, u. C: Celeste WallJumpCheckDist 3 px, 8 px/u.")] [SerializeField] float wallJumpCheckDist = 0.375f;
        [Tooltip("Tavan temasından sonra değişken zıplama payı, saniye. C: Celeste CeilingVarJumpGrace 0.05 s, 8 px/u.")] [SerializeField] float ceilingVarJumpGrace = 0.05f;
        [Tooltip("Duvar hızını tutma süresi, saniye. C: Celeste WallSpeedRetentionTime 0.06 s, 8 px/u.")] [SerializeField] float wallSpeedRetentionTime = 0.06f;

        [Header("Çarpışma")]
        [Tooltip("Zemin için aşağı sorgu mesafesi, u. Ö: oynayarak ayarlanacak.")] [SerializeField] float groundCheckDistance = 0.08f;
        [Tooltip("Köşe düzeltme mesafesi, px. C: Celeste UpwardCornerCorrection 4 px, 8 px/u.")] [SerializeField] float cornerCorrectionPx = 4f;
        [Tooltip("Bir unit içindeki piksel sayısı. Ö: proje ölçeği.")] [SerializeField] float pixelsPerUnit = 16f;
        [Tooltip("Hız gizmosunun gösterdiği süre, saniye. Ö: oynayarak ayarlanacak.")] [SerializeField] float velocityGizmoSeconds = 0.1f;

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
        public bool WallJumpEnabled => wallJumpEnabled;
        public float FastMaxFallSpeed => fastMaxFallSpeed;
        public float FastMaxAccel => fastMaxAccel;
        public float WallSlideStartMax => wallSlideStartMax;
        public float WallSlideTime => wallSlideTime;
        public float WallJumpHSpeed => wallJumpHSpeed;
        public float WallJumpForceTime => wallJumpForceTime;
        public float WallJumpCheckDist => wallJumpCheckDist;
        public float CeilingVarJumpGrace => ceilingVarJumpGrace;
        public float WallSpeedRetentionTime => wallSpeedRetentionTime;
        public float GroundCheckDistance => groundCheckDistance;
        public float CornerCorrectionPx => cornerCorrectionPx;
        public float PixelsPerUnit => pixelsPerUnit;
        public float VelocityGizmoSeconds => velocityGizmoSeconds;
    }
}
