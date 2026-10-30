using System;
using Game.Boss;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class BossDecisionTests
    {
        BossAttack first;
        BossAttack second;

        [SetUp]
        public void SetUp()
        {
            first = ScriptableObject.CreateInstance<BossAttack>();
            first.id = "charge";
            second = ScriptableObject.CreateInstance<BossAttack>();
            second.id = "slam";
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(first);
            UnityEngine.Object.DestroyImmediate(second);
        }

        [Test]
        public void WeightedSelection_IsRepeatableForSameSeedAndRespectsRange()
        {
            var entries = new[]
            {
                new BossAttackEntry { attack = first, weight = 1f, minRange = 0f, maxRange = 4f },
                new BossAttackEntry { attack = second, weight = 20f, minRange = 5f, maxRange = 10f }
            };
            var a = BossAttackSelector.Select(entries, 2f, null, 0, new System.Random(12));
            var b = BossAttackSelector.Select(entries, 2f, null, 0, new System.Random(12));
            Assert.AreEqual(first, a.attack);
            Assert.AreEqual(a.attack, b.attack);
        }

        [Test]
        public void WeightedSelection_ExcludesAttackAfterTwoConsecutiveSelections()
        {
            var entries = new[]
            {
                new BossAttackEntry { attack = first, weight = 1f, maxRange = 10f },
                new BossAttackEntry { attack = second, weight = 1f, maxRange = 10f }
            };
            var result = BossAttackSelector.Select(entries, 2f, first.id, 2, new System.Random(1));
            Assert.AreEqual(second, result.attack);
        }

        [Test]
        public void HitDirectionAndPhaseQueue_UseFacingAndStrictHealthThreshold()
        {
            Assert.IsTrue(BossCombatMath.IsFromFront(2f, 0f, 1));
            Assert.IsTrue(BossCombatMath.IsFromFront(-2f, 0f, -1));
            Assert.IsFalse(BossCombatMath.IsFromFront(-2f, 0f, 1));
            Assert.IsTrue(BossCombatMath.ShouldQueuePhase(9f, 10f, 0, 2));
            Assert.IsFalse(BossCombatMath.ShouldQueuePhase(10f, 10f, 0, 2));
            Assert.IsFalse(BossCombatMath.ShouldQueuePhase(1f, 0f, 1, 2));
        }

        [Test]
        public void Telegraph_UsesPhaseScaleAndMinimum()
        {
            Assert.AreEqual(0.64f, BossCombatMath.Telegraph(0.8f, 0.8f, 0.45f), 0.001f);
            Assert.AreEqual(0.45f, BossCombatMath.Telegraph(0.4f, 0.8f, 0.45f), 0.001f);
        }
    }
}
