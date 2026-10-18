using Game.Core;
using Game.Hook;
using UnityEngine;

namespace Game.Feedback
{
    /// <summary>
    /// Kancanın görsel geri bildirimi: nişan çizgisi, kılıç izi, çekiş itkisi.
    /// SÖZLEŞME İSKELETİ: gövde Lane 3. Alan adları (hook, sword, aimLine, trail, kickEvent) değişmez.
    /// </summary>
    public class HookFeedback : MonoBehaviour
    {
        [SerializeField] SwordHook hook;
        [SerializeField] Transform sword;
        [SerializeField] LineRenderer aimLine;
        [SerializeField] TrailRenderer trail;
        [SerializeField] Vector2Event kickEvent;
    }
}
