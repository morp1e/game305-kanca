using System;
using System.Reflection;
using Game.Combat;
using Game.Editor.Enemies;
using Game.Enemies;
using Game.Hook;
using Game.Visual;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Game.Tests
{
    public class EnemyVisualTests
    {
        static readonly BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
        static readonly BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        [Test]
        public void FactoryMigrationMovesRootRendererOnceAndKeepsGreybox()
        {
            var root = new GameObject("Walker");
            root.transform.localScale = new Vector3(1f, .8f, 1f);
            var oldRenderer = root.AddComponent<SpriteRenderer>();
            oldRenderer.color = new Color(1f, .48f, .12f);
            var color = oldRenderer.color;
            var method = typeof(EnemyFactory).GetMethod("EnsureVisual", PrivateStatic);

            try
            {
                method.Invoke(null, new object[] { root, new Vector2(1f, .8f), color, null, false });
                method.Invoke(null, new object[] { root, new Vector2(1f, .8f), color, null, false });

                Assert.That(root.GetComponent<SpriteRenderer>(), Is.Null);
                Assert.That(root.transform.Find("Visual"), Is.Not.Null);
                Assert.That(root.GetComponentsInChildren<SpriteRenderer>(true).Length, Is.EqualTo(1));
                Assert.That(root.GetComponentsInChildren<SpriteAnimPlayer>(true).Length, Is.EqualTo(1));
                var renderer = root.transform.Find("Visual").GetComponent<SpriteRenderer>();
                Assert.That(renderer.color, Is.EqualTo(color));
                Assert.That(renderer.sprite, Is.Not.Null);
                Assert.That(root.transform.Find("Visual").localScale, Is.EqualTo(new Vector3(1f, .8f, 1f)));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BruteShieldUsesChestSizedPlacementForColliderBody()
        {
            var root = new GameObject("Brute");
            var bodyRenderer = root.AddComponent<SpriteRenderer>();
            var ensureShield = typeof(EnemyFactory).GetMethod("EnsureShield", PrivateStatic);

            try
            {
                ensureShield.Invoke(null, new object[] { root, bodyRenderer, new Vector2(1.3f, 2.4f) });
                Transform shield = root.transform.Find("Shield");

                Assert.That(shield.localPosition.x, Is.EqualTo(0.546f).Within(0.0001f));
                Assert.That(shield.localPosition.y, Is.EqualTo(0.24f).Within(0.0001f));
                Assert.That(shield.localScale.x, Is.EqualTo(0.26f).Within(0.0001f));
                Assert.That(shield.localScale.y, Is.EqualTo(1.008f).Within(0.0001f));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BruteShieldVisualMirrorsWithFacingWithoutChangingShieldArcMath()
        {
            var root = new GameObject("Brute");
            root.SetActive(false);
            var body = root.AddComponent<SpriteRenderer>();
            var shield = new GameObject("Shield");
            shield.transform.SetParent(root.transform, false);
            shield.transform.localPosition = new Vector3(.5f, .2f, 0f);
            shield.transform.localScale = new Vector3(.3f, .8f, 1f);
            shield.AddComponent<SpriteRenderer>();
            root.AddComponent<Rigidbody2D>();
            root.AddComponent<BoxCollider2D>();
            root.AddComponent<ContactDamage>();
            root.AddComponent<HookPullable>();
            var brain = root.AddComponent<BruteBrain>();
            root.SetActive(true);

            try
            {
                var facing = typeof(EnemyBrain).GetProperty("Facing", BindingFlags.Instance | BindingFlags.NonPublic);
                var updateVisual = typeof(EnemyBrain).GetMethod("UpdateVisual", PrivateInstance);
                var shieldCache = typeof(EnemyBrain).GetField("shieldVisual", PrivateInstance);
                if (shieldCache.GetValue(brain) == null)
                    typeof(EnemyBrain).GetMethod("Awake", PrivateInstance).Invoke(brain, null);
                facing.GetSetMethod(true).Invoke(brain, new object[] { -1 });
                Assert.That(facing.GetValue(brain), Is.EqualTo(-1), "test düşman yönünü sola ayarlamalı");
                updateVisual.Invoke(brain, null);

                Assert.That(shield.transform.localPosition.x, Is.EqualTo(-.5f).Within(.0001f));
                Assert.That(shield.transform.position.x, Is.EqualTo(-.5f).Within(.0001f));
                Assert.That(shield.transform.localScale.x, Is.EqualTo(-.3f).Within(.0001f));
                Assert.That(ShieldMath.IsBlocked(Vector2.right, Vector2.zero, Vector2.right, 120f), Is.True);
                Assert.That(ShieldMath.IsBlocked(Vector2.left, Vector2.zero, Vector2.right, 120f), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [TestCase(EnemyState.Patrol, false, "walk")]
        [TestCase(EnemyState.Chase, false, "walk")]
        [TestCase(EnemyState.Patrol, true, "fly")]
        [TestCase(EnemyState.Notice, false, "idle")]
        [TestCase(EnemyState.Telegraph, false, "windup")]
        [TestCase(EnemyState.Attack, false, "attack")]
        [TestCase(EnemyState.Recover, false, "recover")]
        [TestCase(EnemyState.Hurt, false, "hurt")]
        [TestCase(EnemyState.Stunned, false, "hurt")]
        [TestCase(EnemyState.Dead, false, "death")]
        public void AnimatorMapsStatesToClips(EnemyState state, bool flyer, string expected)
        {
            Assert.That(EnemyAnimator.ClipName(state, flyer), Is.EqualTo(expected));
        }

        [Test]
        public void AnimatorPlaysMappedClipFromAssignedSet()
        {
            var root = new GameObject("Walker");
            root.SetActive(false);
            var definition = ScriptableObject.CreateInstance<EnemyDefinition>();
            var set = ScriptableObject.CreateInstance<SpriteAnimSet>();
            var texture = new Texture2D(9, 1);
            string[] names = { "walk", "fly", "idle", "windup", "attack", "recover", "hurt", "death" };
            var sprites = new Sprite[names.Length + 1];
            var clips = new SpriteAnimClip[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                sprites[i] = Sprite.Create(texture, new Rect(i, 0, 1, 1), new Vector2(.5f, .5f));
                clips[i] = new SpriteAnimClip { name = names[i], frames = new[] { sprites[i] }, loop = true };
            }
            sprites[names.Length] = Sprite.Create(texture, new Rect(names.Length, 0, 1, 1), new Vector2(.5f, .5f));
            set.clips = clips;
            definition.visualSet = set;
            root.AddComponent<Rigidbody2D>();
            root.AddComponent<BoxCollider2D>();
            root.AddComponent<ContactDamage>();
            root.AddComponent<HookPullable>();
            var brain = root.AddComponent<WalkerBrain>();
            var health = root.GetComponent<EnemyHealth>();
            SetField(brain, "definition", definition);
            SetField(health, "definition", definition);
            SetField(health, "brain", brain);
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprites[names.Length];
            var player = visual.AddComponent<SpriteAnimPlayer>();
            SetField(player, "spriteRenderer", renderer);
            SetField(player, "set", set);
            var animator = root.AddComponent<EnemyAnimator>();
            SetField(animator, "brain", brain);
            SetField(animator, "player", player);

            try
            {
                root.SetActive(true);
                FieldInfo rendererCache = typeof(EnemyAnimator).GetField("spriteRenderer", PrivateInstance);
                if (rendererCache.GetValue(animator) == null)
                    typeof(EnemyAnimator).GetMethod("Awake", PrivateInstance).Invoke(animator, null);

                MethodInfo handle = typeof(EnemyAnimator).GetMethod("HandleStateChanged", PrivateInstance);
                foreach (EnemyState state in new[] { EnemyState.Patrol, EnemyState.Chase, EnemyState.Notice, EnemyState.Telegraph, EnemyState.Attack, EnemyState.Recover, EnemyState.Hurt, EnemyState.Stunned, EnemyState.Dead })
                {
                    handle.Invoke(animator, new object[] { EnemyState.Patrol, state });
                    string clip = EnemyAnimator.ClipName(state, false);
                    int clipIndex = Array.IndexOf(names, clip);
                    Assert.That(renderer.sprite, Is.SameAs(sprites[clipIndex]), state.ToString());
                }
                set.clips = new[] { clips[0] };
                handle.Invoke(animator, new object[] { EnemyState.Patrol, EnemyState.Notice });
                Assert.That(renderer.sprite, Is.SameAs(sprites[names.Length]), "Set içinde olmayan klipte gri kutu sprite'ı korunmalı.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                UnityEngine.Object.DestroyImmediate(definition);
                UnityEngine.Object.DestroyImmediate(set);
                foreach (Sprite sprite in sprites) UnityEngine.Object.DestroyImmediate(sprite);
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        static void SetField(object target, string name, object value)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
                if (field == null) continue;
                field.SetValue(target, value);
                return;
            }
            throw new MissingFieldException(target.GetType().Name, name);
        }
    }
}
