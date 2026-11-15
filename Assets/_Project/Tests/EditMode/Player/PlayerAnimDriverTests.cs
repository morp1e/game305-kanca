using System.Reflection;
using Game.Player;
using Game.Visual;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class PlayerAnimDriverTests
    {
        GameObject playerRoot;
        GameObject visual;
        PlayerMotor motor;
        PlayerHealth health;
        PlayerArmRig armRig;
        PlayerAnimDriver driver;
        SpriteAnimSet set;
        Sprite[] frames;

        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp()
        {
            playerRoot = new GameObject("Player");
            playerRoot.SetActive(false);
            playerRoot.AddComponent<Rigidbody2D>();
            playerRoot.AddComponent<CapsuleCollider2D>();
            motor = playerRoot.AddComponent<PlayerMotor>();
            SetField(motor, "body", playerRoot.GetComponent<Rigidbody2D>());
            SetField(motor, "tuning", ScriptableObject.CreateInstance<PlayerMovementTuning>());
            health = playerRoot.AddComponent<PlayerHealth>();
            SetField(health, "tuning", ScriptableObject.CreateInstance<PlayerHealthTuning>());
            armRig = playerRoot.AddComponent<PlayerArmRig>();

            visual = new GameObject("Visual");
            visual.transform.SetParent(playerRoot.transform, false);
            var renderer = visual.AddComponent<SpriteRenderer>();
            var player = visual.AddComponent<SpriteAnimPlayer>();
            driver = visual.AddComponent<PlayerAnimDriver>();
            set = ScriptableObject.CreateInstance<SpriteAnimSet>();
            frames = new Sprite[1];
            var texture = new Texture2D(1, 1);
            frames[0] = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(.5f, 0f));
            var names = new[] { "idle", "run", "jump", "fall", "wall_slide", "hurt", "death" };
            var clips = new SpriteAnimClip[names.Length];
            for (int i = 0; i < clips.Length; i++)
                clips[i] = new SpriteAnimClip { name = names[i], frames = frames, fps = 10f, loop = i < 5 };
            set.clips = clips;
            SetField(player, "spriteRenderer", renderer);
            SetField(player, "set", set);
            SetField(driver, "motor", motor);
            SetField(driver, "health", health);
            SetField(driver, "armRig", armRig);
            SetField(driver, "animPlayer", player);
            playerRoot.SetActive(true);
            SetField(health, "currentHealth", 5f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(playerRoot);
            Object.DestroyImmediate(set);
            foreach (Sprite sprite in frames)
            {
                Texture2D texture = sprite.texture;
                Object.DestroyImmediate(sprite);
                Object.DestroyImmediate(texture);
            }
        }

        [Test]
        public void LocomotionAndDamageStatesChooseExpectedClips()
        {
            SetMotorState(true, false, Vector2.zero);
            Tick(); Assert.AreEqual("idle", driver.ActiveClipName);
            SetMotorState(true, false, Vector2.right);
            Tick(); Assert.AreEqual("run", driver.ActiveClipName);
            SetMotorState(false, false, Vector2.up);
            Tick(); Assert.AreEqual("jump", driver.ActiveClipName);
            SetMotorState(false, false, Vector2.down);
            Tick(); Assert.AreEqual("fall", driver.ActiveClipName);
            SetMotorState(false, true, Vector2.down);
            Tick(); Assert.AreEqual("wall_slide", driver.ActiveClipName);

            driver.SendMessage("HandleDamaged");
            Tick(); Assert.AreEqual("hurt", driver.ActiveClipName);
            driver.SendMessage("HandleDied");
            Tick(); Assert.AreEqual("death", driver.ActiveClipName);
        }

        [Test]
        public void FacingLeftFlipsOnlyTheSpriteRenderer()
        {
            typeof(PlayerArmRig).GetField("<Facing>k__BackingField", Private).SetValue(armRig, -1);
            Vector3 scale = visual.transform.localScale;
            Tick();
            Assert.IsTrue(visual.GetComponent<SpriteRenderer>().flipX);
            Assert.AreEqual(scale, visual.transform.localScale);
        }

        [Test]
        public void ArmSpritesFollowBodySetWithoutChangingHandTips()
        {
            var hookArm = new GameObject("HookArm");
            var swordArm = new GameObject("SwordArm");
            var hookSprite = new GameObject("Sprite");
            var swordSprite = new GameObject("Sprite");
            hookSprite.transform.SetParent(hookArm.transform, false);
            swordSprite.transform.SetParent(swordArm.transform, false);
            hookSprite.AddComponent<SpriteRenderer>();
            swordSprite.AddComponent<SpriteRenderer>();
            var handTip = new GameObject("HandTip");
            handTip.transform.SetParent(hookArm.transform, false);

            try
            {
                var configure = typeof(Game.Editor.Sandbox.PrefabFactory).GetMethod(
                    "ConfigureArmSpriteVisibility", BindingFlags.Static | BindingFlags.NonPublic);
                configure.Invoke(null, new object[] { hookArm, swordArm, true });

                Assert.IsFalse(hookSprite.GetComponent<SpriteRenderer>().enabled);
                Assert.IsFalse(swordSprite.GetComponent<SpriteRenderer>().enabled);
                Assert.AreSame(handTip.transform, hookArm.transform.Find("HandTip"));

                configure.Invoke(null, new object[] { hookArm, swordArm, false });
                Assert.IsTrue(hookSprite.GetComponent<SpriteRenderer>().enabled);
                Assert.IsTrue(swordSprite.GetComponent<SpriteRenderer>().enabled);
            }
            finally
            {
                Object.DestroyImmediate(hookArm);
                Object.DestroyImmediate(swordArm);
            }
        }

        void SetMotorState(bool grounded, bool wallSliding, Vector2 velocity)
        {
            typeof(PlayerMotor).GetField("<IsGrounded>k__BackingField", Private).SetValue(motor, grounded);
            typeof(PlayerMotor).GetField("<IsWallSliding>k__BackingField", Private).SetValue(motor, wallSliding);
            motor.Velocity = velocity;
        }

        void Tick() => driver.SendMessage("Update");

        static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, Private).SetValue(target, value);
    }
}
