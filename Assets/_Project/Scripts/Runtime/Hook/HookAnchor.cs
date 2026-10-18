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
        /// <summary>Kılıcın saplandığı dünya noktası.</summary>
        public Vector2 Point => transform.position;
    }
}
