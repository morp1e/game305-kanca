using Game.Visual;
using Game.Level;
using UnityEngine;

namespace Game.Player
{
    /// <summary>Oyuncu durumunu görsel klip adlarına bağlar; hareket davranışını değiştirmez.</summary>
    public sealed class PlayerAnimDriver : MonoBehaviour
    {
        [SerializeField, Tooltip("Oyuncu hareket durumu.")] PlayerMotor motor;
        [SerializeField, Tooltip("Oyuncu hasar ve ölüm durumu.")] PlayerHealth health;
        [SerializeField, Tooltip("Yön bilgisi için kol rig'i.")] PlayerArmRig armRig;
        [SerializeField, Tooltip("Animasyon setini oynatan bileşen.")] SpriteAnimPlayer animPlayer;
        [SerializeField, Min(0f), Tooltip("Bu yatay hızın üstünde koşu klibi oynar.")] float runSpeedThreshold = 0.1f;

        float hurtRemaining;
        bool dead;
        RespawnController respawn;

        public string ActiveClipName { get; private set; }

        public static string SelectClip(
            Vector2 velocity,
            bool isGrounded,
            bool isWallSliding,
            bool isHurt,
            bool isDead,
            float runSpeedThreshold)
        {
            if (isDead) return "death";
            if (isHurt) return "hurt";
            if (isWallSliding) return "wall_slide";
            if (!isGrounded) return velocity.y > 0.01f ? "jump" : "fall";
            return Mathf.Abs(velocity.x) > runSpeedThreshold ? "run" : "idle";
        }

        public static bool IsFacingLeft(int facing) => facing < 0;

        public float PlayDeath(float maximumDuration)
        {
            if (animPlayer == null) return 0f;
            float clipDuration = animPlayer.GetClipDuration("death");
            if (clipDuration <= 0f) return 0f;
            float duration = Mathf.Min(Mathf.Max(0f, maximumDuration), Mathf.Max(0.5f, clipDuration));
            if (duration <= 0f) return 0f;
            dead = true;
            ActiveClipName = "death";
            animPlayer.Play("death", duration);
            return duration;
        }

        public void ResetAfterRespawn()
        {
            dead = false;
            hurtRemaining = 0f;
            ActiveClipName = null;
            Play("idle");
        }

        void Awake()
        {
            if (motor == null) motor = GetComponentInParent<PlayerMotor>();
            if (health == null) health = GetComponentInParent<PlayerHealth>();
            if (armRig == null) armRig = GetComponentInParent<PlayerArmRig>();
            if (animPlayer == null) animPlayer = GetComponent<SpriteAnimPlayer>();
            respawn = GetComponentInParent<RespawnController>();
        }

        void OnEnable()
        {
            if (health != null)
            {
                health.Damaged += HandleDamaged;
                health.Died += HandleDied;
            }
        }

        void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
                health.Died -= HandleDied;
            }
        }

        void Update()
        {
            if (animPlayer == null || motor == null) return;
            animPlayer.SetFlip(armRig != null && IsFacingLeft(armRig.Facing));
            if (respawn == null) respawn = GetComponentInParent<RespawnController>();
            if (respawn != null && respawn.IsDead) dead = true;
            if (dead && respawn != null && !respawn.IsDead)
            {
                dead = false;
                hurtRemaining = 0f;
                ActiveClipName = null;
            }

            if (health != null && health.CurrentHealth <= 0f) dead = true;
            string clip;
            if (dead)
            {
                clip = SelectClip(
                    motor.Velocity,
                    motor.IsGrounded,
                    motor.IsWallSliding,
                    false,
                    true,
                    runSpeedThreshold);
            }
            else
            {
                hurtRemaining = Mathf.Max(0f, hurtRemaining - Time.deltaTime);
                clip = SelectClip(
                    motor.Velocity,
                    motor.IsGrounded,
                    motor.IsWallSliding,
                    hurtRemaining > 0f,
                    false,
                    runSpeedThreshold);
            }

            if (clip == "death")
            {
                Play(clip);
                return;
            }
            if (clip == "hurt")
            {
                Play(clip, hurtRemaining);
                return;
            }
            Play(clip);
        }

        void HandleDamaged()
        {
            hurtRemaining = health != null ? health.HurtControlLockSeconds : 0f;
        }

        void HandleDied() => dead = true;

        void Play(string clipName)
        {
            if (ActiveClipName == clipName) return;
            ActiveClipName = clipName;
            animPlayer.Play(clipName);
        }

        void Play(string clipName, float durationSeconds)
        {
            if (ActiveClipName == clipName) return;
            ActiveClipName = clipName;
            animPlayer.Play(clipName, durationSeconds);
        }
    }
}
