using System;
using UnityEngine;

namespace Game.Visual
{
    [CreateAssetMenu(menuName = "Kanca/Görsel/Sprite Animasyon Seti", fileName = "SpriteAnimSet")]
    public sealed class SpriteAnimSet : ScriptableObject
    {
        [Tooltip("Bu karakter veya görsel için animasyon klipleri.")]
        public SpriteAnimClip[] clips = Array.Empty<SpriteAnimClip>();
        [Tooltip("SpriteRenderer görselinin yerel konum farkı.")]
        public Vector2 visualOffset;
        [Min(0.01f), Tooltip("Yalnız görsel köküne uygulanan ölçek.")]
        public float visualScale = 1f;
        [Tooltip("Kareler sola bakıyorsa aynalama hesabını buna göre yapar.")]
        public bool spritesFaceLeft;

        public SpriteAnimClip Find(string clipName)
        {
            if (string.IsNullOrEmpty(clipName) || clips == null) return null;
            foreach (SpriteAnimClip clip in clips)
                if (clip != null && string.Equals(clip.name, clipName, StringComparison.OrdinalIgnoreCase)) return clip;
            return null;
        }
    }
}
