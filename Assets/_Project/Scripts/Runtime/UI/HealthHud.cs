using System.Collections;
using System.Collections.Generic;
using Game.Core;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    /// <summary>
    /// Oyuncu canını ikon dizisi olarak gösterir. Can kanalına bağlanır, oyuncu koduna bağlanmaz.
    /// İkonlar bir şablondan üretilir; en yüksek can değişirse sayı güncellenir.
    /// </summary>
    public sealed class HealthHud : MonoBehaviour
    {
        [SerializeField] HealthEvent health;
        [SerializeField] RectTransform container;
        [SerializeField] Image iconTemplate;
        [SerializeField] Color fullColor = UiPalette.Text;
        [SerializeField] Color emptyColor = UiPalette.Empty;
        [Tooltip("Can kaybında ikonun vurgu süresi, s. Ö: oynayarak ayarlanacak.")]
        [SerializeField] float pulseDuration = 0.25f;
        [Tooltip("Vurgunun başındaki ikon ölçeği. Ö: oynayarak ayarlanacak.")]
        [SerializeField] float pulseScale = 1.6f;

        readonly List<Image> icons = new List<Image>();
        int filledCount;
        float lastCurrent = -1f;

        public int IconCount => icons.Count;
        public int FilledCount => filledCount;

        void Awake()
        {
            if (iconTemplate != null) iconTemplate.gameObject.SetActive(false);
        }

        void OnEnable()
        {
            if (health != null) health.Register(Show);
        }

        void OnDisable()
        {
            if (health != null) health.Unregister(Show);
        }

        void Show(float current, float max)
        {
            if (container == null || iconTemplate == null) return;
            int count = Mathf.Max(0, Mathf.CeilToInt(max));
            while (icons.Count < count)
            {
                var icon = Instantiate(iconTemplate, container);
                icon.gameObject.SetActive(true);
                icons.Add(icon);
            }
            while (icons.Count > count)
            {
                var icon = icons[icons.Count - 1];
                icons.RemoveAt(icons.Count - 1);
                Destroy(icon.gameObject);
            }

            filledCount = Mathf.Clamp(Mathf.CeilToInt(current), 0, count);
            for (int i = 0; i < count; i++) icons[i].color = i < filledCount ? fullColor : emptyColor;

            // Kaybedilen ikonlar kısa bir vurgu yapar.
            if (lastCurrent >= 0f && current < lastCurrent && isActiveAndEnabled)
            {
                int lostTo = Mathf.Min(count, Mathf.CeilToInt(lastCurrent));
                for (int i = filledCount; i < lostTo; i++) StartCoroutine(Pulse(icons[i].rectTransform));
            }
            lastCurrent = current;
        }

        IEnumerator Pulse(RectTransform target)
        {
            float elapsed = 0f;
            while (elapsed < pulseDuration && target != null)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = pulseDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / pulseDuration);
                target.localScale = Vector3.one * Mathf.Lerp(pulseScale, 1f, t);
                yield return null;
            }
            if (target != null) target.localScale = Vector3.one;
        }
    }
}
