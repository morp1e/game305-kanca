using Game.Combat;
using Game.Core;
using Game.Enemies;
using Game.Hook;
using UnityEditor;
using UnityEngine;
namespace Game.Editor.Enemies
{
    /// <summary>Düşman ayarlarını ve prefablarını idempotent biçimde üretir.</summary>
    public static class EnemyFactory
    {
        const string DataPath = "Assets/_Project/Data/Enemies";
        const string PrefabPath = "Assets/_Project/Prefabs/Enemies";
        const string SpritePath = "Assets/_Project/Art/Pixel/_Greybox/greybox_square.png";
        const string EventPath = "Assets/_Project/Data/Events/Event_PlayerRespawn.asset";
        [MenuItem("Tools/Kanca/Dusmanlari Kur")]
        public static void BuildAll()
        {
            EnsureFolder("Assets/_Project/Data");
            EnsureFolder(DataPath);
            EnsureFolder("Assets/_Project/Prefabs");
            EnsureFolder(PrefabPath);
            ProjectileDefinition projectileDefinition = GetAsset<ProjectileDefinition>(DataPath + "/ProjectileDef_Spit.asset");
            Projectile projectilePrefab = BuildProjectile(projectileDefinition);
            GameEvent respawn = AssetDatabase.LoadAssetAtPath<GameEvent>(EventPath);
            BuildEnemy("Walker", typeof(WalkerBrain), new Color(1f, .48f, .12f), new Vector2(1f, .8f), 2f, 2f, 5f, 6f, .4f, .8f, 1f, 3f, HookWeightClass.Light, projectileDefinition, projectilePrefab, respawn);
            BuildEnemy("Flyer", typeof(FlyerBrain), new Color(.65f, .32f, .9f), new Vector2(.8f, .8f), 2f, 3f, 6f, 7f, .35f, .7f, .8f, 0f, HookWeightClass.Light, projectileDefinition, projectilePrefab, respawn);
            BuildEnemy("Spitter", typeof(SpitterBrain), new Color(.24f, .8f, .28f), new Vector2(1f, 1.2f), 3f, 0f, 0f, 9f, .5f, .2f, 1.8f, 3f, HookWeightClass.Light, projectileDefinition, projectilePrefab, respawn);
            BuildEnemy("Brute", typeof(BruteBrain), new Color(.48f, .12f, .15f), new Vector2(1.6f, 2.2f), 5f, 1.5f, 1.5f, 6f, .6f, .2f, 1.5f, 3f, HookWeightClass.Heavy, projectileDefinition, projectilePrefab, respawn);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Düşman tanımları ve prefabları kuruldu.");
        }
        static void BuildEnemy(string name, System.Type brainType, Color color, Vector2 size, float health, float speed, float chase, float radius, float telegraph, float attack, float recover, float gravity, HookWeightClass weight, ProjectileDefinition projectile, Projectile projectilePrefab, GameEvent respawn)
        {
            string definitionPath = DataPath + "/EnemyDef_" + name + ".asset";
            bool newDefinition = AssetDatabase.LoadAssetAtPath<EnemyDefinition>(definitionPath) == null;
            EnemyDefinition definition = GetAsset<EnemyDefinition>(definitionPath);
            if (newDefinition)
            {
                definition.maxHealth = health;
                definition.projectile = projectile;
                definition.gravityScale = gravity;
                definition.moveSpeed = speed;
                definition.chaseSpeed = chase;
                definition.detectionRadius = radius;
                definition.loseTargetRadius = radius + 2f;
                definition.telegraphDuration = telegraph;
                definition.attackDuration = attack;
                definition.recoverDuration = recover;
                definition.shieldArcDegrees = name == "Brute" ? 120f : 0f;
                definition.knockbackResistance = name == "Brute" ? 1f : 0f;
                definition.attackCooldown = name == "Spitter" ? 1.8f : recover;
                definition.attackBoxSize = new Vector2(2f, 1.5f);
                if (name == "Brute") definition.attackDamage = 2f;
            }
            if (definition.projectile == null) definition.projectile = projectile;
            // Alanlar CreateAsset'ten sonra C# ile yazıldı; işaretlenmezse Unity diske varsayılanları kaydeder
            // (2026-10-30: dört tanımın hepsi Walker değerleriyle kalmıştı).
            EditorUtility.SetDirty(definition);
            string prefab = PrefabPath + "/" + name + ".prefab";
            bool newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefab) == null;
            GameObject root = LoadOrCreateRoot(name, prefab, out bool loadedContents);
            if (newPrefab)  {
                root.layer = LayerMask.NameToLayer("Enemy");
                root.transform.localScale = new Vector3(size.x, size.y, 1f);
            }
            SpriteRenderer renderer = Ensure<SpriteRenderer>(root);
            if (newPrefab)  {
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
                renderer.color = color;
            }
            Rigidbody2D body = Ensure<Rigidbody2D>(root);
            if (newPrefab)  {
                body.bodyType = RigidbodyType2D.Dynamic;
                body.gravityScale = gravity;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
            }
            BoxCollider2D collider = Ensure<BoxCollider2D>(root);
            if (newPrefab) collider.isTrigger = false;
            ContactDamage contact = Ensure<ContactDamage>(root);
            if (newPrefab) Set(contact, "damage", definition.contactDamage);
            HookPullable pullable = Ensure<HookPullable>(root);
            if (newPrefab) Set(pullable, "weightClass", (int)weight);
            Set(pullable, "body", body);
            EnemyBrain brain = root.GetComponent(brainType) as EnemyBrain;
            if (brain == null) brain = root.AddComponent(brainType) as EnemyBrain;
            Set(brain, "definition", definition);
            EnemyHealth enemyHealth = Ensure<EnemyHealth>(root);
            Set(enemyHealth, "definition", definition);
            Set(enemyHealth, "brain", brain);
            Set(enemyHealth, "playerRespawnEvent", respawn);
            if (newPrefab) collider.size = Vector2.one;
            if (name == "Spitter")  {
                var spitter = (SpitterBrain)brain;
                Set(spitter, "projectilePrefab", projectilePrefab);
            }
            if (name == "Brute" && root.transform.Find("Shield") == null) EnsureShield(root, renderer);
            SaveRoot(root, prefab, loadedContents);
        }
        static Projectile BuildProjectile(ProjectileDefinition definition)
        {
            string path = PrefabPath + "/Projectile_Spit.prefab";
            bool newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path) == null;
            GameObject root = LoadOrCreateRoot("Projectile_Spit", path, out bool loadedContents);
            if (newPrefab) root.layer = LayerMask.NameToLayer("Enemy");
            SpriteRenderer renderer = Ensure<SpriteRenderer>(root);
            if (newPrefab)  {
                renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
                renderer.color = new Color(.3f, .85f, .3f);
            }
            Rigidbody2D body = Ensure<Rigidbody2D>(root);
            if (newPrefab)  {
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
            }
            CircleCollider2D collider = Ensure<CircleCollider2D>(root);
            if (newPrefab)  {
                collider.isTrigger = true;
                collider.radius = definition.radius;
            }
            Ensure<Projectile>(root);
            return SaveRoot(root, path, loadedContents).GetComponent<Projectile>();
        }
        static void EnsureShield(GameObject root, SpriteRenderer parent)
        {
            Transform child = root.transform.Find("Shield");
            GameObject shield = child != null ? child.gameObject : new GameObject("Shield");
            shield.transform.SetParent(root.transform, false);
            shield.layer = LayerMask.NameToLayer("Enemy");
            shield.transform.localPosition = Vector3.right * .35f;
            shield.transform.localScale = new Vector3(.18f, .7f, 1f);
            SpriteRenderer renderer = Ensure<SpriteRenderer>(shield);
            renderer.sprite = parent.sprite;
            renderer.color = new Color(.8f, .8f, .78f);
            renderer.sortingOrder = parent.sortingOrder + 1;
        }
        static GameObject LoadOrCreateRoot(string name, string path, out bool loadedContents)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            loadedContents = existing != null;
            if (!loadedContents) return new GameObject(name);
            return PrefabUtility.LoadPrefabContents(path);
        }
        static GameObject SaveRoot(GameObject root, string path, bool loadedContents)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            if (loadedContents) PrefabUtility.UnloadPrefabContents(root);
            else Object.DestroyImmediate(root);
            return saved;
        }
        static T Ensure<T>(GameObject root) where T : Component  {
            T component = root.GetComponent<T>();
            return component != null ? component : root.AddComponent<T>();
        }
        static T GetAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
        static void Set(Object target, string name, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null && property.objectReferenceValue == null)  {
                property.objectReferenceValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        static void Set(Object target, string name, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null)  {
                property.intValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        static void Set(Object target, string name, float value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(name);
            if (property != null && property.floatValue == 0f)  {
                property.floatValue = value;
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }
        }
        static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Substring(7).Split('/'))  {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }
    }
}
