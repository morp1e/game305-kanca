using System.Collections;
using UnityEngine;

namespace Game.UI
{
    /// <summary>Tam ekran karartma. Zaman ölçeğinden bağımsız çalışır; duraklatmada ve sahne yüklenirken de kararır.</summary>
    public sealed class ScreenFader : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;

        public float Alpha => group != null ? group.alpha : 0f;

        void Awake()
        {
            if (group == null) group = GetComponentInChildren<CanvasGroup>();
            if (group != null)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
        }

        public IEnumerator FadeOut(float duration) => Fade(1f, duration);

        public IEnumerator FadeIn(float duration) => Fade(0f, duration);

        IEnumerator Fade(float target, float duration)
        {
            if (group == null) yield break;
            // Kararırken tıklamalar alttaki menüye geçmesin.
            group.blocksRaycasts = true;
            float start = group.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            group.alpha = target;
            group.blocksRaycasts = target > 0f;
        }
    }
}
