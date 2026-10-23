using Game.Combat;
using NUnit.Framework;

namespace Game.Tests
{
    public class SwordAttackMathTests
    {
        [Test]
        public void SwingAngle_SweepsAimPlusToAimMinus()
        {
            Assert.AreEqual(50f, SwordAttack.SwingAngle(0f, 100f, 0f, 1), 0.001f);
            Assert.AreEqual(0f, SwordAttack.SwingAngle(0f, 100f, 0.5f, 1), 0.001f);
            Assert.AreEqual(-50f, SwordAttack.SwingAngle(0f, 100f, 1f, 1), 0.001f);
        }

        [Test]
        public void SwingAngle_MirrorsSweepWhenFacingLeft()
        {
            Assert.AreEqual(-50f, SwordAttack.SwingAngle(0f, 100f, 0f, -1), 0.001f);
            Assert.AreEqual(50f, SwordAttack.SwingAngle(0f, 100f, 1f, -1), 0.001f);
        }
    }
}
