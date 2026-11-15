using Game.Visual;
using UnityEngine;

namespace Game.Boss
{
    /// <summary>Boss durumlarını görsel kliplere çevirir; mekanik süreleri animasyona aktarır.</summary>
    public sealed class BossAnimator : MonoBehaviour
    {
        [SerializeField, Tooltip("Boss görselindeki animasyon oynatıcı.")]
        SpriteAnimPlayer player;
        BossState currentState;
        float durationRemaining;
        bool movingAttack;
        bool playingHurt;
        bool subscribedToPlayer;

        void Awake() => EnsurePlayerBinding();

        void EnsurePlayerBinding()
        {
            if (player == null) player = GetComponent<SpriteAnimPlayer>();
            if (player != null && !subscribedToPlayer)
            {
                player.ClipFinished += OnClipFinished;
                subscribedToPlayer = true;
            }
        }

        void Update()
        {
            durationRemaining = Mathf.Max(0f, durationRemaining - Time.deltaTime);
        }

        public void PlayState(BossState state, float durationSeconds = 0f, bool isMovingAttack = false)
        {
            EnsurePlayerBinding();
            if (player == null) return;

            currentState = state;
            durationRemaining = durationSeconds;
            movingAttack = isMovingAttack;
            playingHurt = false;
            string clipName = ClipFor(state, isMovingAttack);
            if (UsesDuration(state) && durationSeconds > 0f) player.Play(clipName, durationSeconds);
            else player.Play(clipName);
        }

        public void PlayHurt()
        {
            EnsurePlayerBinding();
            if (player == null) return;
            playingHurt = true;
            player.Play("hurt");
        }

        public void SetFlip(bool faceLeft)
        {
            EnsurePlayerBinding();
            player?.SetFlip(faceLeft);
        }

        public static string ClipFor(BossState state, bool isMovingAttack = false)
        {
            switch (state)
            {
                case BossState.Telegraph: return "windup";
                case BossState.Active: return isMovingAttack ? "move" : "attack";
                case BossState.Recover:
                case BossState.ShieldRewind: return "recover";
                case BossState.Staggered: return "stagger";
                case BossState.Dead: return "death";
                case BossState.Dormant:
                case BossState.Intro:
                case BossState.Idle:
                case BossState.Select:
                case BossState.PhaseTransition:
                default: return "idle";
            }
        }

        static bool UsesDuration(BossState state)
        {
            return state == BossState.Telegraph || state == BossState.Active ||
                   state == BossState.Recover || state == BossState.Staggered;
        }

        void OnClipFinished(string clipName)
        {
            if (!playingHurt || clipName != "hurt") return;
            playingHurt = false;
            if (player == null) return;

            string clipNameToResume = ClipFor(currentState, movingAttack);
            if (UsesDuration(currentState) && durationRemaining > 0f)
                player.Play(clipNameToResume, durationRemaining);
            else player.Play(clipNameToResume);
        }

        void OnDestroy()
        {
            if (player != null && subscribedToPlayer) player.ClipFinished -= OnClipFinished;
        }
    }
}
