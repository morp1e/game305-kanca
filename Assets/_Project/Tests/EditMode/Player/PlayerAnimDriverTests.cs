using System.Reflection;
using Game.Player;
using Game.Visual;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class PlayerAnimDriverTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [Test]
        public void LocomotionAndDamageStatesChooseExpectedClips()
        {
            Assert.AreEqual("idle", PlayerAnimDriver.SelectClip(Vector2.zero, true, false, false, false, 0.1f));
            Assert.AreEqual("run", PlayerAnimDriver.SelectClip(Vector2.right, true, false, false, false, 0.1f));
            Assert.AreEqual("jump", PlayerAnimDriver.SelectClip(Vector2.up, false, false, false, false, 0.1f));
            Assert.AreEqual("fall", PlayerAnimDriver.SelectClip(Vector2.down, false, false, false, false, 0.1f));
            Assert.AreEqual("wall_slide", PlayerAnimDriver.SelectClip(Vector2.down, false, true, false, false, 0.1f));
            Assert.AreEqual("hurt", PlayerAnimDriver.SelectClip(Vector2.zero, true, false, true, false, 0.1f));
            Assert.AreEqual("death", PlayerAnimDriver.SelectClip(Vector2.zero, true, false, true, true, 0.1f));
        }

        [Test]
        public void FacingDirectionMapsToSpriteFlipWithoutChangingClipChoice()
        {
            Assert.IsTrue(PlayerAnimDriver.IsFacingLeft(-1));
            Assert.IsFalse(PlayerAnimDriver.IsFacingLeft(1));
            Assert.AreEqual(
                PlayerAnimDriver.SelectClip(Vector2.right, true, false, false, false, 0.1f),
                PlayerAnimDriver.SelectClip(Vector2.left, true, false, false, false, 0.1f));
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

    }
}
