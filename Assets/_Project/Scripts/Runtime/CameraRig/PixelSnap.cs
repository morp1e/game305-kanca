using UnityEngine;

namespace Game.CameraRig
{
    /// <summary>
    /// Piksel katmanındaki bir görseli 1/pixelsPerUnit ızgarasına oturtur. Fizik kökü değil, onun görsel çocuğu üzerinde durur.
    /// SÖZLEŞME İSKELETİ: gövde Lane D tarafından yazılacak. Alan adları (pixelsPerUnit) değişmez.
    /// </summary>
    public class PixelSnap : MonoBehaviour
    {
        [SerializeField] int pixelsPerUnit = 16;
    }
}
