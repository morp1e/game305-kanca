using Unity.Cinemachine;
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

        [SerializeField] Camera targetCamera;

        Vector3 cameraStart;
        Vector3 layerStart;

        void OnEnable()
        {
            if (targetCamera == null)
                targetCamera = Camera.main;
            if (targetCamera == null)
                return;

            cameraStart = targetCamera.transform.position;
            layerStart = transform.position;
            // Cinemachine kamerayı LateUpdate'te taşır; kendi LateUpdate'imiz ondan önce koşarsa katman bir kare geride kalıp titrer.
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
        }

        void OnDisable() => CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);

        void OnCameraUpdated(CinemachineBrain brain)
        {
            if (targetCamera != null && brain.OutputCamera == targetCamera)
                transform.position = ParallaxMath.Position(layerStart, cameraStart, targetCamera.transform.position, factor);
        }
    }
}
