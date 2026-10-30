using Game.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class SwordAttackMathTests
    {
        [Test]
        public void PhaseFromTime_UsesWindupActiveAndRecovery()
        {
            Assert.AreEqual(0, SwordAttack.PhaseFromTime(0.02f, 0.05f, 0.14f, 0.06f));
            Assert.AreEqual(1, SwordAttack.PhaseFromTime(0.1f, 0.05f, 0.14f, 0.06f));
            Assert.AreEqual(2, SwordAttack.PhaseFromTime(0.22f, 0.05f, 0.14f, 0.06f));
            Assert.AreEqual(3, SwordAttack.PhaseFromTime(0.25f, 0.05f, 0.14f, 0.06f));
        }

        [Test]
        public void SwingAngle_SmoothlySweepsInEitherDirection()
        {
            Assert.AreEqual(50f, SwordAttack.SwingAngle(0f, 100f, 0f, 1), 0.001f);
            Assert.AreEqual(0f, SwordAttack.SwingAngle(0f, 100f, 0.5f, 1), 0.001f);
            Assert.AreEqual(-50f, SwordAttack.SwingAngle(0f, 100f, 1f, 1), 0.001f);
            Assert.AreEqual(-50f, SwordAttack.SwingAngle(0f, 100f, 0f, -1), 0.001f);
            Assert.AreEqual(50f, SwordAttack.SwingAngle(0f, 100f, 1f, -1), 0.001f);
        }

        [Test]
        public void BladeEndpoints_StartAtPivotAndReachTip()
        {
            SwordAttack.BladeEndpoints(new Vector2(2f, 3f), 90f, 1.5f, out Vector2 start, out Vector2 tip);
            Assert.AreEqual(new Vector2(2f, 3f), start);
            Assert.That(tip.x, Is.EqualTo(2f).Within(0.001f));
            Assert.That(tip.y, Is.EqualTo(4.5f).Within(0.001f));
        }

        [Test]
        public void SweepSampleAngles_CoversIntermediateArcAngles()
        {
            var angles = SwordAttack.SweepSampleAngles(0f, 100f, 0f, 1f, 1, 5f);
            Assert.That(angles.Count, Is.GreaterThanOrEqualTo(21));
            Assert.AreEqual(50f, angles[0], 0.001f);
            Assert.AreEqual(-50f, angles[angles.Count - 1], 0.001f);
        }

        [Test]
        public void ComboDirection_UsesAimThenAlternatesWithinCombo()
        {
            Assert.AreEqual(1, SwordAttack.ComboDirection(2f, false, -1));
            Assert.AreEqual(-1, SwordAttack.ComboDirection(-2f, false, 1));
            Assert.AreEqual(-1, SwordAttack.ComboDirection(2f, true, 1));
        }

        [Test]
        public void StartInterval_IsMeasuredFromPreviousStart()
        {
            Assert.IsFalse(SwordAttack.CanStartSwing(1.39f, 1f, 0.4f));
            Assert.IsTrue(SwordAttack.CanStartSwing(1.4f, 1f, 0.4f));
        }
    }
}
