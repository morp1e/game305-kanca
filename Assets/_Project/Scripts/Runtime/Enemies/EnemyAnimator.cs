using Game.Visual;
using UnityEngine;

namespace Game.Enemies
{
    /// <summary>Düşman mekanik durumlarını görsel animasyon kliplerine bağlar.</summary>
    public sealed class EnemyAnimator : MonoBehaviour
    {
        [SerializeField, Tooltip("Durum değişikliklerini bildiren düşman beyni.")]
        EnemyBrain brain;
        [SerializeField, Tooltip("Animasyon oynatıcısı.")]
        SpriteAnimPlayer player;
        SpriteRenderer spriteRenderer;
        Sprite fallbackSprite;

        void Awake()
        {
            if (brain == null) brain = GetComponent<EnemyBrain>();
            if (player == null) player = GetComponentInChildren<SpriteAnimPlayer>();
            if (player != null) spriteRenderer = player.GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null) fallbackSprite = spriteRenderer.sprite;
        }

        void OnEnable()
        {
            if (brain != null) brain.StateChanged += HandleStateChanged;
        }

        void Start()
        {
            if (brain != null) PlayForState(brain.CurrentState);
        }

        void OnDisable()
        {
            if (brain != null) brain.StateChanged -= HandleStateChanged;
        }

        void HandleStateChanged(EnemyState previous, EnemyState next) => PlayForState(next);

        public void ResetToIdle()
        {
            if (player == null || brain == null) return;
            if (brain.Definition == null || brain.Definition.visualSet == null || brain.Definition.visualSet.Find("idle") == null)
            {
                if (spriteRenderer != null) spriteRenderer.sprite = fallbackSprite;
                return;
            }
            player.Play("idle", true);
        }

        void PlayForState(EnemyState state)
        {
            if (player == null || brain == null) return;
            string clip = ClipName(state, brain is FlyerBrain);
            if (brain.Definition == null || brain.Definition.visualSet == null || brain.Definition.visualSet.Find(clip) == null)
            {
                if (spriteRenderer != null) spriteRenderer.sprite = fallbackSprite;
                return;
            }
            if (state == EnemyState.Telegraph)
                player.Play(clip, brain.Definition != null ? brain.Definition.telegraphDuration : 0f);
            else if (state == EnemyState.Attack)
                player.Play(clip, brain.Definition != null ? brain.Definition.attackDuration : 0f);
            else if (state == EnemyState.Hurt || state == EnemyState.Stunned)
                player.Play(clip, brain.StunDuration);
            else if (state == EnemyState.Recover)
                player.Play(clip, brain.RecoveryDuration);
            else if (state == EnemyState.Dead)
                player.Play(clip, false);
            else
                player.Play(clip);
        }

        public static string ClipName(EnemyState state, bool flyer)
        {
            switch (state)
            {
                case EnemyState.Patrol:
                case EnemyState.Chase:
                    return flyer ? "fly" : "walk";
                case EnemyState.Notice:
                case EnemyState.Pulled:
                    return "idle";
                case EnemyState.Telegraph:
                    return "windup";
                case EnemyState.Attack:
                    return "attack";
                case EnemyState.Recover:
                    return "recover";
                case EnemyState.Hurt:
                case EnemyState.Stunned:
                    return "hurt";
                case EnemyState.Dead:
                    return "death";
                default:
                    return "idle";
            }
        }
    }
}
