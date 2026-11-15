using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Game.Visual;

namespace Game.Editor.Import
{
    [Serializable]
    public sealed class AnimSetRow
    {
        public string set;
        public float[] visualOffset;
        public float visualScale = 1f;
        public bool spritesFaceLeft;
        public AnimClipRow[] clips;
    }

    [Serializable]
    public sealed class AnimClipRow
    {
        public string name;
        public string source;
        public float fps = 10f;
        public bool loop = true;
        public int eventFrame = -1;
        public int[] frames;
    }

    public static class AnimSetJsonParser
    {
        [Serializable]
        sealed class Table { public AnimSetRow[] items; }

        public static bool TryParse(string json, out AnimSetRow[] rows)
        {
            rows = null;
            if (string.IsNullOrWhiteSpace(json)) return false;
            try
            {
                rows = JsonUtility.FromJson<Table>("{\"items\":" + json + "}")?.items;
                if (rows != null)
                    foreach (AnimSetRow row in rows)
                        if (row != null && row.visualScale <= 0f) row.visualScale = 1f;
                return rows != null;
            }
            catch (ArgumentException) { return false; }
        }
    }

    public static class SpriteAnimSetBuilder
    {
        const string TablePath = "Assets/ThirdParty/GothicVania/anim-sets.json";

        [MenuItem("Tools/Kanca/Animasyon Setlerini Kur")]
        public static void BuildFromMenu()
        {
            if (!File.Exists(TablePath)) File.WriteAllText(TablePath, "[]\n");
            string json = File.ReadAllText(TablePath);
            if (!AnimSetJsonParser.TryParse(json, out AnimSetRow[] rows))
            {
                Debug.LogError("Animasyon set tablosu JSON olarak okunamadı: " + TablePath);
                return;
            }
            if (rows == null || rows.Length == 0)
            {
                Debug.Log("Animasyon set tablosu boş. İçerik eklendiğinde menüyü yeniden çalıştırın.");
                return;
            }

            foreach (AnimSetRow row in rows) BuildSet(row);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static void BuildSet(AnimSetRow row)
        {
            if (row == null || string.IsNullOrWhiteSpace(row.set)) return;
            EnsureAssetFolder(Path.GetDirectoryName(row.set)?.Replace('\\', '/'));
            SpriteAnimSet set = AssetDatabase.LoadAssetAtPath<SpriteAnimSet>(row.set);
            if (set == null)
            {
                set = ScriptableObject.CreateInstance<SpriteAnimSet>();
                AssetDatabase.CreateAsset(set, row.set);
            }

            set.visualOffset = row.visualOffset != null && row.visualOffset.Length >= 2
                ? new Vector2(row.visualOffset[0], row.visualOffset[1]) : Vector2.zero;
            set.visualScale = Mathf.Max(0.01f, row.visualScale);
            set.spritesFaceLeft = row.spritesFaceLeft;
            var clips = new List<SpriteAnimClip>();
            if (row.clips != null)
            {
                foreach (AnimClipRow clipRow in row.clips)
                {
                    if (clipRow == null || string.IsNullOrWhiteSpace(clipRow.source)) continue;
                    Sprite[] sprites = LoadSourceSprites(clipRow.source);
                    if (clipRow.frames != null && clipRow.frames.Length >= 2 &&
                        (clipRow.frames[0] < 0 || clipRow.frames[0] >= sprites.Length ||
                         clipRow.frames[1] < clipRow.frames[0] || clipRow.frames[1] >= sprites.Length))
                    {
                        Debug.LogWarning($"Animasyon klibi '{clipRow.name}' kaynağı '{clipRow.source}' için istenen " +
                            $"[{clipRow.frames[0]}, {clipRow.frames[1]}] aralığı {sprites.Length} kareye sığmıyor; " +
                            "var olan kareler kullanılacak.");
                    }
                    sprites = SelectFrames(sprites, clipRow.frames);
                    clips.Add(new SpriteAnimClip
                    {
                        name = clipRow.name,
                        frames = sprites,
                        fps = Mathf.Max(0.01f, clipRow.fps),
                        loop = clipRow.loop,
                        eventFrame = clipRow.eventFrame
                    });
                }
            }
            set.clips = clips.ToArray();
            EditorUtility.SetDirty(set);
        }

        public static Sprite[] SelectFrames(Sprite[] sprites, int[] range)
        {
            if (sprites == null || range == null || range.Length < 2) return sprites ?? Array.Empty<Sprite>();
            int first = range[0];
            int last = range[1];
            if (first < 0 || last < first || first >= sprites.Length) return Array.Empty<Sprite>();
            last = Mathf.Min(last, sprites.Length - 1);
            return sprites.Skip(first).Take(last - first + 1).ToArray();
        }

        static Sprite[] LoadSourceSprites(string source)
        {
            if (AssetDatabase.IsValidFolder(source))
            {
                string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { source });
                return guids.Select(AssetDatabase.GUIDToAssetPath)
                    .SelectMany(path => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                    .OrderBy(sprite => sprite.name, NaturalStringComparer.Instance)
                    .ToArray();
            }
            return AssetDatabase.LoadAllAssetsAtPath(source).OfType<Sprite>()
                .OrderBy(sprite => sprite.name, NaturalStringComparer.Instance).ToArray();
        }

        static void EnsureAssetFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) || folder == "Assets" || AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureAssetFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
