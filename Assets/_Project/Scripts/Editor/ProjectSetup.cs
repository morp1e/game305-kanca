using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Presets;
using UnityEngine;

namespace Game.Editor
{
    /// <summary>
    /// Proje ayarlarını docs/teknik-mimari.md'ye göre kurar. Tekrar çalıştırmak güvenlidir.
    /// Menü: Tools > Kanca > Projeyi Kur. Komut satırı: -executeMethod Game.Editor.ProjectSetup.Run
    /// </summary>
    public static class ProjectSetup
    {
        const string Root = "Assets/_Project";
        const string PresetDir = Root + "/Settings/Presets";
        const string PixelDir = Root + "/Art/Pixel";
        const string PaintedDir = Root + "/Art/Painted";

        [MenuItem("Tools/Kanca/Projeyi Kur")]
        public static void Run()
        {
            PlayerSettings.companyName = "Morp1e";
            PlayerSettings.productName = "Kanca";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;

            Directory.CreateDirectory(PresetDir);
            var pixel = CreateTexturePreset("Pixel", ConfigurePixel);
            var painted = CreateTexturePreset("Painted", ConfigurePainted);
            SetFolderDefaults(pixel, painted);

            EditorBuildSettings.scenes = AssetDatabase.FindAssets("t:Scene", new[] { Root + "/Scenes" })
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(p => p.Contains("/Sandbox/") ? 1 : 0)
                .ThenBy(p => p)
                .Select(p => new EditorBuildSettingsScene(p, true))
                .ToArray();

            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup] Tamamlandı.");
        }

        static void ConfigurePixel(TextureImporter ti)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 16;
            ti.filterMode = FilterMode.Point;
            ti.textureCompression = TextureImporterCompression.Uncompressed;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.alphaIsTransparency = true;
            var s = new TextureImporterSettings();
            ti.ReadTextureSettings(s);
            s.spriteMeshType = SpriteMeshType.FullRect;
            s.spriteAlignment = (int)SpriteAlignment.BottomCenter;
            ti.SetTextureSettings(s);
        }

        static void ConfigurePainted(TextureImporter ti)
        {
            ti.textureType = TextureImporterType.Sprite;
            ti.spriteImportMode = SpriteImportMode.Single;
            ti.spritePixelsPerUnit = 64;
            ti.filterMode = FilterMode.Bilinear;
            ti.textureCompression = TextureImporterCompression.CompressedHQ;
            ti.mipmapEnabled = false;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.maxTextureSize = 4096;
            ti.alphaIsTransparency = true;
        }

        // Preset bir importer örneğinden üretilir; bunun için geçici bir doku içe aktarılır.
        static Preset CreateTexturePreset(string name, System.Action<TextureImporter> configure)
        {
            string tmp = $"{PresetDir}/_tmp_{name}.png";
            var tex = new Texture2D(4, 4);
            File.WriteAllBytes(tmp, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(tmp, ImportAssetOptions.ForceSynchronousImport);

            var importer = (TextureImporter)AssetImporter.GetAtPath(tmp);
            configure(importer);
            var preset = new Preset(importer);

            string path = $"{PresetDir}/Texture_{name}.preset";
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.CreateAsset(preset, path);
            AssetDatabase.DeleteAsset(tmp);
            return AssetDatabase.LoadAssetAtPath<Preset>(path);
        }

        static void SetFolderDefaults(Preset pixel, Preset painted)
        {
            var type = pixel.GetPresetType();
            var others = Preset.GetDefaultPresetsForType(type)
                .Where(d => d.preset != pixel && d.preset != painted);
            var defaults = others
                .Append(new DefaultPreset($"glob:\"{PixelDir}/**\"", pixel))
                .Append(new DefaultPreset($"glob:\"{PaintedDir}/**\"", painted))
                .ToArray();
            Preset.SetDefaultPresetsForType(type, defaults);
        }
    }
}
