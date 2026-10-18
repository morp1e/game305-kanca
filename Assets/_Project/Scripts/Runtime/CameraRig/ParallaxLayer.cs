using UnityEngine;

namespace Game.CameraRig
{
    /// <summary>
    /// Boyalı arka plan katmanını kameraya göre kaydırır. factor 0: dünyaya sabit, 1: kamerayla birlikte gider.
    /// SÖZLEŞME İSKELETİ: gövde Lane D tarafından yazılacak. Alan adları (factor) değişmez.
    /// </summary>
    public class ParallaxLayer : MonoBehaviour
    {
        [SerializeField] Vector2 factor = new Vector2(0.5f, 0.5f);
    }
}
