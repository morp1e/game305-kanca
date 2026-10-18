using System.Reflection;
using Game.Dialogue;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class LineSelectorTests
    {
        SwordLineSet set;
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp() => set = ScriptableObject.CreateInstance<SwordLineSet>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(set);

        static SwordLineSet.CueLines Entry(SwordCue cue, float chance, float cooldown, params string[] lines) =>
            new SwordLineSet.CueLines { cue = cue, chance = chance, cooldown = cooldown, lines = lines };

        void Configure(params SwordLineSet.CueLines[] entries) =>
            typeof(SwordLineSet).GetField("cues", Private).SetValue(set, entries);

        [Test]
        public void ChanceZero_NeverSelects()
        {
            Configure(Entry(SwordCue.Death, 0f, 0f, "Hayattayım."));
            var selector = new LineSelector(() => 0f);
            Assert.That(selector.Select(set, SwordCue.Death, 1f), Is.Null);
        }

        [Test]
        public void ChanceOne_SelectsLine()
        {
            Configure(Entry(SwordCue.Death, 1f, 0f, "Hayattayım."));
            var selector = new LineSelector(() => 0f);
            Assert.That(selector.Select(set, SwordCue.Death, 1f), Is.EqualTo("Hayattayım."));
        }

        [Test]
        public void CueCooldown_PreventsSelectionUntilElapsed()
        {
            Configure(Entry(SwordCue.Stick, 1f, 2f, "Tuttum."));
            var selector = new LineSelector(() => 0f);
            Assert.That(selector.Select(set, SwordCue.Stick, 1f), Is.EqualTo("Tuttum."));
            Assert.That(selector.Select(set, SwordCue.Stick, 2.9f), Is.Null);
            Assert.That(selector.Select(set, SwordCue.Stick, 3f), Is.EqualTo("Tuttum."));
        }

        [Test]
        public void GlobalCooldown_PreventsOtherCue()
        {
            set.globalCooldown = 2f;
            Configure(Entry(SwordCue.Stick, 1f, 0f, "Saplandım."), Entry(SwordCue.Death, 1f, 0f, "Yandık."));
            var selector = new LineSelector(() => 0f);
            Assert.That(selector.Select(set, SwordCue.Stick, 1f), Is.EqualTo("Saplandım."));
            Assert.That(selector.Select(set, SwordCue.Death, 2.9f), Is.Null);
            Assert.That(selector.Select(set, SwordCue.Death, 3f), Is.EqualTo("Yandık."));
        }

        [Test]
        public void ConsecutiveSelection_DoesNotRepeatSameLine()
        {
            Configure(Entry(SwordCue.Respawn, 1f, 0f, "Bir.", "İki."));
            var selector = new LineSelector(() => 0f);
            var first = selector.Select(set, SwordCue.Respawn, 1f);
            var second = selector.Select(set, SwordCue.Respawn, 2f);
            Assert.That(first, Is.Not.EqualTo(second));
        }

        [Test]
        public void EmptyList_ReturnsNull()
        {
            Configure(Entry(SwordCue.Idle, 1f, 0f));
            Assert.That(new LineSelector(() => 0f).Select(set, SwordCue.Idle, 1f), Is.Null);
        }

        [Test]
        public void MissingCue_ReturnsNull()
        {
            Configure(Entry(SwordCue.Idle, 1f, 0f, "Bekliyorum."));
            Assert.That(new LineSelector(() => 0f).Select(set, SwordCue.Death, 1f), Is.Null);
        }

        [Test]
        public void NullSet_ReturnsNull()
        {
            Assert.That(new LineSelector(() => 0f).Select(null, SwordCue.Death, 1f), Is.Null);
        }
    }
}
