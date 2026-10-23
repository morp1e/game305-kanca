using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Game.CameraRig;
using Game.Core;
using Game.DebugTools;
using Game.Hook;
using Game.Input;
using Game.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Game.Level;
using Game.Dialogue;

namespace Game.Editor.Sandbox
{
    /// <summary>Gri kutu kanca parkurunu ve deneme sahnesini üretir.</summary>
    public static class SandboxBuilder
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox/Sandbox_Hook.unity";
        const string PixelDir = "Assets/_Project/Art/Pixel/_Greybox";
        const string PaintedDir = "Assets/_Project/Art/Painted/_Greybox";
        const string TuningDir = "Assets/_Project/Data/Tuning";
        const string EventDir = "Assets/_Project/Data/Events";
        const string PrefabDir = "Assets/_Project/Prefabs";
        const string TileDir = "Assets/_Project/Art/Pixel/_Greybox/Tiles";

        struct Block
        {
            public string name;
            public Rect rect;
            public Block(string name, float x, float y, float w, float h) { this.name = name; rect = new Rect(x, y, w, h); }
        }

        static readonly Block[] Course =
        {
            new Block("Ground_Start", 0, 0, 18, 2), new Block("Wall_Start", 0, 2, 1, 7),
            new Block("Platform_Low_01", 8, 4, 5, 1), new Block("Platform_High_01", 16, 8, 5, 1),
            new Block("Gap_HookRequired_Left", 18, 0, 2, 2), new Block("Gap_HookRequired_Right", 30, 0, 2, 2),
            new Block("Platform_Chain_01", 24, 5, 7, 1), new Block("Platform_Chain_02", 33, 9, 5, 1),
            new Block("Ceiling_CornerTest", 37, 13, 6, 1), new Block("Shaft_Left", 42, 0, 1, 18),
            new Block("Shaft_Right", 49, 0, 1, 18), new Block("Shaft_Floor", 42, 0, 8, 1),
            new Block("Platform_Shaft_01", 44, 5, 4, 1), new Block("Platform_Shaft_02", 44, 11, 4, 1),
            new Block("LOS_Wall", 54, 0, 1, 8), new Block("Platform_LOS", 52, 8, 8, 1),
            new Block("Platform_End_01", 62, 4, 6, 1), new Block("Ground_End", 68, 0, 12, 2),
            new Block("Wall_End", 79, 2, 1, 10)
        };

        [MenuItem("Tools/Kanca/Gri Kutu Sahnesini Kur")]
        public static void Build()
        {
            // Yeni sahne açılışı mevcut sahneyi kapatır; kaydedilmemiş iş varsa önce sor.
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            // Sahne önce açılır: NewScene kullanılmayan asset'leri boşaltır, ondan önce yüklenen
            // tuning/olay referansları boşa düşer ve sahneye null olarak yazılır.
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EnsureFolder(PixelDir); EnsureFolder(PaintedDir); EnsureFolder(TuningDir); EnsureFolder(EventDir);
            CreateGreyboxTextures();
            // Pixel preset'i pivotu alt ortaya koyar (gerçek karakter çizimi için doğru). Gri kutular ise collider'larla
            // ortalanmış genel şekiller; alt orta pivotla görsel collider'dan yarım boy yukarıda kalıyordu.
            EnsureCenterPivot(PixelDir + "/greybox_square.png");
            EnsureCenterPivot(PixelDir + "/greybox_circle.png");
            CreatePaintedTextures();
            var square = LoadSprite(PixelDir + "/greybox_square.png");
            var backgrounds = new[] { LoadSprite(PaintedDir + "/bg_test_far.png"), LoadSprite(PaintedDir + "/bg_test_mid.png"), LoadSprite(PaintedDir + "/bg_test_near.png") };
            var movement = SharedSandboxSetup.GetMovementTuning();
            var hook = SharedSandboxSetup.GetHookTuning();
            var armTuning = SharedSandboxSetup.GetArmTuning();
            var cameraTuning = SharedSandboxSetup.GetCameraTuning();
            var trauma = SharedSandboxSetup.GetTraumaEvent();

            AssetDatabase.SaveAssets();
            PrefabFactory.Build(movement, hook, armTuning);
            var playerRigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/PlayerRig.prefab");
            var playerRig = (GameObject)PrefabUtility.InstantiatePrefab(playerRigPrefab);
            playerRig.transform.position = new Vector3(3, 3, 0);
            var player = playerRig.transform.Find("Player").gameObject;
            CreateCourse(square);
            CreatePrefabInstances();
            CreateNonHookableObstacle(square);
            CreateBackgrounds(backgrounds);
            var camera = SharedSandboxSetup.CreateCamera(player.transform, cameraTuning, trauma);
            // Kamera oyuncudan sonra kuruluyor; nişan kamerası burada bağlanır.
            SharedSandboxSetup.Set(player.GetComponent<GrappleHook>(), ("aimCamera", camera));
            SharedSandboxSetup.Set(player.GetComponent<Game.Combat.SwordAttack>(), ("aimCamera", camera));
            SharedSandboxSetup.Set(player.GetComponent<PlayerArmRig>(), ("aimCamera", camera));
            SharedSandboxSetup.CreateHud(player.GetComponent<PlayerMotor>(), player.GetComponent<GrappleHook>(), player.GetComponent<RespawnController>(), player.GetComponent<SwordVoice>());
            SharedSandboxSetup.CreateGlobalLight();
            EnsureBuildScene();
            EditorSceneManager.SaveScene(scene, ScenePath);
            Selection.activeGameObject = player;
            Debug.Log("[SandboxBuilder] Sandbox_Hook hazır");
        }

        static void CreateGreyboxTextures()
        {
            WriteTexture(PixelDir + "/greybox_square.png", 16, 16, (x, y) => Color.white);
            WriteTexture(PixelDir + "/greybox_circle.png", 16, 16, (x, y) =>
            {
                float dx = x - 7.5f, dy = y - 7.5f;
                return dx * dx + dy * dy <= 56 ? Color.white : Color.clear;
            });
        }

        static void CreatePaintedTextures()
        {
            Color[] baseColors = { new Color(.12f, .2f, .36f), new Color(.19f, .3f, .42f), new Color(.33f, .37f, .4f) };
            for (int layer = 0; layer < baseColors.Length; layer++)
            {
                int current = layer;
                WriteTexture(PaintedDir + "/bg_test_" + new[] { "far", "mid", "near" }[layer] + ".png", 1024, 512, (x, y) =>
                {
                    float t = y / 511f;
                    Color c = Color.Lerp(baseColors[current] * .55f, baseColors[current] * 1.3f, t);
                    int band = (x / (180 + current * 47) + current * 3) % 7;
                    if (band == 1 && y > 70 + current * 35 && y < 210 + current * 28) c = Color.Lerp(c, baseColors[current] * 1.55f, .32f);
                    return c;
                });
            }
        }

        static void WriteTexture(string path, int width, int height, Func<int, int, Color> pixel)
        {
            if (File.Exists(path)) return;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var colors = new Color[width * height];
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++) colors[y * width + x] = pixel(x, y);
            texture.SetPixels(colors); texture.Apply();
            File.WriteAllBytes(path, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            ConfigureTexture(path, path.StartsWith(PixelDir, StringComparison.Ordinal) ? 16 : 64,
                path.StartsWith(PixelDir, StringComparison.Ordinal) ? FilterMode.Point : FilterMode.Bilinear);
        }

        static void ConfigureTexture(string path, int ppu, FilterMode filter)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) { Debug.LogWarning("[SandboxBuilder] TextureImporter bulunamadı: " + path); return; }
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = ppu; importer.filterMode = filter; importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp; importer.alphaIsTransparency = true;
            if (filter == FilterMode.Point) importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        static void EnsureCenterPivot(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            if (settings.spriteAlignment == (int)SpriteAlignment.Center) return;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            ConfigureTexture(path, path.StartsWith(PixelDir, StringComparison.Ordinal) ? 16 : 64,
                path.StartsWith(PixelDir, StringComparison.Ordinal) ? FilterMode.Point : FilterMode.Bilinear);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void CreateCourse(Sprite unused)
        {
            EnsureFolder(TileDir);
            // Pixel klasör preset'i pivotu alt ortaya koyar (karakterin ayağı için). Karo hücreyi tam doldurmalı,
            // bu yüzden karo için ortadan pivotlu ayrı bir doku kullanılır; çarpışma da sprite'tan değil ızgaradan gelir.
            string tilePath = TileDir + "/greybox_tile.png";
            WriteTexture(tilePath, 16, 16, (x, y) => Color.white);
            EnsureCenterPivot(tilePath);
            var tileSprite = AssetDatabase.LoadAssetAtPath<Sprite>(tilePath);

            var tile = AssetDatabase.LoadAssetAtPath<Tile>(TileDir + "/tile_ground.asset");
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, TileDir + "/tile_ground.asset");
            }
            tile.sprite = tileSprite; tile.color = new Color(.48f, .5f, .53f); tile.colliderType = Tile.ColliderType.Grid;
            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssets();
            var grid = new GameObject("Grid"); grid.AddComponent<Grid>();
            var ground = new GameObject("Ground"); ground.layer = LayerMask.NameToLayer("Ground"); ground.transform.SetParent(grid.transform, false);
            var map = ground.AddComponent<Tilemap>(); ground.AddComponent<TilemapRenderer>();
            // Karo başına kutu. CompositeCollider2D (Merge) Unity 6.3'te bu kurulumda hiç şekil üretmedi (TileProbe, 2026-10-18);
            // birleştirme gerekirse önce PlayMode testiyle doğrulanmalı.
            ground.AddComponent<TilemapCollider2D>();
            foreach (var block in Course)
            {
                int x0 = Mathf.RoundToInt(block.rect.xMin), y0 = Mathf.RoundToInt(block.rect.yMin);
                int width = Mathf.RoundToInt(block.rect.width), height = Mathf.RoundToInt(block.rect.height);
                for (int x = 0; x < width; x++) for (int y = 0; y < height; y++) map.SetTile(new Vector3Int(x0 + x, y0 + y, 0), tile);
            }
        }

        static void CreatePrefabInstances()
        {
            SharedSandboxSetup.InstantiatePrefab("Hazard_Spikes", new Vector3(24, 2.25f, 0), new Vector3(5, 1, 1));
            SharedSandboxSetup.InstantiatePrefab("Hazard_Spikes", new Vector3(16, 9, 0), new Vector3(3, 1, 1));
            SharedSandboxSetup.InstantiatePrefab("Checkpoint", new Vector3(4, 2.5f, 0));
            SharedSandboxSetup.InstantiatePrefab("Checkpoint", new Vector3(50, 2.5f, 0));
            SharedSandboxSetup.InstantiatePrefab("TrainingDummy", new Vector3(7, 2f, 0));
            SharedSandboxSetup.InstantiatePrefab("HeavyDummy", new Vector3(12, 2f, 0));
            SharedSandboxSetup.InstantiatePrefab("Crate", new Vector3(17, 1f, 0));
        }

        static void CreateNonHookableObstacle(Sprite sprite)
        {
            var obstacle = new GameObject("NonHookable_Obstacle") { layer = LayerMask.NameToLayer("Ground") };
            obstacle.transform.position = new Vector3(57f, 4f, 0f);
            obstacle.transform.localScale = new Vector3(1.5f, 2f, 1f);
            obstacle.AddComponent<SpriteRenderer>().sprite = sprite;
            obstacle.AddComponent<BoxCollider2D>();
            obstacle.AddComponent<NonHookable>();
        }

        static void CreateBackgrounds(Sprite[] sprites)
        {
            string[] names = { "BG_Far", "BG_Mid", "BG_Near" }; float[] factors = { .9f, .6f, .3f };
            for (int i = 0; i < sprites.Length; i++)
            {
                var go = new GameObject("Background_" + names[i]); var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprites[i]; renderer.sortingLayerName = names[i];
                go.transform.position = new Vector3(40, 8, 10 + i); go.transform.localScale = new Vector3(5.2f, 4.5f, 1);
            var parallax = go.AddComponent<ParallaxLayer>(); SharedSandboxSetup.Set(parallax, ("factor", new Vector2(factors[i], factors[i])));
            }
        }

        static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Substring("Assets/".Length).Split('/'))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        static void EnsureBuildScene()
        {
            SharedSandboxSetup.EnsureBuildScene(ScenePath);
        }
    }
}
