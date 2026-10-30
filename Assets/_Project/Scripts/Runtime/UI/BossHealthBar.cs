using System.Collections;
using Game.Core;
using TMPro;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// Boss can çubuğu. Dövüş başlayınca adıyla görünür; can düştükçe dolu çubuk anında, arkadaki hasar izi gecikmeyle iner.
    /// Doluluk RectTransform genişliğiyle verilir, sprite gerektirmez. Boss yenilince veya oyuncu yeniden doğunca gizlenir.
    /// </summary>
    public sealed class BossHealthBar : MonoBehaviour
    {
        [SerializeField] StringEvent fightStarted;
        [SerializeField] HealthEvent health;
        [SerializeField] GameEvent defeated;
        [SerializeField] GameEvent playerRespawned;
        [SerializeField] CanvasGroup group;
        [SerializeField] TMP_Text bossName;
        [SerializeField] RectTransform fill;
        [SerializeField] RectTransform trail;
        [Tooltip("Hasar izinin inmeye başlamadan önce beklediği süre, s. Ö: oynayarak ayarlanacak.")]
        [SerializeField] float trailDelay = 0.45f;
        [Tooltip("Hasar izinin inme hızı, çubuk oranı / s. Ö: oynayarak ayarlanacak.")]
        [SerializeField] float trailSpeed = 0.8f;
        [Tooltip("Görünme ve gizlenme geçişi, s. Ö: oynayarak ayarlanacak.")]
        [SerializeField] float fadeDuration = 0.3f;
        [Tooltip("Boss yenildikten sonra çubuğun ekranda kalma süresi, s. Ö: oynayarak ayarlanacak.")]
        [SerializeField] float defeatedHold = 1.5f;

        float shown = 1f;
        float trailValue = 1f;
        float trailHoldUntil;
        bool visible;
        Coroutine fadeRoutine;

        /// <summary>Son istenen görünürlük (geçiş sürerken de hedef değeri verir).</summary>
        public bool IsVisible => visible;
        public float FillRatio => fill != null ? fill.anchorMax.x : 0f;
        public float TrailRatio => trail != null ? trail.anchorMax.x : 0f;

        void Awake()
        {
            if (group != null) group.alpha = 0f;
            SetRatio(fill, 1f);
            SetRatio(trail, 1f);
        }

        void OnEnable()
        {
            if (fightStarted != null) fightStarted.Register(OnStarted);
            if (health != null) health.Register(OnHealth);
            if (defeated != null) defeated.Register(OnDefeated);
            if (playerRespawned != null) playerRespawned.Register(OnPlayerRespawned);
        }

        void OnDisable()
        {
            if (fightStarted != null) fightStarted.Unregister(OnStarted);
            if (health != null) health.Unregister(OnHealth);
            if (defeated != null) defeated.Unregister(OnDefeated);
            if (playerRespawned != null) playerRespawned.Unregister(OnPlayerRespawned);
        }

        void Update()
        {
            if (trail == null || Time.unscaledTime < trailHoldUntil || Mathf.Approximately(trailValue, shown)) return;
            trailValue = Mathf.MoveTowards(trailValue, shown, trailSpeed * Time.unscaledDeltaTime);
            SetRatio(trail, trailValue);
        }

        void OnStarted(string displayName)
        {
            if (bossName != null) bossName.text = displayName;
            FadeTo(true, 0f);
        }

        void OnHealth(float current, float max)
        {
            float ratio = max <= 0f ? 0f : Mathf.Clamp01(current / max);
            if (ratio < shown) trailHoldUntil = Time.unscaledTime + trailDelay;
            shown = ratio;
            SetRatio(fill, ratio);
            // Can artınca (dövüş sıfırlandı) iz beklemeden eşitlenir.
            if (ratio >= trailValue)
            {
                trailValue = ratio;
                SetRatio(trail, ratio);
            }
        }

        void OnDefeated() => FadeTo(false, defeatedHold);

        void OnPlayerRespawned() => FadeTo(false, 0f);

        void FadeTo(bool show, float delay)
        {
            visible = show;
            if (group == null) return;
            if (fadeRoutine != null) StopCoroutine(fadeRoutine);
            float target = show ? 1f : 0f;
            if (!isActiveAndEnabled)
            {
                group.alpha = target;
                return;
            }
            fadeRoutine = StartCoroutine(Fade(target, delay));
        }

        IEnumerator Fade(float target, float delay)
        {
            if (delay > 0f) yield return new WaitForSecondsRealtime(delay);
            float from = group.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, target, fadeDuration <= 0f ? 1f : elapsed / fadeDuration);
                yield return null;
            }
            group.alpha = target;
            fadeRoutine = null;
        }

        static void SetRatio(RectTransform bar, float ratio)
        {
            if (bar == null) return;
            var max = bar.anchorMax;
            max.x = Mathf.Clamp01(ratio);
            bar.anchorMax = max;
        }
    }
}
