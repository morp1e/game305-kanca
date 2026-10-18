using Game.Feedback;
using Game.Hook;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class HookFeedbackMathTests
    {
        [Test]
        public void PulseMultiplier_StaysWithinConfiguredBounds()
        {
            for (var i = 0; i < 100; i++)
            {
                var value = HookFeedbackMath.PulseMultiplier(i * 0.017f, 6f, 1.15f);
                Assert.That(value, Is.InRange(1f, 1.15f));
            }
        }

        [Test]
        public void PulseMultiplier_HitsBothExtremes()
        {
            Assert.That(HookFeedbackMath.PulseMultiplier(0.125f, 2f, 1.15f), Is.EqualTo(1.15f).Within(0.0001f));
            Assert.That(HookFeedbackMath.PulseMultiplier(0.375f, 2f, 1.15f), Is.EqualTo(1f).Within(0.0001f));
        }

        [TestCase(HookState.InHand, false)]
        [TestCase(HookState.Thrown, true)]
        [TestCase(HookState.Stuck, false)]
        [TestCase(HookState.Pulling, false)]
        [TestCase(HookState.Launch, false)]
        [TestCase(HookState.Returning, true)]
        public void TrailEmission_MatchesHookState(HookState state, bool expected)
        {
            Assert.That(HookFeedbackMath.ShouldEmitTrail(state), Is.EqualTo(expected));
        }

        [Test]
        public void DashTexture_HasRequestedDimensionsAndPointFiltering()
        {
            var texture = HookFeedbackMath.CreateDashTexture(8, 4);
            try
            {
                Assert.That(texture.width, Is.EqualTo(8));
                Assert.That(texture.height, Is.EqualTo(1));
                Assert.That(texture.filterMode, Is.EqualTo(FilterMode.Point));
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test]
        public void DashTexture_AlternatesOpaqueAndTransparentPixels()
        {
            var texture = HookFeedbackMath.CreateDashTexture(8, 4);
            try
            {
                for (var x = 0; x < 4; x++) Assert.That(texture.GetPixel(x, 0).a, Is.EqualTo(1f));
                for (var x = 4; x < 8; x++) Assert.That(texture.GetPixel(x, 0).a, Is.EqualTo(0f));
            }
            finally { Object.DestroyImmediate(texture); }
        }

        [Test]
        public void TrailWidth_TapersToZero()
        {
            var curve = HookFeedbackMath.TrailWidth;
            Assert.That(curve.Evaluate(0f), Is.EqualTo(1f).Within(0.001f));
            Assert.That(curve.Evaluate(1f), Is.EqualTo(0f).Within(0.001f));
        }
    }
}
