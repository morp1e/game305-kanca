using Game.CameraRig;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.CameraRig
{
    public class CameraMathTests
    {
        [Test]
        public void TraumaDecaySubtractsRateTimesDelta()
        {
            Assert.That(TraumaMath.Decay(0.8f, 1f, 0.25f), Is.EqualTo(0.55f).Within(0.0001f));
        }

        [Test]
        public void TraumaDecayClampsAtZero()
        {
            Assert.That(TraumaMath.Decay(0.1f, 1f, 1f), Is.Zero);
        }

        [Test]
        public void ShakeAmountSquaresTrauma()
        {
            Assert.That(TraumaMath.ShakeAmount(0.5f, 2f), Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void ShakeAmountClampsTrauma()
        {
            Assert.That(TraumaMath.ShakeAmount(2f, 2f), Is.EqualTo(1f));
        }

        [Test]
        public void OffsetScalesProvidedNoise()
        {
            Assert.That(TraumaMath.Offset(new Vector2(1f, -1f), 1f, 2f, 0.5f), Is.EqualTo(new Vector2(0.5f, -0.5f)));
        }

        [Test]
        public void SnapRoundsPositiveValuesToNearestGridPoint()
        {
            Assert.That(SnapMath.Snap(0.11f, 0.0625f), Is.EqualTo(0.125f).Within(0.0001f));
        }

        [Test]
        public void SnapRoundsNegativeValuesToNearestGridPoint()
        {
            Assert.That(SnapMath.Snap(-0.11f, 0.0625f), Is.EqualTo(-0.125f).Within(0.0001f));
        }

        [Test]
        public void SnapVectorSnapsEachAxis()
        {
            Assert.That(SnapMath.Snap(new Vector3(0.11f, -0.11f, 0.02f), 0.0625f),
                Is.EqualTo(new Vector3(0.125f, -0.125f, 0f)));
        }

        [Test]
        public void ParallaxFactorZeroKeepsLayerInWorld()
        {
            Vector3 result = ParallaxMath.Position(Vector3.one, Vector3.zero, new Vector3(10f, 4f, 2f), Vector2.zero);
            Assert.That(result, Is.EqualTo(Vector3.one));
        }

        [Test]
        public void ParallaxFactorOneFollowsCameraDisplacement()
        {
            Vector3 result = ParallaxMath.Position(Vector3.one, Vector3.zero, new Vector3(10f, 4f, 2f), Vector2.one);
            Assert.That(result, Is.EqualTo(new Vector3(11f, 5f, 1f)));
        }

        [Test]
        public void ParallaxFactorsScaleAxesIndependently()
        {
            Vector3 result = ParallaxMath.Position(Vector3.zero, Vector3.zero, new Vector3(10f, 4f, 2f), new Vector2(0.5f, 0.25f));
            Assert.That(result, Is.EqualTo(new Vector3(5f, 1f, 0f)));
        }
    }
}
