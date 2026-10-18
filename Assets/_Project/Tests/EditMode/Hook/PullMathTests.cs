using NUnit.Framework;
using UnityEngine;
using Game.Hook;

namespace Game.Tests.EditMode.Hook
{
    public class PullMathTests
    {
        [Test]
        public void ExitVelocityDownUsesBaseMultiplier()
        {
            var result = PullMath.GetExitVelocity(Vector2.down, 30f, 0.667f, 0.75f);
            Assert.That(result.x, Is.EqualTo(0f).Within(0.001f));
            Assert.That(result.y, Is.EqualTo(-20.01f).Within(0.01f));
        }

        [Test]
        public void ExitVelocityUpAppliesUpMultiplier()
        {
            var result = PullMath.GetExitVelocity(Vector2.up, 30f, 0.667f, 0.75f);
            Assert.That(result.y, Is.EqualTo(15.0075f).Within(0.01f));
        }

        [Test]
        public void FasterIncomingVelocityIsKeptAlongPullDirection()
        {
            var speed = PullMath.GetPullSpeed(30f, 1f, new Vector2(40f, 10f), Vector2.right, true);
            Assert.That(speed, Is.EqualTo(40f));
        }

        [Test]
        public void IncomingVelocityAgainstPullDoesNotIncreaseSpeed()
        {
            var speed = PullMath.GetPullSpeed(30f, 1f, Vector2.left * 40f, Vector2.right, true);
            Assert.That(speed, Is.EqualTo(30f));
        }

        [Test]
        public void TangentProjectionRemovesNormalComponent()
        {
            var result = PullMath.ProjectOntoTangent(new Vector2(3f, 4f), Vector2.up);
            Assert.That(result.x, Is.EqualTo(3f));
            Assert.That(result.y, Is.EqualTo(0f));
        }
    }
}
