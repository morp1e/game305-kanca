using Game.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class SwordAttackMathTests
    {
        [Test]
        public void ComboSequenceAdvancesThroughThreeStepsThenStartsOver()
        {
            var steps = new[] { new ComboStep(), new ComboStep(), new ComboStep() };
            var combo = new ComboSequence();
            Assert.AreEqual(0, combo.Start(0f, steps, 0.3f, 0.45f));
            Assert.AreEqual(1, combo.Start(0.3f, steps, 0.3f, 0.45f));
            Assert.AreEqual(2, combo.Start(0.6f, steps, 0.3f, 0.45f));
            Assert.AreEqual(-1, combo.Start(1.04f, steps, 0.3f, 0.45f));
            Assert.AreEqual(0, combo.Start(1.05f, steps, 0.3f, 0.45f));
        }

        [Test]
        public void FinisherWindowDelaysTheNextComboStart()
        {
            var combo = new ComboSequence();
            var steps = new[] { new ComboStep(), new ComboStep(), new ComboStep() };
            Assert.AreEqual(0, combo.Start(0f, steps, 0.3f, 0.45f));
            Assert.AreEqual(1, combo.Start(0.3f, steps, 0.3f, 0.45f));
            Assert.AreEqual(2, combo.Start(0.6f, steps, 0.3f, 0.45f));
            Assert.IsFalse(combo.CanStart(1.04f, 0.3f, 0.45f));
            Assert.IsTrue(combo.CanStart(1.05f, 0.3f, 0.45f));
        }

        [Test]
        public void MissedComboWindowResetsToFirstStep()
        {
            var steps = new[] { new ComboStep(), new ComboStep(), new ComboStep() };
            var combo = new ComboSequence();
            Assert.AreEqual(0, combo.Start(1f, steps, 0.3f, 0.45f));
            Assert.AreEqual(1, combo.Start(1.3f, steps, 0.3f, 0.45f));
            Assert.AreEqual(0, combo.Start(1.76f, steps, 0.3f, 0.45f));
        }

        [Test]
        public void ComboPhasesUseConfiguredStepDurations()
        {
            var tuning = ScriptableObject.CreateInstance<SwordTuning>();
            try
            {
                Assert.AreEqual(3, tuning.comboSteps.Length);
                var first = tuning.comboSteps[0];
                for (int i = 0; i < tuning.comboSteps.Length; i++)
                {
                    var step = tuning.comboSteps[i];
                    float windup = Mathf.Max(0f, step.windup);
                    float active = Mathf.Max(0.001f, step.active);
                    float recovery = Mathf.Max(0f, step.recovery);
                    Assert.AreEqual(0, ComboSequence.Phase(windup - 0.001f, step), $"adım {i + 1} hazırlık sınırı");
                    Assert.AreEqual(1, ComboSequence.Phase(windup, step), $"adım {i + 1} aktif başlangıcı");
                    Assert.AreEqual(2, ComboSequence.Phase(windup + active, step), $"adım {i + 1} toparlanma başlangıcı");
                    Assert.AreEqual(3, ComboSequence.Phase(windup + active + recovery, step), $"adım {i + 1} bitişi");
                }
                var finisher = tuning.comboSteps[2];
                Assert.That(finisher.windup, Is.LessThanOrEqualTo(0.05f));
                Assert.That(finisher.arcDegrees, Is.GreaterThan(first.arcDegrees));
                Assert.That(finisher.damage, Is.GreaterThan(first.damage));
                Assert.That(finisher.hitstop, Is.GreaterThan(first.hitstop));
            }
            finally { Object.DestroyImmediate(tuning); }
        }

        [Test]
        public void SwingAngleSmoothlySweepsInEitherDirection()
        {
            Assert.AreEqual(50f, SwordAttack.SwingAngle(0f, 100f, 0f, 1), 0.001f);
            Assert.AreEqual(0f, SwordAttack.SwingAngle(0f, 100f, 0.5f, 1), 0.001f);
            Assert.AreEqual(-50f, SwordAttack.SwingAngle(0f, 100f, 1f, 1), 0.001f);
            Assert.AreEqual(-50f, SwordAttack.SwingAngle(0f, 100f, 0f, -1), 0.001f);
            Assert.AreEqual(50f, SwordAttack.SwingAngle(0f, 100f, 1f, -1), 0.001f);
        }

        [Test]
        public void BladeEndpointsStartAtPivotAndReachTip()
        {
            SwordAttack.BladeEndpoints(new Vector2(2f, 3f), 90f, 1.5f, out Vector2 start, out Vector2 tip);
            Assert.AreEqual(new Vector2(2f, 3f), start);
            Assert.That(tip.x, Is.EqualTo(2f).Within(0.001f));
            Assert.That(tip.y, Is.EqualTo(4.5f).Within(0.001f));
        }

        [Test]
        public void SweepSamplesCoverTheIntermediateArc()
        {
            var angles = SwordAttack.SweepSampleAngles(0f, 100f, 0f, 1f, 1, 5f);
            Assert.That(angles.Count, Is.GreaterThanOrEqualTo(21));
            Assert.AreEqual(50f, angles[0], 0.001f);
            Assert.AreEqual(-50f, angles[angles.Count - 1], 0.001f);
        }
    }
}
