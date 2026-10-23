using System.Linq;
using Game.CameraRig;
using Game.Core;
using Game.DebugTools;
using Game.Dialogue;
using Game.Hook;
using Game.Level;
using Game.Player;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Editor.Sandbox
{
    /// <summary>Sandbox sahnelerinin ortak prefab, tuning, kamera ve HUD kurulumunu yapar.</summary>
    internal static class SharedSandboxSetup
    {
        const string PrefabDir = "Assets/_Project/Prefabs";
        const string TuningDir = "Assets/_Project/Data/Tuning";
        const string EventDir = "Assets/_Project/Data/Events";

        internal static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        internal static PlayerMovementTuning GetMovementTuning() => GetOrCreate<PlayerMovementTuning>(TuningDir + "/PlayerMovementTuning.asset");
        internal static GrappleHookTuning GetHookTuning() => GetOrCreate<GrappleHookTuning>(TuningDir + "/GrappleHookTuning.asset");
        internal static PlayerRigTuning GetArmTuning() => GetOrCreate<PlayerRigTuning>(TuningDir + "/PlayerRigTuning.asset");
        internal static CameraTuning GetCameraTuning() => GetOrCreate<CameraTuning>(TuningDir + "/CameraTuning.asset");
        internal static FloatEvent GetTraumaEvent() => GetOrCreate<FloatEvent>(EventDir + "/Event_CameraTrauma.asset");

        internal static GameObject InstantiatePrefab(string name, Vector3 position, Vector3? scale = null)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabDir + "/" + name + ".prefab");
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.position = position;
            if (scale.HasValue) instance.transform.localScale = scale.Value;
            return instance;
        }

        internal static Camera CreateCamera(Transform player, CameraTuning tuning, FloatEvent trauma)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 8.4375f; camera.backgroundColor = new Color(.035f, .04f, .08f); camera.clearFlags = CameraClearFlags.SolidColor;
            var pixelPerfect = cameraObject.AddComponent<PixelPerfectCamera>();
            pixelPerfect.assetsPPU = 16; pixelPerfect.refResolutionX = 480; pixelPerfect.refResolutionY = 270;
            pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.None; pixelPerfect.cropFrame = PixelPerfectCamera.CropFrame.None;
            cameraObject.AddComponent<CinemachineBrain>();
            var cmObject = new GameObject("CM_Follow"); var cm = cmObject.AddComponent<CinemachineCamera>(); cm.Follow = player; cm.Lens.OrthographicSize = 8.4375f;
            var composer = cmObject.AddComponent<CinemachinePositionComposer>(); composer.Damping = new Vector3(.25f, .25f, 0);
            composer.Composition.DeadZone = new ScreenComposerSettings.DeadZoneSettings { Enabled = true, Size = new Vector2(.08f, .08f) };
            cmObject.AddComponent<CinemachinePixelPerfect>();
            var shake = cmObject.AddComponent<CameraShake>();
            Set(shake, ("tuning", tuning), ("traumaEvent", trauma), ("kickEvent", AssetDatabase.LoadAssetAtPath<Vector2Event>(EventDir + "/Event_CameraKick.asset")));
            return camera;
        }

        internal static void CreateHud(PlayerMotor motor, GrappleHook hook, RespawnController respawn, SwordVoice voice)
        {
            var hud = new GameObject("DebugHud").AddComponent<DebugHud>();
            Set(hud, ("motor", motor), ("grappleHook", hook), ("respawn", respawn), ("swordVoice", voice));
        }

        internal static void CreateGlobalLight()
        {
            var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Global;
            light.intensity = 1f;
        }

        internal static void Set(UnityEngine.Object target, params (string name, object value)[] values)
        {
            var serialized = new SerializedObject(target);
            foreach (var item in values)
            {
                var property = serialized.FindProperty(item.name);
                if (property == null) { Debug.LogWarning($"[SandboxSetup] {target.GetType().Name}.{item.name} alanı bulunamadı."); continue; }
                if (item.value is UnityEngine.Object obj) property.objectReferenceValue = obj;
                else if (item.value is Vector2 vector) property.vector2Value = vector;
                else if (item.value is int integer) property.intValue = integer;
                else if (item.value is float number) property.floatValue = number;
                else if (item.value is bool boolean) property.boolValue = boolean;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        internal static void EnsureBuildScene(string scenePath)
        {
            var scenes = EditorBuildSettings.scenes.ToList();
            if (!scenes.Any(s => s.path == scenePath)) scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
