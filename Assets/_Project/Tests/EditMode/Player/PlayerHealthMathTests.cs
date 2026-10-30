using Game.Player;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class PlayerHealthMathTests
    {
        [TestCase(2f, 8f, 0f)]
        [TestCase(9f, 2f, 7f)]
        public void DamageAndClampStayWithinHealthBounds(float current, float damage, float expected)
        {
            Assert.That(PlayerHealthMath.ClampHealth(PlayerHealthMath.ApplyDamage(current, damage), 7f), Is.EqualTo(expected));
            Assert.That(PlayerHealthMath.ClampHealth(10f, 7f), Is.EqualTo(7f));
        }

        [Test]
        public void KnockbackPointsAwayFromSourceAndOpposesFacingWhenDirectionIsZero()
        {
            Assert.That(PlayerHealthMath.KnockbackDirection(1f, 1).x, Is.GreaterThan(0f), "kaynak solda olduğunda oyuncu sağa itilir");
            Assert.That(PlayerHealthMath.KnockbackDirection(0f, 1).x, Is.LessThan(0f), "sağa bakarken sıfır yön sola iter");
            Assert.That(PlayerHealthMath.KnockbackDirection(0f, -1).x, Is.GreaterThan(0f), "sola bakarken sıfır yön sağa iter");
        }

        [Test]
        public void InvulnerabilityUsesExclusiveEndTime()
        {
            Assert.That(PlayerHealthMath.IsInvulnerable(1.5f, 2f), Is.True);
            Assert.That(PlayerHealthMath.IsInvulnerable(2f, 2f), Is.False);
        }
    }
}
