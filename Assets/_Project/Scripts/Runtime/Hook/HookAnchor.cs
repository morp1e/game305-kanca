using UnityEngine;

namespace Game.Hook
{
    /// <summary>
    /// Kılıcın saplanabildiği işaretli nokta. Anchor katmanında bir Collider2D ister.
    /// SÖZLEŞME İSKELETİ: gövde Lane B tarafından yazılacak.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class HookAnchor : MonoBehaviour
    {
        [SerializeField] SpriteRenderer visual;
        [SerializeField] Color idleTint = new Color(0.8f, 0.8f, 0.8f, 1f);
        [SerializeField] Color targetedTint = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] Color activeTint = new Color(1f, 0.45f, 0.1f, 1f);

        /// <summary>Kılıcın saplandığı dünya noktası.</summary>
        public Vector2 Point => transform.position;

        void Awake()
        {
            if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();
            SetVisualState(AnchorVisualState.Idle);
        }

        public void SetVisualState(AnchorVisualState state)
        {
            if (visual == null) return;
            switch (state)
            {
                case AnchorVisualState.Targeted: visual.color = targetedTint; break;
                case AnchorVisualState.Active: visual.color = activeTint; break;
                default: visual.color = idleTint; break;
            }
        }
    }
}
