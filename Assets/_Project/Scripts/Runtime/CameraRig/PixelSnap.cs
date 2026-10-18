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

        Vector3 initialLocalPosition;

        void OnEnable() => initialLocalPosition = transform.localPosition;

        void LateUpdate()
        {
            if (pixelsPerUnit <= 0)
                return;

            Vector3 desiredWorld = transform.parent == null
                ? initialLocalPosition
                : transform.parent.TransformPoint(initialLocalPosition);
            Vector3 snappedWorld = SnapMath.Snap(desiredWorld, 1f / pixelsPerUnit);
            Vector3 worldDelta = snappedWorld - transform.position;
            transform.localPosition += transform.parent == null
                ? worldDelta
                : transform.parent.InverseTransformVector(worldDelta);
        }
    }
}
