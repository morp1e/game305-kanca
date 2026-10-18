using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    // Klasör preset'lerinin gerçekten uygulandığını, yeni içe aktarılan bir dokuda kontrol eder.
    public class ImportPresetTests
    {
        static TextureImporter ImportTemp(string folder)
        {
            string path = $"{folder}/_test_import.png";
            var tex = new Texture2D(8, 8);
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return (TextureImporter)AssetImporter.GetAtPath(path);
        }

        [TearDown]
        public void Cleanup()
        {
            AssetDatabase.DeleteAsset("Assets/_Project/Art/Pixel/_test_import.png");
            AssetDatabase.DeleteAsset("Assets/_Project/Art/Painted/_test_import.png");
        }

        [Test]
        public void PixelFolder_GetsPixelSettings()
        {
            var ti = ImportTemp("Assets/_Project/Art/Pixel");
            Assert.AreEqual(TextureImporterType.Sprite, ti.textureType);
            Assert.AreEqual(16f, ti.spritePixelsPerUnit);
            Assert.AreEqual(FilterMode.Point, ti.filterMode);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, ti.textureCompression);
        }

        [Test]
        public void PaintedFolder_GetsPaintedSettings()
        {
            var ti = ImportTemp("Assets/_Project/Art/Painted");
            Assert.AreEqual(TextureImporterType.Sprite, ti.textureType);
            Assert.AreEqual(64f, ti.spritePixelsPerUnit);
            Assert.AreEqual(FilterMode.Bilinear, ti.filterMode);
            Assert.AreEqual(4096, ti.maxTextureSize);
        }
    }
}
