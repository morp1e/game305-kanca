using Game.Enemies;
using NUnit.Framework;
using UnityEngine;
namespace Game.Tests
{
    public class EnemyMathTests
    {
        [Test] public void ShieldBlocksFrontAndAllowsRearHits()
        {
            Assert.That(ShieldMath.IsBlocked(Vector2.right, Vector2.zero, Vector2.right, 120f), Is.True);
            Assert.That(ShieldMath.IsBlocked(Vector2.right, Vector2.zero, Vector2.left, 120f), Is.False);
        }
        [Test] public void WalkerTurnsWhenGroundIsMissingOrWallAhead()
        {
            Assert.That(EnemyMath.ShouldTurnAtEdge(false), Is.True);
            Assert.That(EnemyMath.ShouldTurnAtEdge(true), Is.False);
            Assert.That(EnemyMath.ShouldTurnAtWall(true), Is.True);
            Assert.That(EnemyMath.ShouldTurnAtWall(false), Is.False);
        }
        [Test] public void DeflectedProjectileUsesAimDirectionAndScaledSpeed()
        {
            Assert.That(EnemyMath.DeflectDirection(new Vector2(3f, 4f)), Is.EqualTo(new Vector2(.6f, .8f)));
            Assert.That(EnemyMath.DeflectSpeed(6f, 1.5f), Is.EqualTo(9f));
        }
        [Test] public void SwordDamageDoesNotInterruptTelegraphButHookTransitionCan()
        {
            Assert.That(EnemyMath.CanTransition(EnemyState.Telegraph, EnemyState.Hurt), Is.False);
            Assert.That(EnemyMath.CanTransition(EnemyState.Telegraph, EnemyState.Pulled), Is.True);
        }
    }
}
