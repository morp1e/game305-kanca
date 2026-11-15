using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace Game.Editor.Import
{
    public sealed class PixelSpriteImporter : AssetPostprocessor
    {
        const string ThirdPartyRoot = "Assets/ThirdParty/GothicVania/";
        const string ProjectPixelRoot = "Assets/_Project/Art/Pixel/";

        public static class SpriteSheetGrid
        {
            public static int CalculateColumns(int textureWidth, int frameWidth, int columns)
            {
                return columns > 0 ? columns : Mathf.Max(1, frameWidth > 0 ? textureWidth / frameWidth : 1);
            }

            public static bool TryGetFrameRect(int textureWidth, int textureHeight, int frameWidth, int frameHeight,
                int columns, int frameIndex, out Rect rect)
            {
                rect = default;
                if (textureWidth <= 0 || textureHeight <= 0 || frameWidth <= 0 || frameHeight <= 0 || frameIndex < 0)
                    return false;

                int safeColumns = CalculateColumns(textureWidth, frameWidth, columns);
                int column = frameIndex % safeColumns;
                int row = frameIndex / safeColumns;
                int x = column * frameWidth;
                int y = textureHeight - (row + 1) * frameHeight;
                if (x < 0 || y < 0 || x + frameWidth > textureWidth || y + frameHeight > textureHeight)
                    return false;

                rect = new Rect(x, y, frameWidth, frameHeight);
                return true;
            }
        }

        static bool IsPropTiled(string path) => path.StartsWith(ProjectPixelRoot, StringComparison.OrdinalIgnoreCase)
            && Path.GetFileNameWithoutExtension(path).StartsWith("prop_", StringComparison.OrdinalIgnoreCase);

        static bool IsPixelPath(string path) => path.StartsWith(ThirdPartyRoot, StringComparison.OrdinalIgnoreCase)
            || path.StartsWith(ProjectPixelRoot, StringComparison.OrdinalIgnoreCase);

        public static int RequiredMaxTextureSize(int width, int height)
        {
            int largestEdge = Mathf.Max(1, Mathf.Max(width, height));
            return Mathf.Min(8192, Mathf.NextPowerOfTwo(largestEdge));
        }

        void OnPreprocessTexture()
        {
            if (!IsPixelPath(assetPath) || !assetPath.EndsWith(".png", StringComparison.OrdinalIgnoreCase)) return;
            TextureImporter importer = (TextureImporter)assetImporter;
            importer.GetSourceTextureWidthAndHeight(out int sourceWidth, out int sourceHeight);
            int maxTextureSize = RequiredMaxTextureSize(sourceWidth, sourceHeight);
            if (importer.maxTextureSize != maxTextureSize) importer.maxTextureSize = maxTextureSize;
            importer.textureType = TextureImporterType.Sprite;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            SetPixelSettings(importer, SpriteMeshType.FullRect, SpriteAlignment.Custom, new Vector2(0.5f, 0f));
            importer.wrapMode = IsPropTiled(assetPath) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;

            string sidecarPath = SpriteSheetSidecar.ResolvePath(assetPath);
            if (File.Exists(sidecarPath) && SpriteSheetSidecar.TryParse(File.ReadAllText(sidecarPath), out SpriteSheetSidecar sidecar))
            {
                ApplySheet(importer, sidecar);
                return;
            }

            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = DefaultPixelsPerUnit(assetPath);
            importer.spritePivot = new Vector2(0.5f, 0f);
        }

        static void ApplySheet(TextureImporter importer, SpriteSheetSidecar sidecar)
        {
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = sidecar.ppu;
            float pivotX = sidecar.pivot[0], pivotY = sidecar.pivot[1];
            importer.GetSourceTextureWidthAndHeight(out int textureWidth, out int textureHeight);
            int columns = SpriteSheetGrid.CalculateColumns(textureWidth, sidecar.frameWidth, sidecar.columns);
            string baseName = Path.GetFileNameWithoutExtension(importer.assetPath);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existingIds = new Dictionary<string, GUID>();
            foreach (SpriteRect existing in provider.GetSpriteRects()) existingIds[existing.name] = existing.spriteID;
            var spriteRects = new List<SpriteRect>(sidecar.frames);
            for (int i = 0; i < sidecar.frames; i++)
            {
                if (!SpriteSheetGrid.TryGetFrameRect(textureWidth, textureHeight, sidecar.frameWidth,
                    sidecar.frameHeight, columns, i, out Rect rect))
                {
                    Debug.LogWarning($"Sprite sheet '{Path.GetFileName(importer.assetPath)}': kare {i} grid dışına taştı; " +
                        $"ızgara {columns} sütun, {Mathf.CeilToInt(sidecar.frames / (float)columns)} satır, " +
                        $"doku {textureWidth}x{textureHeight}, kare {sidecar.frameWidth}x{sidecar.frameHeight}. Kare atlandı.");
                    continue;
                }
                string spriteName = $"{baseName}_{i}";
                Vector2 pivot = sidecar.PivotForFrame(i, pivotX, pivotY);
                spriteRects.Add(new SpriteRect
                {
                    name = spriteName,
                    rect = rect,
                    pivot = pivot,
                    alignment = SpriteAlignment.Custom,
                    spriteID = existingIds.TryGetValue(spriteName, out GUID spriteId) ? spriteId : GUID.Generate()
                });
            }
            provider.SetSpriteRects(spriteRects.ToArray());
            provider.Apply();
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            foreach (string path in importedAssets)
            {
                if (!IsPixelPath(path) || !path.EndsWith(".aseprite", StringComparison.OrdinalIgnoreCase)) continue;
                AssetImporter importer = AssetImporter.GetAtPath(path);
                if (importer == null || importer.GetType().FullName != "UnityEditor.U2D.Aseprite.AsepriteImporter") continue;
                bool tiledProp = IsPropTiled(path);
                TextureWrapMode wrap = tiledProp ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
                bool pivotChanged = HasBottomCenterPivotChanges(importer);
                float defaultPpu = DefaultPixelsPerUnit(path);
                bool changed = (float)GetProperty(importer, "spritePixelsPerUnit") != defaultPpu
                    || (FilterMode)GetProperty(importer, "filterMode") != FilterMode.Point
                    || (SpriteMeshType)GetProperty(importer, "spriteMeshType") != SpriteMeshType.FullRect
                    || (bool)GetProperty(importer, "mipmapEnabled")
                    || (TextureWrapMode)GetProperty(importer, "wrapModeU") != wrap
                    || (TextureWrapMode)GetProperty(importer, "wrapModeV") != wrap
                    || !HasUncompressedPlatformSettings(importer)
                    || pivotChanged;
                if (!changed) continue;
                SetProperty(importer, "spritePixelsPerUnit", defaultPpu);
                SetProperty(importer, "filterMode", FilterMode.Point);
                SetProperty(importer, "spriteMeshType", SpriteMeshType.FullRect);
                SetProperty(importer, "mipmapEnabled", false);
                SetProperty(importer, "wrapModeU", wrap);
                SetProperty(importer, "wrapModeV", wrap);
                ConfigureBottomCenterPivots(importer);
                SetUncompressedPlatformSettings(importer);
                importer.SaveAndReimport();
            }
        }

        static object GetProperty(AssetImporter importer, string name) => importer.GetType().GetProperty(name).GetValue(importer);
        static void SetProperty(AssetImporter importer, string name, object value) => importer.GetType().GetProperty(name).SetValue(importer, value);

        static float DefaultPixelsPerUnit(string path) =>
            path.StartsWith(ThirdPartyRoot, StringComparison.OrdinalIgnoreCase) ? 24f : 16f;

        static void SetPixelSettings(TextureImporter importer, SpriteMeshType meshType, SpriteAlignment alignment, Vector2 pivot)
        {
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteMeshType == meshType && settings.spriteAlignment == (int)alignment && settings.spritePivot == pivot) return;
            settings.spriteMeshType = meshType;
            settings.spriteAlignment = (int)alignment;
            settings.spritePivot = pivot;
            importer.SetTextureSettings(settings);
        }

        static bool HasBottomCenterPivotChanges(AssetImporter importer)
        {
            ISpriteEditorDataProvider provider = importer as ISpriteEditorDataProvider;
            if (provider == null) return false;
            provider.InitSpriteEditorDataProvider();
            foreach (SpriteRect rect in provider.GetSpriteRects())
                if (rect.alignment != SpriteAlignment.Custom || rect.pivot != new Vector2(0.5f, 0f)) return true;
            return false;
        }

        static void ConfigureBottomCenterPivots(AssetImporter importer)
        {
            ISpriteEditorDataProvider provider = importer as ISpriteEditorDataProvider;
            if (provider == null) return;
            provider.InitSpriteEditorDataProvider();
            SpriteRect[] rects = provider.GetSpriteRects();
            for (int i = 0; i < rects.Length; i++)
            {
                rects[i].alignment = SpriteAlignment.Custom;
                rects[i].pivot = new Vector2(0.5f, 0f);
            }
            provider.SetSpriteRects(rects);
            provider.Apply();
        }

        static bool HasUncompressedPlatformSettings(AssetImporter importer)
        {
            object defaults = GetDefaultPlatformSettings(importer);
            PropertyInfo defaultCompression = defaults?.GetType().GetProperty("textureCompression");
            if (defaultCompression == null || (TextureImporterCompression)defaultCompression.GetValue(defaults) != TextureImporterCompression.Uncompressed)
                return false;
            var serialized = new SerializedObject(importer);
            SerializedProperty platforms = serialized.FindProperty("m_PlatformSettings");
            if (platforms == null) return false;
            for (int i = 0; i < platforms.arraySize; i++)
            {
                SerializedProperty compression = platforms.GetArrayElementAtIndex(i).FindPropertyRelative("textureCompression");
                if (compression != null && compression.intValue != (int)TextureImporterCompression.Uncompressed) return false;
            }
            return true;
        }

        static void SetUncompressedPlatformSettings(AssetImporter importer)
        {
            var serialized = new SerializedObject(importer);
            SerializedProperty platforms = serialized.FindProperty("m_PlatformSettings");
            if (platforms != null)
            {
                for (int i = 0; i < platforms.arraySize; i++)
                {
                    SerializedProperty compression = platforms.GetArrayElementAtIndex(i).FindPropertyRelative("textureCompression");
                    if (compression != null) compression.intValue = (int)TextureImporterCompression.Uncompressed;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            object defaultSettings = GetDefaultPlatformSettings(importer);
            if (defaultSettings == null) return;
            PropertyInfo compressionProperty = defaultSettings.GetType().GetProperty("textureCompression");
            if (compressionProperty == null) return;
            compressionProperty.SetValue(defaultSettings, TextureImporterCompression.Uncompressed);
            importer.GetType().GetMethod("SetImporterPlatformSettings", new[] { defaultSettings.GetType() })
                ?.Invoke(importer, new[] { defaultSettings });
        }

        static object GetDefaultPlatformSettings(AssetImporter importer) =>
            importer.GetType().GetMethod("GetImporterPlatformSettings", new[] { typeof(BuildTarget) })
                ?.Invoke(importer, new object[] { BuildTarget.NoTarget });
    }
}
