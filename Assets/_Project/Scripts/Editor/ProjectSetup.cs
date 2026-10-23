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
            SetupLayers();
            EnsureTmpEssentials();

            // Kılıç ve oyuncu cast'leri kendi collider'larının içinden başlar; kendine çarpmasın.
            Physics2D.queriesStartInColliders = false;

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

        // Fizik katmanları (indeks sabit, kod LayerMask'larını isimle alır) ve çizim sırası.
        static readonly (int index, string name)[] Layers =
        {
            (6, "Ground"), (7, "Player"), (9, "Hazard"), (10, "Enemy"), (11, "Sword"),
        };

        // Default oynanış katmanıdır; arka plan katmanları önüne, ön plan ve UI arkasına girer.
        static readonly string[] SortingBefore = { "BG_Far", "BG_Mid", "BG_Near" };
        static readonly string[] SortingAfter = { "FG", "UI_World" };

        static void SetupLayers()
        {
            var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);

            var layers = tagManager.FindProperty("layers");
            foreach (var (index, name) in Layers)
            {
                var slot = layers.GetArrayElementAtIndex(index);
                if (string.IsNullOrEmpty(slot.stringValue) || slot.stringValue == name)
                    slot.stringValue = name;
                else
                    Debug.LogWarning($"[ProjectSetup] Layer {index} dolu ('{slot.stringValue}'), '{name}' yazılmadı.");
            }

            var sorting = tagManager.FindProperty("m_SortingLayers");
            bool Exists(string n)
            {
                for (int i = 0; i < sorting.arraySize; i++)
                    if (sorting.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == n) return true;
                return false;
            }
            int DefaultIndex()
            {
                for (int i = 0; i < sorting.arraySize; i++)
                    if (sorting.GetArrayElementAtIndex(i).FindPropertyRelative("uniqueID").longValue == 0) return i;
                return 0;
            }
            void Insert(int at, string n)
            {
                sorting.InsertArrayElementAtIndex(at);
                var e = sorting.GetArrayElementAtIndex(at);
                e.FindPropertyRelative("name").stringValue = n;
                e.FindPropertyRelative("uniqueID").longValue = (uint)n.GetHashCode() | 1u;
                e.FindPropertyRelative("locked").boolValue = false;
            }
            foreach (var n in SortingBefore)
                if (!Exists(n)) Insert(DefaultIndex(), n);
            foreach (var n in SortingAfter)
                if (!Exists(n)) Insert(sorting.arraySize, n);

            tagManager.ApplyModifiedProperties();
        }

        // Kılıcın konuşma balonu TextMeshPro kullanır; temel font ve ayar dosyaları projede olmalı.
        static void EnsureTmpEssentials()
        {
            if (AssetDatabase.LoadAssetAtPath<Object>("Assets/TextMesh Pro/Resources/TMP Settings.asset") != null) return;
            var package = Directory.GetFiles(Path.GetFullPath("Library/PackageCache"), "TMP Essential Resources.unitypackage", SearchOption.AllDirectories)
                .FirstOrDefault();
            if (package == null) { Debug.LogWarning("[ProjectSetup] TMP Essential Resources paketi bulunamadı."); return; }
            AssetDatabase.ImportPackage(package, false);
        }

        static void SetFolderDefaults(Preset pixel, Preset painted)
        {
            var type = pixel.GetPresetType();
            string pixelFilter = $"glob:\"{PixelDir}/**\"";
            string paintedFilter = $"glob:\"{PaintedDir}/**\"";
            // Tekrar çalıştırmada preset yeniden yaratılır; eski kayıt silinmiş preset'i gösterir,
            // o yüzden kendi filtrelerimizi ve boşa düşmüş kayıtları atıp baştan ekliyoruz.
            var others = Preset.GetDefaultPresetsForType(type)
                .Where(d => d.preset != null && d.filter != pixelFilter && d.filter != paintedFilter);
            var defaults = others
                .Append(new DefaultPreset(pixelFilter, pixel))
                .Append(new DefaultPreset(paintedFilter, painted))
                .ToArray();
            Preset.SetDefaultPresetsForType(type, defaults);
        }
    }
}
