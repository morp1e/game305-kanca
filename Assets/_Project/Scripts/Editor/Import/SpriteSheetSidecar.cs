using System;
using System.IO;
using UnityEngine;

namespace Game.Editor.Import
{
    [Serializable]
    public sealed class SpriteSheetSidecar
    {
        [Serializable]
        public sealed class SpriteSheetFramePivot
        {
            public int frame;
            public float[] pivot;
        }

        public int ppu = 16;
        public int frameWidth;
        public int frameHeight;
        public int frames;
        public int columns;
        public float[] pivot = { 0.5f, 0f };
        public SpriteSheetFramePivot[] framePivots;

        public Vector2 PivotForFrame(int frame, float defaultX, float defaultY)
        {
            if (framePivots == null) return new Vector2(defaultX, defaultY);
            foreach (SpriteSheetFramePivot entry in framePivots)
            {
                if (entry == null || entry.frame != frame || entry.pivot == null || entry.pivot.Length != 2) continue;
                return new Vector2(entry.pivot[0], entry.pivot[1]);
            }
            return new Vector2(defaultX, defaultY);
        }

        [Serializable]
        sealed class JsonRoot { public SpriteSheetSidecar data; }

        public static string ResolvePath(string assetPath) => ResolvePath(assetPath, File.Exists);

        public static string ResolvePath(string assetPath, Func<string, bool> exists)
        {
            string preferred = assetPath + ".slice.json";
            if (exists != null && exists(preferred)) return preferred;
            string legacy = Path.ChangeExtension(assetPath, ".slice.json");
            return exists != null && exists(legacy) ? legacy : preferred;
        }

        public static bool TryParse(string json, out SpriteSheetSidecar sidecar)
        {
            sidecar = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                sidecar = JsonUtility.FromJson<SpriteSheetSidecar>(json);
                if (sidecar == null || sidecar.frameWidth <= 0 || sidecar.frameHeight <= 0 || sidecar.frames <= 0) return false;
                if (sidecar.ppu <= 0) sidecar.ppu = 16;
                if (sidecar.pivot == null || sidecar.pivot.Length != 2) sidecar.pivot = new[] { 0.5f, 0f };
                return true;
            }
            catch (ArgumentException) { return false; }
        }
    }

}
