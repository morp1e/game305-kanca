using System;
using System.Collections.Generic;
using System.IO;
using Game.CameraRig;
using Game.Combat;
using Game.Core;
using Game.DebugTools;
using Game.Dialogue;
using Game.Hook;
using Game.Level;
using Game.Player;
using Game.Editor.Sandbox;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Game.Editor.Level
{
    public static class LevelBuilder
    {
        const string LegendPath = "Assets/_Project/Data/Levels/LevelLegend.asset";
        const string Prefabs = "Assets/_Project/Prefabs/";
        const string Tiles = "Assets/ThirdParty/Kenney_PixelPlatformer/Tiles/";
        static readonly Dictionary<char, int> SurfaceTiles = new Dictionary<char, int> { ['#'] = 1, ['X'] = 1, ['~'] = 4 };

        [MenuItem("Tools/Kanca/Bolumu Haritadan Kur")]
        public static void BuildSelected()
        {
            var asset = Selection.activeObject as TextAsset;
            string path = asset == null ? "Assets/_Project/Data/Levels/Level_01.txt" : AssetDatabase.GetAssetPath(asset);
            BuildFromMap(path, "Assets/_Project/Scenes/Levels/" + Path.GetFileNameWithoutExtension(path) + ".unity", true);
        }

        public static void BuildLevel01() => BuildFromMap("Assets/_Project/Data/Levels/Level_01.txt", "Assets/_Project/Scenes/Levels/Level_01.unity", true);

        public static void BuildFromMap(string mapAssetPath, string scenePath, bool overwrite)
        {
            var text = AssetDatabase.LoadAssetAtPath<TextAsset>(mapAssetPath);
            if (text == null) throw new FileNotFoundException("Harita bulunamadı: " + mapAssetPath);
            var legend = GetLegend();
            var map = LevelMapParser.Parse(text.text, GetKnown(legend));
            foreach (var warning in map.Warnings) Debug.LogWarning(warning);
            if (File.Exists(scenePath) && !overwrite) throw new IOException("Sahne zaten var: " + scenePath);
            EnsureFolder(Path.GetDirectoryName(scenePath).Replace('\\', '/'));
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var grid = new GameObject("Grid", typeof(Grid));
            var ground = CreateTilemap(grid.transform, "Ground", true, false);
            var noHook = CreateTilemap(grid.transform, "NonHookable", true, true);
            var decor = CreateTilemap(grid.transform, "Decor", false, false);
            Tile[] top = { LoadTile(0), LoadTile(1), LoadTile(2) };
            var body = LoadTile(20) ?? top[0];
            var entities = new GameObject("Entities");
            Vector3 playerPos = Vector3.zero;
            for (int y = 0; y < map.Height; y++)
            for (int x = 0; x < map.Width; x++)
            {
                char c = LevelMapParser.GetCell(map, x, y);
                var entry = legend.Find(c);
                if (entry == null) continue;
                var cell = new Vector3Int(x, y, 0);
                if (entry.kind == LevelLegendKind.TileGround || entry.kind == LevelLegendKind.TileNonHookable || entry.kind == LevelLegendKind.TileDecor)
                {
                    var target = entry.kind == LevelLegendKind.TileGround ? ground : entry.kind == LevelLegendKind.TileNonHookable ? noHook : decor;
                    Tile tile = c == '#' && LevelMapParser.IsTopTile(map, x, y) ? top[(x + y) % top.Length] : c == '#' ? body : LoadTile(SurfaceTiles.TryGetValue(c, out int idx) ? idx : 20);
                    if (tile != null) target.SetTile(cell, tile);
                    continue;
                }
                Vector3 pos = new Vector3(x + .5f, y, 0) + (Vector3)entry.offset;
                if (c == 'S') { playerPos = pos; continue; }
                if (entry.kind == LevelLegendKind.HazardZone) { BuildHazardZone(entities.transform, pos); continue; }
                if (entry.kind == LevelLegendKind.Exit) { BuildExit(entities.transform, pos, entry); continue; }
                SpawnPrefab(entities.transform, c, entry, pos);
            }
            var rigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + "PlayerRig.prefab");
            if (rigPrefab == null) throw new FileNotFoundException("PlayerRig prefab bulunamadı.");
            var rig = (GameObject)PrefabUtility.InstantiatePrefab(rigPrefab);
            rig.transform.SetParent(entities.transform);
            rig.transform.position = playerPos;
            var player = rig.transform.Find("Player").gameObject;
            var respawn = player.GetComponent<RespawnController>();
            SharedSandboxSetup.Set(respawn, ("killHeight", float.IsNaN(map.KillHeight) ? -6f : map.KillHeight));
            var camera = SharedSandboxSetup.CreateCamera(player.transform, SharedSandboxSetup.GetCameraTuning(), SharedSandboxSetup.GetTraumaEvent());
            SharedSandboxSetup.Set(player.GetComponent<GrappleHook>(), ("aimCamera", camera));
            SharedSandboxSetup.Set(player.GetComponent<SwordAttack>(), ("aimCamera", camera));
            SharedSandboxSetup.Set(player.GetComponent<PlayerArmRig>(), ("aimCamera", camera));
            SharedSandboxSetup.CreateHud(player.GetComponent<PlayerMotor>(), player.GetComponent<GrappleHook>(), respawn, player.GetComponent<SwordVoice>());
            SharedSandboxSetup.CreateGlobalLight();
            CreateBounds(map, camera, map.CameraPadding);
            SpawnOptional("UI/GameHud", entities.transform, new Vector3(0,0,0));
            SpawnOptional("UI/EventSystem", entities.transform, Vector3.zero);
            EditorSceneManager.SaveScene(scene, scenePath);
            SharedSandboxSetup.EnsureBuildScene(scenePath);
            AssetDatabase.SaveAssets();
            OrderBuildScenes(scenePath);
            Debug.Log($"Bölüm kuruldu: {scenePath} ({map.Width}x{map.Height}).");
        }

        static IEnumerable<char> GetKnown(LevelLegend legend) { foreach (var entry in legend.entries) if (!string.IsNullOrEmpty(entry.character)) yield return entry.character[0]; }
        static LevelLegend GetLegend()
        {
            var asset = AssetDatabase.LoadAssetAtPath<LevelLegend>(LegendPath);
            if (asset != null) return asset;
            EnsureFolder("Assets/_Project/Data/Levels");
            asset = ScriptableObject.CreateInstance<LevelLegend>();
            void Add(char c, LevelLegendKind kind, string path = null) => asset.entries.Add(new LevelLegendEntry { character = c.ToString(), kind = kind, prefabPath = path });
            Add('#', LevelLegendKind.TileGround); Add('X', LevelLegendKind.TileNonHookable); Add('~', LevelLegendKind.TileDecor);
            Add('^', LevelLegendKind.Prefab, "Hazard_Spikes"); Add('S', LevelLegendKind.Prefab, "PlayerRig"); Add('C', LevelLegendKind.Prefab, "Checkpoint");
            Add('z', LevelLegendKind.HazardZone); Add('w', LevelLegendKind.Prefab, "Enemies/Walker"); Add('f', LevelLegendKind.Prefab, "Enemies/Flyer"); Add('t', LevelLegendKind.Prefab, "Enemies/Turret"); Add('b', LevelLegendKind.Prefab, "Enemies/Brute");
            Add('k', LevelLegendKind.Prefab, "Crate"); Add('d', LevelLegendKind.Prefab, "TrainingDummy"); Add('B', LevelLegendKind.Prefab, "Boss/BossArena"); Add('E', LevelLegendKind.Exit, "Level/LevelExit");
            AssetDatabase.CreateAsset(asset, LegendPath); return asset;
        }
        static Tilemap CreateTilemap(Transform parent, string name, bool collider, bool nonHookable)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer)); go.layer = LayerMask.NameToLayer("Ground"); go.transform.SetParent(parent, false);
            go.GetComponent<TilemapRenderer>().sortingOrder = name == "Decor" ? -2 : name == "Ground" ? -1 : 0;
            if (collider) { go.AddComponent<TilemapCollider2D>(); go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static; }
            if (nonHookable) go.AddComponent<NonHookable>(); return go.GetComponent<Tilemap>();
        }
        static Tile LoadTile(int i) => AssetDatabase.LoadAssetAtPath<Tile>(Tiles + "tilemap_packed_" + i + ".asset");
        static void SpawnPrefab(Transform root, char c, LevelLegendEntry entry, Vector3 pos)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefabs + entry.prefabPath + ".prefab");
            if (prefab == null) { Debug.LogWarning($"'{c}' için prefab eksik: {entry.prefabPath}"); var missing = new GameObject("Missing_" + entry.prefabPath); missing.transform.SetParent(root); missing.transform.position = pos; return; }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab); instance.transform.SetParent(root); instance.transform.position = pos;
        }
        static void SpawnOptional(string path, Transform root, Vector3 pos) { var e = new LevelLegendEntry { prefabPath = path }; SpawnPrefab(root, '?', e, pos); }
        static void BuildHazardZone(Transform root, Vector3 pos)
        {
            var go = new GameObject("HazardRespawnZone"); go.transform.SetParent(root); go.transform.position = pos;
            var box = go.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = new Vector2(3, 4);
            var type = Type.GetType("Game.Level.HazardRespawnZone, Game.Runtime");
            if (type == null) { Debug.LogWarning("HazardRespawnZone henüz bulunamadı; yer tutucu oluşturuldu."); return; }
            var zone = go.AddComponent(type); var child = new GameObject("RespawnPoint"); child.transform.SetParent(go.transform); child.transform.localPosition = Vector3.zero;
            var so = new SerializedObject(zone); var p = so.FindProperty("respawnPoint"); if (p != null) p.objectReferenceValue = child.transform; else { p = so.FindProperty("spawnOffset"); if (p != null) p.vector2Value = Vector2.zero; } so.ApplyModifiedPropertiesWithoutUndo();
        }
        static void BuildExit(Transform root, Vector3 pos, LevelLegendEntry entry) { SpawnPrefab(root, 'E', entry, pos); }
        static void CreateBounds(LevelMapData map, Camera camera, float padding)
        {
            var go = new GameObject("LevelBounds", typeof(LevelBounds)); go.layer = LayerMask.NameToLayer("Ignore Raycast"); var polygon = go.GetComponent<PolygonCollider2D>(); polygon.isTrigger = true;
            polygon.points = new[] { new Vector2(-padding,-padding), new Vector2(map.Width+padding,-padding), new Vector2(map.Width+padding,map.Height+padding), new Vector2(-padding,map.Height+padding) };
            var cm = GameObject.Find("CM_Follow"); if (cm != null) { var confiner = cm.AddComponent<CinemachineConfiner2D>(); confiner.BoundingShape2D = polygon; }
        }
        static void OrderBuildScenes(string levelPath)
        {
            var current = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            var ordered = new List<EditorBuildSettingsScene>();
            AddByName("Title"); Add(levelPath); AddByName("End");
            foreach (var scene in current) if (!ordered.Exists(item => item.path == scene.path)) ordered.Add(scene);
            EditorBuildSettings.scenes = ordered.ToArray();
            void AddByName(string name) { var match = current.Find(s => Path.GetFileNameWithoutExtension(s.path) == name); if (match != null) Add(match.path); }
            void Add(string path) { var match = current.Find(s => s.path == path); if (match != null && !ordered.Exists(s => s.path == path)) ordered.Add(match); }
        }
        static void EnsureFolder(string path) { if (AssetDatabase.IsValidFolder(path)) return; var parent=Path.GetDirectoryName(path).Replace('\\','/'); if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent); AssetDatabase.CreateFolder(parent,Path.GetFileName(path)); }
    }
}
