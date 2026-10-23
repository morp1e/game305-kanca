using NUnit.Framework;
using Game.Player;

namespace Game.Tests
{
    public class MovementMathTests
    {
        [Test]
        public void ApproachStopsAtTarget()
        {
            Assert.AreEqual(4f, MovementMath.Approach(3f, 4f, 2f));
        }

        [Test]
        public void OverspeedSameDirectionUsesRunReduce()
        {
            float next = MovementMath.StepHorizontal(20f, 1f, true, 0.02f, 11f, 100f, 40f, 0.65f, 10f);
            Assert.AreEqual(19.2f, next, 0.0001f);
        }

        [Test]
        public void OverspeedIsNotClampedOnFirstStep()
        {
            float next = MovementMath.StepHorizontal(18f, 1f, true, 0.02f, 11f, 100f, 40f, 0.65f, 10f);
            Assert.That(next, Is.GreaterThan(11f));
        }

        [Test]
        public void OppositeInputUsesAcceleration()
        {
            float next = MovementMath.StepHorizontal(18f, -1f, true, 0.02f, 11f, 100f, 40f, 0.65f, 10f);
            Assert.AreEqual(16f, next, 0.0001f);
        }

        [Test]
        public void NoInputReducesOverspeed()
        {
            float next = MovementMath.StepHorizontal(18f, 0f, true, 0.02f, 11f, 100f, 40f, 0.65f, 10f);
            Assert.AreEqual(16f, next, 0.0001f);
        }

        [Test]
        public void HookLaunchNoInputKeepsSlowRunReduceDecay()
        {
            float next = MovementMath.StepHorizontal(18f, 0f, true, 0.02f, 11f, 100f, 40f, 0.65f, 10f, true);
            Assert.AreEqual(17.2f, next, 0.0001f);
        }

        [Test]
        public void NoInputStopsAtMaxSpeedWithinOneTenthSecond()
        {
            float velocity = 11f;
            for (int i = 0; i < 5; i++)
                velocity = MovementMath.StepHorizontal(velocity, 0f, true, 0.02f, 11f, 125f, 50f, 0.65f, 20f);

            Assert.AreEqual(0f, velocity, 0.0001f);
        }

        [Test]
        public void TurningAtMaxSpeedReversesWithinTwoTenthsSecond()
        {
            float velocity = 11f;
            for (int i = 0; i < 9; i++)
                velocity = MovementMath.StepHorizontal(velocity, -1f, true, 0.02f, 11f, 125f, 50f, 0.65f, 20f);

            Assert.That(velocity, Is.LessThan(0f));
        }

        [Test]
        public void HalfGravityAppliesNearApexWhileJumpHeld()
        {
            float next = MovementMath.StepGravity(4f, 0.02f, true, 100f, 5f, 20f);
            Assert.AreEqual(3f, next, 0.0001f);
        }

        [Test]
        public void GravityClampsMaximumFallSpeed()
        {
            float next = MovementMath.StepGravity(-19f, 0.1f, false, 100f, 5f, 20f);
            Assert.AreEqual(-20f, next, 0.0001f);
        }

        [Test]
        public void ReleasingJumpStopsVarJumpTimerWithoutCuttingVelocity()
        {
            Assert.AreEqual(8f, MovementMath.ApplyVariableJump(8f, false, 13f, 0.1f));
            Assert.AreEqual(13f, MovementMath.ApplyVariableJump(8f, true, 13f, 0.1f));
        }

        [Test]
        public void FastFallCapApproachesCelesteFastMaximum()
        {
            float cap = MovementMath.Approach(20f, 30f, 37.5f * 0.2f);
            Assert.AreEqual(27.5f, cap, 0.0001f);
            Assert.AreEqual(-27.5f, MovementMath.StepGravity(-27f, 1f, false, 112.5f, 5f, cap), 0.0001f);
        }

        [Test]
        public void WallSlideSpeedLerpsFromWallStartMaximumToMaxFall()
        {
            Assert.AreEqual(2.5f, MovementMath.WallSlideCap(20f, 2.5f, 1.2f, 1.2f), 0.0001f);
            Assert.AreEqual(11.25f, MovementMath.WallSlideCap(20f, 2.5f, 0.6f, 1.2f), 0.0001f);
        }

        [Test]
        public void LaunchJumpAddsVerticalBonusAndKeepsHorizontalMomentum()
        {
            var velocity = MovementMath.ApplyJump(new UnityEngine.Vector2(18f, -2f), true, 13f, 2f, 5f, -1f);
            Assert.AreEqual(18f, velocity.x);
            Assert.AreEqual(15f, velocity.y);
        }

        [Test]
        public void RunningJumpAddsHorizontalBoost()
        {
            var velocity = MovementMath.ApplyJump(new UnityEngine.Vector2(3f, 0f), false, 13f, 2f, 5f, -1f);
            Assert.AreEqual(-2f, velocity.x);
            Assert.AreEqual(13f, velocity.y);
        }

        [Test]
        public void CoyoteJumpWorksWithinGraceTime()
        {
            var gate = new JumpGate();
            gate.Tick(0.01f, true, false, 0.1f, 0.1f);
            gate.Tick(0.05f, false, true, 0.1f, 0.1f);
            Assert.IsTrue(gate.TryConsumeJump(out bool launched));
            Assert.IsFalse(launched);
        }

        [Test]
        public void CoyoteJumpExpiresAfterGraceTime()
        {
            var gate = new JumpGate();
            gate.Tick(0.01f, true, false, 0.1f, 0.1f);
            gate.Tick(0.11f, false, true, 0.1f, 0.1f);
            Assert.IsFalse(gate.TryConsumeJump(out _));
        }

        [Test]
        public void BufferedJumpSurvivesUntilLanding()
        {
            var gate = new JumpGate();
            gate.Tick(0.01f, false, true, 0.1f, 0.1f);
            Assert.IsFalse(gate.TryConsumeJump(out _));
            gate.Tick(0.04f, true, false, 0.1f, 0.1f);
            Assert.IsTrue(gate.TryConsumeJump(out _));
        }

        [Test]
        public void LaunchJumpAddsBonusAndConsumesWindow()
        {
            var gate = new JumpGate();
            gate.OpenLaunchWindow(0.1f);
            gate.Tick(0.01f, false, true, 0.1f, 0.1f);
            Assert.IsTrue(gate.TryConsumeJump(out bool launched));
            Assert.IsTrue(launched);
            gate.Tick(0.01f, false, true, 0.1f, 0.1f);
            Assert.IsFalse(gate.TryConsumeJump(out _));
        }
    }
}
