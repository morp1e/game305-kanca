using System.Reflection;
using Game.Boss;
using Game.Visual;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class BossAnimatorTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject visual;
        SpriteAnimSet set;
        Sprite[] frames;
        BossAnimator animator;

        [SetUp]
        public void SetUp()
        {
            visual = new GameObject("Visual");
            var renderer = visual.AddComponent<SpriteRenderer>();
            var player = visual.AddComponent<SpriteAnimPlayer>();
            animator = visual.AddComponent<BossAnimator>();
            typeof(BossAnimator).GetField("player", Private).SetValue(animator, player);

            frames = new Sprite[8];
            string[] names = { "idle", "move", "windup", "attack", "recover", "hurt", "stagger", "death" };
            var clips = new SpriteAnimClip[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                frames[i] = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
                clips[i] = new SpriteAnimClip { name = names[i], frames = new[] { frames[i] }, loop = i != 5 };
            }
            set = ScriptableObject.CreateInstance<SpriteAnimSet>();
            set.clips = clips;
            typeof(SpriteAnimPlayer).GetField("spriteRenderer", Private).SetValue(player, renderer);
            typeof(SpriteAnimPlayer).GetField("set", Private).SetValue(player, set);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(visual);
            Object.DestroyImmediate(set);
            foreach (Sprite frame in frames) Object.DestroyImmediate(frame);
        }

        [TestCase(BossState.Dormant, false, 0)]
        [TestCase(BossState.Idle, false, 0)]
        [TestCase(BossState.PhaseTransition, false, 0)]
        [TestCase(BossState.Telegraph, false, 2)]
        [TestCase(BossState.Active, false, 3)]
        [TestCase(BossState.Active, true, 1)]
        [TestCase(BossState.Recover, false, 4)]
        [TestCase(BossState.Staggered, false, 6)]
        [TestCase(BossState.Dead, false, 7)]
        public void StateMapsToClipAndPlaysSyntheticSet(BossState state, bool moving, int expectedFrame)
        {
            animator.PlayState(state, 1.6f, moving);

            Assert.AreEqual(frames[expectedFrame], visual.GetComponent<SpriteRenderer>().sprite);
        }

        [Test]
        public void MissingClipDoesNotReplaceExistingSprite()
        {
            var renderer = visual.GetComponent<SpriteRenderer>();
            renderer.sprite = frames[5];
            set.clips = new SpriteAnimClip[0];

            animator.PlayState(BossState.Telegraph, 0.8f);

            Assert.AreEqual(frames[5], renderer.sprite);
        }

        [Test]
        public void HurtClipReturnsToCurrentBossStateWhenItFinishes()
        {
            animator.PlayState(BossState.Recover, 1.6f);

            animator.PlayHurt();
            Assert.AreEqual(frames[5], visual.GetComponent<SpriteRenderer>().sprite);

            visual.GetComponent<SpriteAnimPlayer>().Advance(0.2f);

            Assert.AreEqual(frames[4], visual.GetComponent<SpriteRenderer>().sprite);
        }
    }
}
