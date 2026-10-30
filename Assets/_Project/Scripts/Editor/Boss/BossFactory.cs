using System.IO;
using Game.Boss;
using Game.Combat;
using Game.Core;
using Game.Hook;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Boss
{
    /// <summary>Boss dövüşünün ayarlarını, olaylarını ve gri kutu prefab'larını üretir.</summary>
    public static class BossFactory
    {
        const string DataPath = "Assets/_Project/Data/Boss";
        const string EventPath = "Assets/_Project/Data/Events";
        const string PrefabPath = "Assets/_Project/Prefabs/Boss";
        const string GreySpritePath = "Assets/_Project/Art/Pixel/_Greybox/greybox_square.png";

        [MenuItem("Tools/Kanca/Boss'u Kur")]
        public static void BuildAll()
        {
            Directory.CreateDirectory(DataPath);
            Directory.CreateDirectory(EventPath);
            Directory.CreateDirectory(PrefabPath);
            AssetDatabase.Refresh();

            var started = GetOrCreate<StringEvent>(EventPath + "/Event_BossFightStarted.asset");
            var health = GetOrCreate<HealthEvent>(EventPath + "/Event_BossHealth.asset");
            var defeated = GetOrCreate<GameEvent>(EventPath + "/Event_BossDefeated.asset");
            var won = GetOrCreate<GameEvent>(EventPath + "/Event_GameWon.asset");
            var respawn = GetOrCreate<GameEvent>(EventPath + "/Event_PlayerRespawn.asset");
            var trauma = GetOrCreate<FloatEvent>(EventPath + "/Event_CameraTrauma.asset");

            BossAttack charge = Attack("BossAttack_ShieldCharge", "shield_charge", BossMoveType.Charge, 0.8f, 1f, 1f, true, 16f);
            BossAttack slam = Attack("BossAttack_GroundSlam", "ground_slam", BossMoveType.Slam, 0.7f, 0.15f, 0.6f, true, 0f);
            BossAttack bash = Attack("BossAttack_ShieldBash", "shield_bash", BossMoveType.Stationary, 0.45f, 0.15f, 0.35f, false, 0f);
            BossAttack whip = Attack("BossAttack_ChainWhip", "chain_whip", BossMoveType.Whip, 0.6f, 0.25f, 0.7f, false, 0f);
            BossAttack collapse = Attack("BossAttack_BeamCollapse", "beam_collapse", BossMoveType.BeamCollapse, 0.8f, 0.1f, 0.5f, false, 0f);
            string phaseOnePath = DataPath + "/BossPhase_1.asset";
            string phaseTwoPath = DataPath + "/BossPhase_2.asset";
            bool newPhaseOne = AssetDatabase.LoadAssetAtPath<BossPhase>(phaseOnePath) == null;
            bool newPhaseTwo = AssetDatabase.LoadAssetAtPath<BossPhase>(phaseTwoPath) == null;
            BossPhase phaseOne = GetOrCreate<BossPhase>(phaseOnePath);
            BossPhase phaseTwo = GetOrCreate<BossPhase>(phaseTwoPath);
            if (newPhaseOne)
                ConfigurePhase(phaseOne, 10f, 1f, 1f, 0.45f, 0.6f, 1f, true, new[]
                {
                    Entry(charge, 3f, 0f, 40f), Entry(slam, 2f, 0f, 40f), Entry(bash, 2f, 0f, 2.5f)
                });
            if (newPhaseTwo)
                ConfigurePhase(phaseTwo, 0f, 2f, 0.8f, 0.45f, 0.3f, 0.6f, false, new[]
                {
                    Entry(charge, 3f, 0f, 40f), Entry(slam, 2f, 0f, 40f), Entry(bash, 2f, 0f, 2.5f),
                    Entry(whip, 2f, 0f, 40f), Entry(collapse, 1f, 0f, 40f)
                });
            string definitionPath = DataPath + "/BossDefinition_ZincirliBekci.asset";
            bool newDefinition = AssetDatabase.LoadAssetAtPath<BossDefinition>(definitionPath) == null;
            var definition = GetOrCreate<BossDefinition>(definitionPath);
            if (newDefinition) ConfigureDefinition(definition, phaseOne, phaseTwo);

            GameObject shockwave = BuildProjectilePrefab("Shockwave", 0.8f, 0.9f, Color.white);
            GameObject debris = BuildProjectilePrefab("Debris", 0.45f, 0.6f, new Color(.45f, .3f, .2f));
            if (slam.spawnPrefab == null)
            {
                SetObject(slam, "spawnPrefab", shockwave);
                SetFloat(slam, "spawnSpeed", 10f);
            }
            if (collapse.spawnPrefab == null)
            {
                SetObject(collapse, "spawnPrefab", debris);
                SetFloat(collapse, "spawnSpeed", 3f);
            }

            GameObject bossPrefab = BuildBossPrefab(definition, health, trauma, defeated, won);
            BuildArenaPrefab(bossPrefab, definition, started, health, respawn, defeated);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        static T GetOrCreate<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        static BossAttack Attack(string name, string id, BossMoveType movement, float telegraph, float active, float recovery, bool loose, float speed)
        {
            string path = DataPath + "/" + name + ".asset";
            bool created = AssetDatabase.LoadAssetAtPath<BossAttack>(path) == null;
            BossAttack attack = GetOrCreate<BossAttack>(path);
            if (!created) return attack;
            SetString(attack, "id", id);
            SetInt(attack, "movement", (int)movement);
            SetFloat(attack, "telegraph", telegraph);
            SetFloat(attack, "active", active);
            SetFloat(attack, "recovery", recovery);
            SetBool(attack, "loosensShield", loose);
            SetFloat(attack, "moveSpeed", speed);
            if (attack.hitboxes == null || attack.hitboxes.Length == 0)
            {
                Vector2 hitboxSize = movement == BossMoveType.Whip ? new Vector2(28f, 1f) : new Vector2(2f, 2f);
                Vector2 hitboxOffset = movement == BossMoveType.Whip ? Vector2.zero : new Vector2(2f, 0f);
                attack.hitboxes = new[] { new BossHitbox { offset = hitboxOffset, size = hitboxSize, activeFrom = 0f, activeTo = active, damage = 1f } };
                EditorUtility.SetDirty(attack);
            }
            return attack;
        }

        static BossAttackEntry Entry(BossAttack attack, float weight, float min, float max)
        {
            return new BossAttackEntry { attack = attack, weight = weight, minRange = min, maxRange = max };
        }

        static void ConfigurePhase(BossPhase phase, float threshold, float duration, float scale, float minimum, float idleMin, float idleMax, bool shield, BossAttackEntry[] attacks)
        {
            SetFloat(phase, "enterAtHealth", threshold);
            SetFloat(phase, "transitionDuration", duration);
            SetFloat(phase, "telegraphScale", scale);
            SetFloat(phase, "minTelegraph", minimum);
            SetFloat(phase, "idleMin", idleMin);
            SetFloat(phase, "idleMax", idleMax);
            SetBool(phase, "hasShield", shield);
            var serialized = new SerializedObject(phase);
            SerializedProperty list = serialized.FindProperty("attacks");
            list.arraySize = attacks.Length;
            for (int i = 0; i < attacks.Length; i++)
            {
                SerializedProperty element = list.GetArrayElementAtIndex(i);
                element.FindPropertyRelative("attack").objectReferenceValue = attacks[i].attack;
                element.FindPropertyRelative("weight").floatValue = attacks[i].weight;
                element.FindPropertyRelative("minRange").floatValue = attacks[i].minRange;
                element.FindPropertyRelative("maxRange").floatValue = attacks[i].maxRange;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(phase);
        }

        static void ConfigureDefinition(BossDefinition definition, BossPhase first, BossPhase second)
        {
            SetString(definition, "displayName", "Zincirli Bekçi");
            SetFloat(definition, "maxHealth", 24f);
            SetFloat(definition, "introDuration", 1f);
            SetInt(definition, "rearComboHits", 6);
            SetFloat(definition, "comboWindow", 2f);
            SetFloat(definition, "comboStagger", 1f);
            SetFloat(definition, "shieldStagger", 1.6f);
            SetFloat(definition, "shieldRewind", 0.5f);
            SetFloat(definition, "contactDamage", 1f);
            SetFloat(definition, "victoryDelay", 2.5f);
            var serialized = new SerializedObject(definition);
            var phases = serialized.FindProperty("phases");
            phases.arraySize = 2;
            phases.GetArrayElementAtIndex(0).objectReferenceValue = first;
            phases.GetArrayElementAtIndex(1).objectReferenceValue = second;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(definition);
        }

        static GameObject BuildProjectilePrefab(string name, float width, float height, Color color)
        {
            string path = PrefabPath + "/" + name + ".prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = new GameObject(name) { layer = LayerMask.NameToLayer("Enemy") };
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(width, height);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GreySpritePath);
            renderer.color = color;
            root.AddComponent<BossProjectile>();
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static GameObject BuildBossPrefab(BossDefinition definition, HealthEvent health, FloatEvent trauma, GameEvent defeated, GameEvent won)
        {
            string path = PrefabPath + "/ChainedWarden.prefab";
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;
            var root = new GameObject("ChainedWarden") { layer = LayerMask.NameToLayer("Enemy") };
            var body = root.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            var collider = root.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(3f, 3f);
            var renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GreySpritePath);
            renderer.color = new Color(.22f, .24f, .28f);
            var contact = root.AddComponent<ContactDamage>();
            SetLayerMask(contact, "targetLayers", LayerMask.GetMask("Player"));
            var brain = root.AddComponent<BossBrain>();
            SetObject(brain, "definition", definition);
            SetObject(brain, "body", body);
            SetObject(brain, "bodyCollider", collider);
            SetObject(brain, "contactDamage", contact);
            SetObject(brain, "visual", renderer);
            SetObject(brain, "healthEvent", health);
            SetObject(brain, "traumaEvent", trauma);
            SetObject(brain, "defeatedEvent", defeated);
            SetObject(brain, "gameWonEvent", won);

            var shieldRoot = new GameObject("Shield") { layer = LayerMask.NameToLayer("Enemy") };
            shieldRoot.transform.SetParent(root.transform, false);
            shieldRoot.transform.localPosition = new Vector3(2f, 0f);
            var shieldBody = shieldRoot.AddComponent<Rigidbody2D>();
            shieldBody.bodyType = RigidbodyType2D.Kinematic;
            shieldBody.gravityScale = 0f;
            var shieldCollider = shieldRoot.AddComponent<BoxCollider2D>();
            shieldCollider.size = new Vector2(1f, 2.5f);
            var shieldRenderer = shieldRoot.AddComponent<SpriteRenderer>();
            shieldRenderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GreySpritePath);
            shieldRenderer.color = new Color(.72f, .75f, .8f);
            shieldRoot.AddComponent<NonHookable>();
            var pullable = shieldRoot.AddComponent<HookPullable>();
            SetInt(pullable, "weightClass", (int)HookWeightClass.Light);
            SetObject(pullable, "body", shieldBody);
            var shield = shieldRoot.AddComponent<BossShield>();
            SetObject(shield, "boss", brain);
            SetObject(shield, "body", shieldBody);
            SetObject(shield, "pullable", pullable);
            SetObject(shield, "visual", shieldRenderer);
            SetObject(brain, "shield", shield);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return prefab;
        }

        static void BuildArenaPrefab(GameObject bossPrefab, BossDefinition definition, StringEvent started, HealthEvent health, GameEvent respawn, GameEvent defeated)
        {
            string path = PrefabPath + "/BossArena.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var root = new GameObject("BossArena");
            var rootBody = root.AddComponent<Rigidbody2D>();
            rootBody.bodyType = RigidbodyType2D.Kinematic;
            rootBody.gravityScale = 0f;
            var arena = root.AddComponent<BossArena>();
            var entry = new GameObject("EntryTrigger") { layer = 2 };
            entry.transform.SetParent(root.transform, false);
            entry.transform.localPosition = new Vector3(-10f, 3f);
            var entryCollider = entry.AddComponent<BoxCollider2D>();
            entryCollider.isTrigger = true;
            entryCollider.size = new Vector2(2f, 6f);

            var bossInstance = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab);
            bossInstance.name = "Boss";
            bossInstance.transform.SetParent(root.transform, false);
            bossInstance.transform.localPosition = new Vector3(7f, 1.5f);
            // Kapılar duvar boşluğunun iç sütununda (iç alan x [-14, 14), duvarlar dışında).
            GameObject leftDoor = Door(root.transform, "LeftDoor", new Vector3(-14.5f, 1.5f));
            GameObject rightDoor = Door(root.transform, "RightDoor", new Vector3(14.5f, 1.5f));
            // Kirişler arenanın kendi objeleri: alt yüzleri zeminden 7 karo yukarıda, 4 karo geniş.
            Collider2D leftBeam = Beam(root.transform, "BeamLeft", new Vector3(-7f, 7.5f));
            Collider2D rightBeam = Beam(root.transform, "BeamRight", new Vector3(7f, 7.5f));
            leftDoor.SetActive(false);
            rightDoor.SetActive(false);
            SetObject(arena, "boss", bossInstance.GetComponent<BossBrain>());
            SetObject(bossInstance.GetComponent<BossBrain>(), "arena", arena);
            SetObject(arena, "definition", definition);
            SetObject(arena, "entryTrigger", entryCollider);
            SetObject(arena, "fightStarted", started);
            SetObject(arena, "bossHealth", health);
            SetObject(arena, "playerRespawn", respawn);
            SetObject(arena, "bossDefeated", defeated);
            SetArrayObjects(arena, "doors", new Object[] { leftDoor, rightDoor });
            SetArrayObjects(arena, "beamColliders", new Object[] { leftBeam, rightBeam });
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        static Collider2D Beam(Transform parent, string name, Vector3 position)
        {
            var beam = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
            beam.transform.SetParent(parent, false);
            beam.transform.localPosition = position;
            beam.transform.localScale = new Vector3(4f, 1f, 1f);
            beam.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
            var collider = beam.AddComponent<BoxCollider2D>();
            collider.size = Vector2.one;
            var renderer = beam.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GreySpritePath);
            renderer.color = new Color(.42f, .38f, .34f);
            return collider;
        }

        static GameObject Door(Transform parent, string name, Vector3 position)
        {
            var door = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
            door.transform.SetParent(parent, false);
            door.transform.localPosition = position;
            door.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            door.AddComponent<BoxCollider2D>().size = new Vector2(1f, 3f);
            return door;
        }

        static void SetObject(Object target, string field, Object value)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.FindProperty(field);
            if (property != null) { property.objectReferenceValue = value; serialized.ApplyModifiedPropertiesWithoutUndo(); }
        }

        static void SetArrayObjects(Object target, string field, Object[] values)
        {
            var serialized = new SerializedObject(target);
            var array = serialized.FindProperty(field);
            array.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) array.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetString(Object target, string field, string value) { var s = new SerializedObject(target); var p = s.FindProperty(field); if (p != null) { p.stringValue = value; s.ApplyModifiedPropertiesWithoutUndo(); } }
        static void SetFloat(Object target, string field, float value) { var s = new SerializedObject(target); var p = s.FindProperty(field); if (p != null) { p.floatValue = value; s.ApplyModifiedPropertiesWithoutUndo(); } }
        static void SetInt(Object target, string field, int value) { var s = new SerializedObject(target); var p = s.FindProperty(field); if (p != null) { p.intValue = value; s.ApplyModifiedPropertiesWithoutUndo(); } }
        static void SetBool(Object target, string field, bool value) { var s = new SerializedObject(target); var p = s.FindProperty(field); if (p != null) { p.boolValue = value; s.ApplyModifiedPropertiesWithoutUndo(); } }
        static void SetLayerMask(Object target, string field, LayerMask value) { var s = new SerializedObject(target); var p = s.FindProperty(field); if (p != null) { p.intValue = value.value; s.ApplyModifiedPropertiesWithoutUndo(); } }
    }
}
