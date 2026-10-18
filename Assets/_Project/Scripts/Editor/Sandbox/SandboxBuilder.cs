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
            EnsureFolder(PixelDir); EnsureFolder(PaintedDir); EnsureFolder(TuningDir); EnsureFolder(EventDir);
            CreateGreyboxTextures();
            CreatePaintedTextures();
            var square = LoadSprite(PixelDir + "/greybox_square.png");
            var circle = LoadSprite(PixelDir + "/greybox_circle.png");
            var backgrounds = new[] { LoadSprite(PaintedDir + "/bg_test_far.png"), LoadSprite(PaintedDir + "/bg_test_mid.png"), LoadSprite(PaintedDir + "/bg_test_near.png") };
            var movement = GetOrCreate<PlayerMovementTuning>(TuningDir + "/PlayerMovementTuning.asset");
            var hook = GetOrCreate<SwordHookTuning>(TuningDir + "/SwordHookTuning.asset");
            var cameraTuning = GetOrCreate<CameraTuning>(TuningDir + "/CameraTuning.asset");
            var trauma = GetOrCreate<FloatEvent>(EventDir + "/Event_CameraTrauma.asset");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var player = CreatePlayer(square, movement, hook, trauma);
            CreateCourse(square);
            CreateAnchors(circle);
            CreateBackgrounds(backgrounds);
            var camera = CreateCamera(player.transform, cameraTuning, trauma);
            // Kamera oyuncudan sonra kuruluyor; nişan kamerası burada bağlanır.
            Set(player.GetComponent<SwordHook>(), ("aimCamera", camera));
            var respawn = player.AddComponent<RespawnBelow>();
            Set(respawn, ("motor", player.GetComponent<PlayerMotor>()));
            CreateHud(player.GetComponent<PlayerMotor>(), player.GetComponent<SwordHook>());
            CreateGlobalLight();
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

        static Sprite LoadSprite(string path)
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null) return sprite;
            ConfigureTexture(path, path.StartsWith(PixelDir, StringComparison.Ordinal) ? 16 : 64,
                path.StartsWith(PixelDir, StringComparison.Ordinal) ? FilterMode.Point : FilterMode.Bilinear);
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }

        static void CreateCourse(Sprite sprite)
        {
            foreach (var block in Course)
            {
                var go = new GameObject(block.name); go.layer = LayerMask.NameToLayer("Ground");
                go.transform.position = new Vector3(block.rect.center.x, block.rect.center.y, 0);
                go.transform.localScale = new Vector3(block.rect.width, block.rect.height, 1);
                var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = new Color(.48f, .5f, .53f);
                var collider = go.AddComponent<BoxCollider2D>(); collider.size = Vector2.one;
            }
        }

        static GameObject CreatePlayer(Sprite sprite, PlayerMovementTuning movement, SwordHookTuning tuning, FloatEvent trauma)
        {
            var player = new GameObject("Player"); player.layer = LayerMask.NameToLayer("Player"); player.transform.position = new Vector3(3, 3, 0);
            var rb = player.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Kinematic; rb.useFullKinematicContacts = true; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var capsule = player.AddComponent<CapsuleCollider2D>(); capsule.direction = CapsuleDirection2D.Vertical; capsule.size = new Vector2(.8f, 1.8f);
            var input = player.AddComponent<PlayerInputReader>(); var motor = player.AddComponent<PlayerMotor>(); var swordHook = player.AddComponent<SwordHook>();
            var visual = new GameObject("Visual"); visual.transform.SetParent(player.transform, false); visual.transform.localScale = new Vector3(1, 2, 1);
            visual.AddComponent<SpriteRenderer>().sprite = sprite; visual.GetComponent<SpriteRenderer>().color = new Color(.2f, .85f, .48f);
            visual.AddComponent<PixelSnap>(); Set(motor, ("tuning", movement), ("input", input));
            var sword = new GameObject("Sword"); sword.layer = LayerMask.NameToLayer("Sword");
            var swordVisual = new GameObject("Visual"); swordVisual.transform.SetParent(sword.transform, false); swordVisual.transform.localScale = new Vector3(1.2f, .25f, 1);
            swordVisual.AddComponent<SpriteRenderer>().sprite = sprite; swordVisual.GetComponent<SpriteRenderer>().color = new Color(.45f, .85f, 1);
            swordVisual.AddComponent<PixelSnap>();
            Set(swordHook, ("tuning", tuning), ("input", input), ("body", motor), ("sword", sword.transform), ("traumaEvent", trauma));
            var reticle = new GameObject("Reticle"); reticle.AddComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(PixelDir + "/greybox_circle.png");
            reticle.GetComponent<SpriteRenderer>().sortingLayerName = "UI_World"; reticle.transform.localScale = Vector3.one * .5f;
            Set(swordHook, ("reticle", reticle.transform));
            return player;
        }

        static void CreateAnchors(Sprite sprite)
        {
            Vector2[] points = { new Vector2(10, 8), new Vector2(20, 10), new Vector2(27, 13), new Vector2(35, 16), new Vector2(46, 16), new Vector2(45, 7), new Vector2(57, 12), new Vector2(65, 9), new Vector2(73, 14) };
            foreach (var point in points)
            {
                var go = new GameObject("Anchor_" + point.x.ToString("00") + "_" + point.y.ToString("00")); go.layer = LayerMask.NameToLayer("Anchor"); go.transform.position = point;
                go.AddComponent<SpriteRenderer>().sprite = sprite;
                var collider = go.AddComponent<CircleCollider2D>(); collider.isTrigger = true; collider.radius = .5f;
                go.AddComponent<HookAnchor>();
            }
        }

        static void CreateBackgrounds(Sprite[] sprites)
        {
            string[] names = { "BG_Far", "BG_Mid", "BG_Near" }; float[] factors = { .9f, .6f, .3f };
            for (int i = 0; i < sprites.Length; i++)
            {
                var go = new GameObject("Background_" + names[i]); var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprites[i]; renderer.sortingLayerName = names[i];
                go.transform.position = new Vector3(40, 8, 10 + i); go.transform.localScale = new Vector3(5.2f, 4.5f, 1);
                var parallax = go.AddComponent<ParallaxLayer>(); Set(parallax, ("factor", new Vector2(factors[i], factors[i])));
            }
        }

        static Camera CreateCamera(Transform player, CameraTuning tuning, FloatEvent trauma)
        {
            var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 8.4375f; camera.backgroundColor = new Color(.035f, .04f, .08f); camera.clearFlags = CameraClearFlags.SolidColor;
            // Seçenek (a): upscale render texture kapalı, boyalı katman bulanıklaşmasın (docs/teknik-mimari.md bölüm 6).
            var pixelPerfect = cameraObject.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = 16; pixelPerfect.refResolutionX = 480; pixelPerfect.refResolutionY = 270;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.None; pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.None;
            cameraObject.AddComponent<CinemachineBrain>();
            var cmObject = new GameObject("CM_Follow"); var cm = cmObject.AddComponent<CinemachineCamera>(); cm.Follow = player; cm.Lens.OrthographicSize = 8.4375f;
            var composer = cmObject.AddComponent<CinemachinePositionComposer>(); composer.Damping = new Vector3(.25f, .25f, 0);
            composer.Composition.DeadZone = new ScreenComposerSettings.DeadZoneSettings { Enabled = true, Size = new Vector2(.08f, .08f) };
            cmObject.AddComponent<CinemachinePixelPerfect>(); var shake = cmObject.AddComponent<CameraShake>(); Set(shake, ("tuning", tuning), ("traumaEvent", trauma));
            return camera;
        }

        static void CreateHud(PlayerMotor motor, SwordHook hook)
        {
            var go = new GameObject("DebugHud"); var hud = go.AddComponent<DebugHud>(); Set(hud, ("motor", motor), ("swordHook", hook));
        }

        static void CreateGlobalLight()
        {
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
        }

        static void Set(UnityEngine.Object target, params (string name, object value)[] values)
        {
            var serialized = new SerializedObject(target);
            foreach (var item in values)
            {
                var property = serialized.FindProperty(item.name);
                if (property == null) { Debug.LogWarning($"[SandboxBuilder] {target.GetType().Name}.{item.name} alanı bulunamadı."); continue; }
                if (item.value is UnityEngine.Object obj) property.objectReferenceValue = obj;
                else if (item.value is Vector2 vector) property.vector2Value = vector;
                else if (item.value is int integer) property.intValue = integer;
                else if (item.value is float number) property.floatValue = number;
                else if (item.value is bool boolean) property.boolValue = boolean;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
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
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == ScenePath)) scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
