using Game.Core;
using Game.Hook;
using UnityEngine;

namespace Game.Dialogue
{
    /// <summary>
    /// Kılıcın konuşması: olaylara göre SwordLineSet'ten replik seçer, kılıcın üstünde balonla gösterir.
    /// SÖZLEŞME İSKELETİ: gövde Lane 2. Alan adları (lines, hook, sword, deathEvent, respawnEvent) değişmez.
    /// </summary>
    public class SwordVoice : MonoBehaviour
    {
        [SerializeField] SwordLineSet lines;
        [SerializeField] SwordHook hook;
        [SerializeField] Transform sword;
        [SerializeField] GameEvent deathEvent;
        [SerializeField] GameEvent respawnEvent;

        /// <summary>Şu an gösterilen replik; yoksa null.</summary>
        public string CurrentLine { get; private set; }
    }
}
