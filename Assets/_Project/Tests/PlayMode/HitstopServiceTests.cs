using System.Collections;
using Game.Combat;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public class HitstopServiceTests
    {
        GameObject host;
        GameObject reactionObject;
        HitstopService service;
        float originalScale;

        [SetUp]
        public void Setup()
        {
            originalScale = Time.timeScale;
            Time.timeScale = 0.7f;
            host = new GameObject("HitstopServiceTest");
            service = host.AddComponent<HitstopService>();
        }

        [TearDown]
        public void Teardown()
        {
            if (host != null) Object.DestroyImmediate(host);
            if (reactionObject != null) Object.DestroyImmediate(reactionObject);
            Time.timeScale = originalScale;
        }

        [UnityTest]
        public IEnumerator HitstopLowersScaleThenRestoresPreviousValue()
        {
            Assert.IsTrue(service.Request(0.12f, stopScale: 0.05f));
            Assert.That(Time.timeScale, Is.EqualTo(0.05f).Within(0.001f));
            yield return new WaitForSecondsRealtime(0.18f);
            Assert.IsFalse(service.IsActive);
            Assert.That(Time.timeScale, Is.EqualTo(0.7f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator NestedRequestKeepsTheLongestRemainingHitstop()
        {
            Assert.IsTrue(service.Request(0.1f, stopScale: 0.05f));
            yield return new WaitForSecondsRealtime(0.04f);
            Assert.IsTrue(service.Request(0.22f, stopScale: 0.05f));
            yield return new WaitForSecondsRealtime(0.13f);
            Assert.IsTrue(service.IsActive);
            yield return new WaitForSecondsRealtime(0.16f);
            Assert.IsFalse(service.IsActive);
            Assert.That(Time.timeScale, Is.EqualTo(0.7f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator RootSpriteHitReactionNeverWritesRigidbodyTransform()
        {
            reactionObject = new GameObject("RootSpriteReaction");
            var body = reactionObject.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            reactionObject.AddComponent<SpriteRenderer>();
            var reaction = reactionObject.AddComponent<CombatHitReaction>();
            Assert.IsTrue(service.Request(0.12f, stopScale: 0.05f));
            reaction.Play(0.12f);
            yield return new WaitForSecondsRealtime(0.02f);
            Vector3 movedPosition = new Vector3(3f, 4f, 0f);
            reactionObject.transform.position = movedPosition;
            yield return new WaitForSecondsRealtime(0.15f);
            Assert.AreEqual(movedPosition, reactionObject.transform.position);
        }
    }
}
