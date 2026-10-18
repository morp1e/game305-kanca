using Game.Core;
using UnityEngine;

namespace Game.CameraRig
{
    /// <summary>
    /// Trauma tabanlı kamera sarsıntısı (Eiserloh, GDC 2016). traumaEvent'e gelen değerler trauma'ya eklenir.
    /// SÖZLEŞME İSKELETİ: gövde Lane D tarafından yazılacak. Alan adları (tuning, traumaEvent) değişmez.
    /// Cinemachine kamerasının olduğu objeye eklenir.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [SerializeField] CameraTuning tuning;
        [SerializeField] FloatEvent traumaEvent;

        public float Trauma { get; private set; }
    }
}
