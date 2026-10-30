using Game.Combat;
using Game.Core;
using Game.Player;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Sandbox
{
    public static class PlayerHealthSetup
    {
        const string Root = "Assets/_Project";
        const string PrefabPath = Root + "/Prefabs/Player.prefab";
        const string RigPrefabPath = Root + "/Prefabs/PlayerRig.prefab";
        const string TuningPath = Root + "/Data/Tuning/PlayerHealthTuning.asset";
        const string HealthEventPath = Root + "/Data/Events/Event_PlayerHealth.asset";
        const string DamagedEventPath = Root + "/Data/Events/Event_PlayerDamaged.asset";

        [MenuItem("Tools/Kanca/Oyuncu Canini Kur")]
        public static void Apply()
        {
            EnsureFolder(Root + "/Data/Tuning");
            EnsureFolder(Root + "/Data/Events");
            var tuning = GetOrCreate<PlayerHealthTuning>(TuningPath);
            var healthEvent = GetOrCreate<HealthEvent>(HealthEventPath);
            var damagedEvent = GetOrCreate<GameEvent>(DamagedEventPath);
            var traumaEvent = AssetDatabase.LoadAssetAtPath<FloatEvent>(Root + "/Data/Events/Event_CameraTrauma.asset");
            // PlayerRig içindeki oyuncu Player.prefab'ın bağlı örneği değil, ayrı bir kopya (PrefabFactory iki prefab'ı da
            // SaveAsPrefabAsset ile kaydediyor). Sahneler PlayerRig'i kullandığı için ikisine de ayrı ayrı eklenir.
            ApplyTo(PrefabPath, root => root, tuning, healthEvent, damagedEvent, traumaEvent);
            ApplyTo(RigPrefabPath, root => root.transform.Find("Player")?.gameObject, tuning, healthEvent, damagedEvent, traumaEvent);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Oyuncu can bileşeni ve olay bağlantıları kuruldu (Player ve PlayerRig).");
        }

        static void ApplyTo(string path, System.Func<GameObject, GameObject> findPlayer, PlayerHealthTuning tuning,
            HealthEvent healthEvent, GameEvent damagedEvent, FloatEvent traumaEvent)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null) return;
            var prefab = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var player = findPlayer(prefab);
                if (player == null)
                {
                    Debug.LogWarning("[PlayerHealthSetup] Oyuncu bulunamadı: " + path);
                    return;
                }
                var health = player.GetComponent<PlayerHealth>();
                if (health == null) health = player.AddComponent<PlayerHealth>();
                var serialized = new SerializedObject(health);
                AssignIfEmpty(serialized, "tuning", tuning);
                AssignIfEmpty(serialized, "healthEvent", healthEvent);
                AssignIfEmpty(serialized, "damagedEvent", damagedEvent);
                AssignIfEmpty(serialized, "traumaEvent", traumaEvent);
                var visual = player.transform.Find("Visual");
                AssignIfEmpty(serialized, "visual", visual != null ? visual.gameObject : null);
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(prefab, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(prefab); }
        }

        static void AssignIfEmpty(SerializedObject serialized, string name, Object value)
        {
            var property = serialized.FindProperty(name);
            if (property != null && property.objectReferenceValue == null && value != null)
                property.objectReferenceValue = value;
        }

        static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Substring(7).Split('/'))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }
    }
}
