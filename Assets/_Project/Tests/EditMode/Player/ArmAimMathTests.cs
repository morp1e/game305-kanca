using Game.Player;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class ArmAimMathTests
    {
        [Test] public void AngleUsesShoulderToTargetDirection() => Assert.AreEqual(90f, ArmAimMath.Angle(Vector2.zero, Vector2.up), .001f);

        [TestCase(0f, 37f, 37f)]
        [TestCase(22.5f, 45f, 46f)]
        [TestCase(45f, 90f, 69f)]
        public void SnapUsesConfiguredStep(float step, float expected, float angle) => Assert.AreEqual(expected, ArmAimMath.SnapAngle(angle, step), .001f);

        [Test]
        public void FacingKeepsPreviousSideInsideDeadZone()
        {
            Assert.AreEqual(-1, ArmAimMath.Facing(Vector2.zero, new Vector2(.1f, 8f), .2f, -1));
            Assert.AreEqual(1, ArmAimMath.Facing(Vector2.zero, new Vector2(.1f, 8f), .2f, 1));
            Assert.AreEqual(-1, ArmAimMath.Facing(Vector2.zero, new Vector2(-.3f, 0f), .2f, 1));
        }

        [Test] public void MirroringReflectsAngleAcrossVerticalAxis() => Assert.AreEqual(135f, ArmAimMath.MirrorAngle(45f), .001f);
    }
}
