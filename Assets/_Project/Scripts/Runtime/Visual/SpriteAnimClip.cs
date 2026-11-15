using System;
using UnityEngine;

namespace Game.Visual
{
    [Serializable]
    public sealed class SpriteAnimClip
    {
        [Tooltip("Klipin adı; çalışma anında bu adla çağrılır.")]
        public string name;
        [Tooltip("Animasyon kareleri sırayla.")]
        public Sprite[] frames;
        [Min(0.01f), Tooltip("Saniyedeki kare sayısı.")]
        public float fps = 10f;
        [Tooltip("Açıksa animasyon son kareden ilk kareye döner.")]
        public bool loop = true;
        [Tooltip("Olay gönderilecek kare; -1 olay olmadığını belirtir.")]
        public int eventFrame = -1;
    }
}
