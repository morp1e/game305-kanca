using System;
using Game.Core;
using Game.Combat;
using Game.Dialogue;
using Game.Feedback;
using Game.Hook;
using Game.Input;
using Game.Level;
using Game.Player;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Sandbox
{
    /// <summary>Greybox oynanış prefab'larını ve başlangıç olay asset'lerini üretir.</summary>
    public static class PrefabFactory
    {
        const string Root = "Assets/_Project";
        const string PrefabDir = Root + "/Prefabs";
        const string DataDir = Root + "/Data";
        const string PixelDir = Root + "/Art/Pixel/_Greybox";

        [MenuItem("Tools/Kanca/Prefabları Üret")]
        public static void BuildMenu()
        {
            var movement = AssetDatabase.LoadAssetAtPath<PlayerMovementTuning>(DataDir + "/Tuning/PlayerMovementTuning.asset");
            var hook = AssetDatabase.LoadAssetAtPath<GrappleHookTuning>(DataDir + "/Tuning/GrappleHookTuning.asset");
            var armTuning = Tuning(DataDir + "/Tuning/PlayerRigTuning.asset");
            Build(movement, hook, armTuning);
        }

        public static void Build(PlayerMovementTuning movement, GrappleHookTuning tuning, PlayerRigTuning armTuning)
        {
            EnsureFolder(PrefabDir); EnsureFolder(DataDir + "/Events"); EnsureFolder(DataDir + "/Dialogue");
            EnsureFolder(DataDir + "/Tuning");
            var square = AssetDatabase.LoadAssetAtPath<Sprite>(PixelDir + "/greybox_square.png");
            var circle = AssetDatabase.LoadAssetAtPath<Sprite>(PixelDir + "/greybox_circle.png");
            var death = Event<GameEvent>(DataDir + "/Events/Event_PlayerDeath.asset");
            var respawn = Event<GameEvent>(DataDir + "/Events/Event_PlayerRespawn.asset");
            var kick = Event<Vector2Event>(DataDir + "/Events/Event_CameraKick.asset");
            var lines = Event<SwordLineSet>(DataDir + "/Dialogue/SwordLines.asset");
            var trauma = AssetDatabase.LoadAssetAtPath<FloatEvent>(DataDir + "/Events/Event_CameraTrauma.asset");
            var swordTuning = Event<SwordTuning>(DataDir + "/Tuning/SwordTuning.asset");
            var dummyTuning = Event<TrainingDummyTuning>(DataDir + "/Tuning/TrainingDummyTuning.asset");
            lines.EnsureCombatCues(); EditorUtility.SetDirty(lines);
            var rig = new GameObject("PlayerRig");
            if (armTuning == null) armTuning = Tuning(DataDir + "/Tuning/PlayerRigTuning.asset");
            var player = CreatePlayer(square, movement, tuning, armTuning, trauma);
            player.transform.SetParent(rig.transform, false);
            player.transform.localPosition = Vector3.zero;
            var hookVisual = CreateHookVisual(square); hookVisual.transform.SetParent(rig.transform, false);
            var reticle = CreateReticle(circle); reticle.transform.SetParent(rig.transform, false);
            var hook = player.GetComponent<GrappleHook>();
            Set(hook, ("hookVisual", hookVisual.transform), ("reticle", reticle.transform));
            Set(hook, ("aimCamera", null));
            var respawnController = player.AddComponent<RespawnController>();
            Set(respawnController, ("motor", player.GetComponent<PlayerMotor>()), ("deathEvent", death), ("respawnEvent", respawn));
            SetOptional(respawnController, "visual", player.transform.Find("Visual").gameObject);
            var feedback = player.AddComponent<HookFeedback>();
            Set(feedback, ("hook", hook), ("hookVisual", hookVisual.transform), ("kickEvent", kick));
            var voice = player.AddComponent<SwordVoice>();
            var voiceEvents = player.AddComponent<SwordVoiceEvents>();
            Set(voice, ("lines", lines), ("hook", hook), ("swordEvents", voiceEvents), ("hookVisual", hookVisual.transform), ("deathEvent", death), ("respawnEvent", respawn));
            var attack = player.AddComponent<SwordAttack>();
            Set(attack, ("tuning", swordTuning), ("input", player.GetComponent<PlayerInputReader>()), ("armRig", player.GetComponent<PlayerArmRig>()), ("body", player.GetComponent<PlayerMotor>()), ("traumaEvent", trauma), ("voiceEvents", voiceEvents));
            SavePrefab(player, PrefabDir + "/Player.prefab");
            SavePrefab(rig, PrefabDir + "/PlayerRig.prefab");
            UnityEngine.Object.DestroyImmediate(rig);

            CreateHazardPrefab(square);
            CreateCheckpointPrefab(square);
            CreateTargetPrefab(square, "TrainingDummy", true, false, dummyTuning);
            CreateTargetPrefab(square, "HeavyDummy", true, true, dummyTuning);
            CreateTargetPrefab(square, "Crate", false, false, dummyTuning);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static GameObject CreatePlayer(Sprite sprite, PlayerMovementTuning movement, GrappleHookTuning tuning, PlayerRigTuning armTuning, FloatEvent trauma)
        {
            var player = new GameObject("Player"); player.layer = LayerMask.NameToLayer("Player");
            var rb = player.AddComponent<Rigidbody2D>(); rb.bodyType = RigidbodyType2D.Dynamic; rb.gravityScale = 0f; rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            var capsule = player.AddComponent<CapsuleCollider2D>(); capsule.direction = CapsuleDirection2D.Vertical; capsule.size = new Vector2(.8f, 1.8f);
            var input = player.AddComponent<PlayerInputReader>(); var motor = player.AddComponent<PlayerMotor>(); var hook = player.AddComponent<GrappleHook>();
            var visualRoot = new GameObject("Visual"); visualRoot.transform.SetParent(player.transform, false);
            var visual = new GameObject("Body"); visual.transform.SetParent(visualRoot.transform, false); visual.transform.localScale = new Vector3(1, 2, 1);
            visual.AddComponent<SpriteRenderer>().sprite = sprite;
            var hookArm = CreateArm("HookArm", sprite, new Color(.25f, .85f, .95f), armTuning.armLength);
            hookArm.transform.SetParent(visualRoot.transform, false);
            var swordArm = CreateArm("SwordArm", sprite, new Color(1f, .72f, .22f), armTuning.armLength);
            swordArm.transform.SetParent(visualRoot.transform, false);
            var armRig = player.AddComponent<PlayerArmRig>();
            Set(armRig, ("tuning", armTuning), ("bodySprite", visual.transform), ("hookArmPivot", hookArm.transform), ("swordArmPivot", swordArm.transform), ("input", input));
            Set(motor, ("tuning", movement), ("input", input), ("armRig", armRig));
            Set(hook, ("tuning", tuning), ("input", input), ("body", motor), ("armRig", armRig), ("traumaEvent", trauma));
            return player;
        }

        static GameObject CreateArm(string name, Sprite sprite, Color color, float length)
        {
            var pivot = new GameObject(name);
            var arm = new GameObject("Sprite"); arm.transform.SetParent(pivot.transform, false); arm.transform.localPosition = new Vector3(length * .5f, 0f, 0f); arm.transform.localScale = new Vector3(length, .2f, 1f);
            var renderer = arm.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = color;
            var hand = new GameObject("HandTip"); hand.transform.SetParent(pivot.transform, false); hand.transform.localPosition = new Vector3(length, 0f, 0f);
            return pivot;
        }

        static GameObject CreateHookVisual(Sprite sprite)
        {
            var hook = new GameObject("GrappleHook");
            var visual = new GameObject("Visual"); visual.transform.SetParent(hook.transform, false); visual.transform.localScale = new Vector3(1.2f, .25f, 1);
            visual.AddComponent<SpriteRenderer>().sprite = sprite;
            return hook;
        }

        static GameObject CreateReticle(Sprite sprite)
        {
            var reticle = new GameObject("Reticle"); var renderer = reticle.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.sortingLayerName = "UI_World"; reticle.transform.localScale = Vector3.one * .5f; return reticle;
        }

        static void CreateHazardPrefab(Sprite sprite)
        {
            var go = new GameObject("Hazard_Spikes"); go.layer = LayerMask.NameToLayer("Hazard");
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = new Color(.72f, .34f, .38f);
            var collider = go.AddComponent<BoxCollider2D>(); collider.isTrigger = true; collider.size = new Vector2(1, .5f);
            go.AddComponent<Hazard>(); SavePrefab(go, PrefabDir + "/Hazard_Spikes.prefab"); UnityEngine.Object.DestroyImmediate(go);
        }

        static void CreateCheckpointPrefab(Sprite sprite)
        {
            var go = new GameObject("Checkpoint");
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.color = new Color(.55f, .8f, .72f);
            var collider = go.AddComponent<BoxCollider2D>(); collider.isTrigger = true; collider.size = new Vector2(1, 3);
            go.AddComponent<Checkpoint>(); SavePrefab(go, PrefabDir + "/Checkpoint.prefab"); UnityEngine.Object.DestroyImmediate(go);
        }

        static void CreateTargetPrefab(Sprite sprite, string name, bool damageable, bool heavy, TrainingDummyTuning dummyTuning)
        {
            var go = new GameObject(name) { layer = damageable ? LayerMask.NameToLayer("Enemy") : 0 };
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite;
            renderer.color = name == "Crate" ? new Color(.58f, .42f, .28f) : heavy ? new Color(.45f, .55f, .62f) : new Color(.75f, .48f, .42f);
            go.transform.localScale = name == "Crate" ? new Vector3(1.1f, 1.1f, 1f) : heavy ? new Vector3(1.5f, 2f, 1f) : new Vector3(1f, 1.6f, 1f);
            var rb = go.AddComponent<Rigidbody2D>(); rb.gravityScale = 0f; rb.bodyType = heavy ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
            if (heavy) rb.mass = 100f;
            go.AddComponent<BoxCollider2D>();
            var pullable = go.AddComponent<HookPullable>();
            var serialized = new SerializedObject(pullable);
            serialized.FindProperty("weightClass").enumValueIndex = heavy ? 1 : 0;
            serialized.FindProperty("body").objectReferenceValue = rb;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            if (damageable)
            {
                var dummy = go.AddComponent<TrainingDummy>();
                var dummySerialized = new SerializedObject(dummy); dummySerialized.FindProperty("tuning").objectReferenceValue = dummyTuning; dummySerialized.ApplyModifiedPropertiesWithoutUndo();
            }
            SavePrefab(go, PrefabDir + "/" + name + ".prefab"); UnityEngine.Object.DestroyImmediate(go);
        }

        static T Event<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }

        static PlayerRigTuning Tuning(string path)
        {
            var asset = AssetDatabase.LoadAssetAtPath<PlayerRigTuning>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<PlayerRigTuning>(); AssetDatabase.CreateAsset(asset, path); return asset;
        }

        static void SavePrefab(GameObject go, string path)
        {
            PrefabUtility.SaveAsPrefabAsset(go, path);
        }

        static void Set(UnityEngine.Object target, params (string name, UnityEngine.Object value)[] values)
        {
            var serialized = new SerializedObject(target);
            foreach (var pair in values)
            {
                var property = serialized.FindProperty(pair.name);
                if (property == null) { Debug.LogWarning($"[PrefabFactory] {target.GetType().Name}.{pair.name} alanı bulunamadı."); continue; }
                property.objectReferenceValue = pair.value;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetOptional(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(name);
            if (property != null) { property.objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        }

        static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Substring(7).Split('/')) { string next = current + "/" + part; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part); current = next; }
        }
    }
}
