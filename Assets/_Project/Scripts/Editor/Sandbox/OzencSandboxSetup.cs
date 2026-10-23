using System;
using System.Collections.Generic;
using System.IO;
using Game.Core;
using Game.Dialogue;
using Game.Hook;
using Game.Level;
using Game.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Game.Editor.Sandbox
{
    /// <summary>Kenney karo paletini ve Özenç'in kişisel elle boyanabilir sandbox sahnesini kurar.</summary>
    public static class OzencSandboxSetup
    {
        const string Root = "Assets/ThirdParty/Kenney_PixelPlatformer";
        const string TilesDir = Root + "/Tiles";
        const string PalettePath = Root + "/Palette_Kenney.prefab";
        const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_Ozenc.unity";
        const string PrefabDir = "Assets/_Project/Prefabs";
        const int TilePixels = 18;

        [MenuItem("Tools/Kanca/Ozenc Sandbox Kur")]
        public static void Setup()
        {
            EnsureSheetImported(Root + "/tilemap_packed.png", "tilemap_packed", 18, 20, 9);
            EnsureSheetImported(Root + "/tilemap-backgrounds_packed.png", "tilemap-backgrounds_packed", 18, 192 / 18, 72 / 18);
            EnsureSheetImported(Root + "/tilemap-characters_packed.png", "tilemap-characters_packed", 24, 9, 3);
            var opaque = GetOpaqueCells(Root + "/tilemap_packed.png", 18, 20, 9);
            EnsureTiles(opaque);
            var palette = EnsurePalette(opaque);
            if (File.Exists(ScenePath))
            {
                Debug.Log("[OzencSandboxSetup] Sandbox_Ozenc mevcut. Sahne korunarak kurulum durduruldu.");
                return;
            }

            CreateScene();
            SharedSandboxSetup.EnsureBuildScene(ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[OzencSandboxSetup] Sandbox_Ozenc oluşturuldu. Tile Palette: " + palette);
        }

        public static Rect[] GetGridRects(int width, int height, int cellSize)
        {
            int columns = width / cellSize;
            int rows = height / cellSize;
            var rects = new Rect[columns * rows];
            for (int visualRow = 0; visualRow < rows; visualRow++)
                for (int column = 0; column < columns; column++)
                    rects[visualRow * columns + column] = new Rect(column * cellSize, (rows - 1 - visualRow) * cellSize, cellSize, cellSize);
            return rects;
        }

        public static bool IsCellOpaque(Color32[] pixels, int textureWidth, Rect rect)
        {
            int x0 = (int)rect.x, y0 = (int)rect.y, width = (int)rect.width, height = (int)rect.height;
            for (int y = y0; y < y0 + height; y++)
                for (int x = x0; x < x0 + width; x++)
                    if (pixels[y * textureWidth + x].a != 0) return true;
            return false;
        }

        static void EnsureSheetImported(string path, string sheetName, int cell, int columns, int rows)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogError("[OzencSandboxSetup] Doku bulunamadı: " + path); return; }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = TilePixels;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.SaveAndReimport();

            var opaque = GetOpaqueCells(path, cell, columns, rows);
            var rects = GetGridRects(columns * cell, rows * cell, cell);
            var factories = new SpriteDataProviderFactories();
            factories.Init();
            var provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            var existingIds = new Dictionary<string, GUID>();
            foreach (var existing in provider.GetSpriteRects()) existingIds[existing.name] = existing.spriteID;
            var sprites = new List<SpriteRect>(opaque.Count);
            foreach (int index in opaque)
            {
                string spriteName = sheetName + "_" + index;
                sprites.Add(new SpriteRect
                {
                    name = spriteName,
                    rect = rects[index],
                    alignment = SpriteAlignment.Center,
                    pivot = new Vector2(.5f, .5f),
                    spriteID = existingIds.TryGetValue(spriteName, out var spriteId) ? spriteId : GUID.Generate()
                });
            }
            provider.SetSpriteRects(sprites.ToArray());
            provider.Apply();
            importer.SaveAndReimport();
        }

        static List<int> GetOpaqueCells(string path, int cell, int columns, int rows)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.LoadImage(File.ReadAllBytes(path));
            var pixels = texture.GetPixels32();
            var result = new List<int>();
            var rects = GetGridRects(texture.width, texture.height, cell);
            for (int i = 0; i < columns * rows; i++)
                if (IsCellOpaque(pixels, texture.width, rects[i])) result.Add(i);
            UnityEngine.Object.DestroyImmediate(texture);
            return result;
        }

        static void EnsureTiles(List<int> opaque)
        {
            EnsureFolder(TilesDir);
            var sprites = AssetDatabase.LoadAllAssetsAtPath(Root + "/tilemap_packed.png");
            foreach (int index in opaque)
            {
                var sprite = Array.Find(sprites, a => a is Sprite s && s.name == "tilemap_packed_" + index) as Sprite;
                if (sprite == null) continue;
                string path = TilesDir + "/tilemap_packed_" + index + ".asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile == null) { tile = ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile, path); }
                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.Grid;
                EditorUtility.SetDirty(tile);
            }
            AssetDatabase.SaveAssets();
        }

        static string EnsurePalette(List<int> opaque)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PalettePath) != null) return PalettePath;
            string paletteFolder = Root;
            var paletteObject = GridPaletteUtility.CreateNewPalette(paletteFolder, "Palette_Kenney", GridLayout.CellLayout.Rectangle, GridPalette.CellSizing.Automatic, Vector3.one, GridLayout.CellSwizzle.XYZ);
            var paletteMap = paletteObject.GetComponentInChildren<Tilemap>();
            if (paletteMap == null) paletteMap = new GameObject("Tilemap", typeof(Tilemap), typeof(TilemapRenderer)).GetComponent<Tilemap>();
            for (int index = 0; index < 180; index++)
            {
                if (!opaque.Contains(index)) continue;
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(TilesDir + "/tilemap_packed_" + index + ".asset");
                int x = index % 20, y = 8 - index / 20;
                paletteMap.SetTile(new Vector3Int(x, y, 0), tile);
            }
            PrefabUtility.SaveAsPrefabAsset(paletteObject, PalettePath);
            UnityEngine.Object.DestroyImmediate(paletteObject);
            return PalettePath;
        }

        static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var grid = new GameObject("Grid", typeof(Grid));
            var ground = CreateTilemap(grid.transform, "Ground", true, false);
            var nonHookable = CreateTilemap(grid.transform, "NonHookable", true, true);
            var decor = CreateTilemap(grid.transform, "Decor", false, false);
            var groundTiles = new[] { LoadTile(0), LoadTile(1), LoadTile(2) };
            var bodyTile = LoadTile(20) ?? groundTiles[0];
            for (int x = 0; x < 30; x++)
            {
                ground.SetTile(new Vector3Int(x, 0, 0), groundTiles[x % groundTiles.Length]);
                ground.SetTile(new Vector3Int(x, -1, 0), bodyTile);
            }
            for (int y = 1; y < 6; y++) ground.SetTile(new Vector3Int(12, y, 0), bodyTile);
            for (int x = 20; x < 27; x++) ground.SetTile(new Vector3Int(x, 6, 0), groundTiles[(x + 1) % groundTiles.Length]);
            nonHookable.SetTile(new Vector3Int(16, 0, 0), LoadTile(0));
            decor.SetTile(new Vector3Int(9, 1, 0), LoadTile(4));

            var movement = SharedSandboxSetup.GetMovementTuning();
            var hook = SharedSandboxSetup.GetHookTuning();
            var arms = SharedSandboxSetup.GetArmTuning();
            var cameraTuning = SharedSandboxSetup.GetCameraTuning();
            var trauma = SharedSandboxSetup.GetTraumaEvent();
            AssetDatabase.SaveAssets();
            PrefabFactory.Build(movement, hook, arms);
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/PlayerRig.prefab");
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            rig.transform.position = new Vector3(4, 2, 0);
            var player = rig.transform.Find("Player").gameObject;
            SharedSandboxSetup.InstantiatePrefab("Checkpoint", new Vector3(4, 1.5f, 0));
            SharedSandboxSetup.InstantiatePrefab("TrainingDummy", new Vector3(7, 2f, 0));
            SharedSandboxSetup.InstantiatePrefab("HeavyDummy", new Vector3(11, 2f, 0));
            SharedSandboxSetup.InstantiatePrefab("Crate", new Vector3(15, 1f, 0));
            var camera = SharedSandboxSetup.CreateCamera(player.transform, cameraTuning, trauma);
            SharedSandboxSetup.Set(player.GetComponent<GrappleHook>(), ("aimCamera", camera));
            SharedSandboxSetup.Set(player.GetComponent<Game.Combat.SwordAttack>(), ("aimCamera", camera));
            SharedSandboxSetup.Set(player.GetComponent<PlayerArmRig>(), ("aimCamera", camera));
            SharedSandboxSetup.CreateHud(player.GetComponent<PlayerMotor>(), player.GetComponent<GrappleHook>(), player.GetComponent<RespawnController>(), player.GetComponent<SwordVoice>());
            SharedSandboxSetup.CreateGlobalLight();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = ground.gameObject;
        }

        static Tilemap CreateTilemap(Transform parent, string name, bool collider, bool noHook)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.layer = LayerMask.NameToLayer("Ground");
            go.transform.SetParent(parent, false);
            var map = go.GetComponent<Tilemap>();
            var renderer = go.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = name == "Decor" ? -2 : name == "Ground" ? -1 : 0;
            if (collider)
            {
                // Karo başına kutu, SandboxBuilder ile aynı. CompositeCollider2D (Merge) Unity 6.3'te burada da
                // sıfır şekil üretti (Sandbox_Ozenc, 2026-10-23); oyuncu zeminden düşüyordu.
                go.AddComponent<TilemapCollider2D>();
            }
            if (noHook) go.AddComponent<NonHookable>();
            return map;
        }

        static Tile LoadTile(int index) => AssetDatabase.LoadAssetAtPath<Tile>(TilesDir + "/tilemap_packed_" + index + ".asset");

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            string name = Path.GetFileName(path);
            if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
