using System.Collections;
using System.Reflection;
using Game.Core;
using Game.Dialogue;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public class SwordVoiceTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject voiceObject;
        SwordVoice voice;
        GameEvent deathEvent;
        SwordLineSet lines;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            voiceObject = new GameObject("SwordVoiceTest");
            voiceObject.SetActive(false);
            voice = voiceObject.AddComponent<SwordVoice>();
            deathEvent = ScriptableObject.CreateInstance<GameEvent>();
            lines = ScriptableObject.CreateInstance<SwordLineSet>();
            typeof(SwordLineSet).GetField("cues", Private).SetValue(lines, new[]
            {
                new SwordLineSet.CueLines { cue = SwordCue.Death, lines = new[] { "Yine mi?" }, chance = 1f, cooldown = 0f }
            });
            lines.globalCooldown = 0f;
            typeof(SwordVoice).GetField("lines", Private).SetValue(voice, lines);
            typeof(SwordVoice).GetField("deathEvent", Private).SetValue(voice, deathEvent);
            voiceObject.SetActive(true);
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (voiceObject != null) Object.Destroy(voiceObject);
            if (deathEvent != null) Object.Destroy(deathEvent);
            if (lines != null) Object.Destroy(lines);
            yield return null;
        }

        [UnityTest]
        public IEnumerator DeathEvent_ShowsDeathLine_WithoutHook()
        {
            deathEvent.Raise();
            Assert.That(voice.CurrentLine, Is.EqualTo("Yine mi?"));
            yield return null;
        }
    }
}
