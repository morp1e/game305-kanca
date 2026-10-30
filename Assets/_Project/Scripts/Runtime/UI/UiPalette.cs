using UnityEngine;

namespace Game.UI
{
    /// <summary>Arayüzün ortak renkleri. Editör fabrikası ve bileşen varsayılanları buradan okur; tema tek yerden değişir.</summary>
    public static class UiPalette
    {
        public static readonly Color Ink = new Color32(0x0B, 0x0E, 0x16, 0xFF);
        public static readonly Color Panel = new Color32(0x12, 0x18, 0x26, 0xEB);
        public static readonly Color Text = new Color32(0xED, 0xE6, 0xD6, 0xFF);
        public static readonly Color TextDim = new Color32(0x9A, 0xA3, 0xB5, 0xFF);
        public static readonly Color Accent = new Color32(0xE8, 0xB0, 0x4A, 0xFF);
        public static readonly Color Damage = new Color32(0xB5, 0x40, 0x2F, 0xFF);
        public static readonly Color Empty = new Color32(0x3A, 0x42, 0x56, 0xFF);
        public static readonly Color Button = new Color32(0x1B, 0x22, 0x33, 0xFF);
        public static readonly Color ButtonHover = new Color32(0x2A, 0x35, 0x50, 0xFF);
    }
}
