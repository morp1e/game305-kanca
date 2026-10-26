using Game.Player;
using NUnit.Framework;

namespace Game.Tests
{
    public class JumpPreviewMathTests
    {
        [Test]
        public void DefaultHeldJumpMatchesPhysicsReference()
        {
            float height = JumpPreviewMath.SimulateHeight(13.125f, 112.5f, 5f, 20f, 0.2f, 0.1f, 0.08f, 0.02f, true);
            Assert.That(height, Is.EqualTo(3.603f).Within(0.15f));
        }

        [Test]
        public void ReleasingJumpImmediatelyProducesLowerJump()
        {
            float held = JumpPreviewMath.SimulateHeight(13.125f, 112.5f, 5f, 20f, 0.2f, 0.1f, 0.08f, 0.02f, true);
            float tapped = JumpPreviewMath.SimulateHeight(13.125f, 112.5f, 5f, 20f, 0.2f, 0.1f, 0.08f, 0.02f, false);
            Assert.That(tapped, Is.LessThan(held));
        }

        [Test]
        public void ApexTimeIsZeroForInvalidStep()
        {
            Assert.That(JumpPreviewMath.SimulateTimeToApex(13f, 100f, 5f, 20f, 0.2f, 0.1f, 0.08f, 0f), Is.EqualTo(0f));
        }
    }
}
