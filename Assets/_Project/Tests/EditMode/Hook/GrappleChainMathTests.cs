using Game.Feedback;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode.Hook
{
    public class GrappleChainMathTests
    {
        [Test]
        public void Compute_ReturnsMidpointAndDistance()
        {
            GrappleChainMath.Compute(new Vector2(2f, 4f), new Vector2(8f, 12f), out var center, out _, out var length);

            Assert.That(center.x, Is.EqualTo(5f).Within(0.001f));
            Assert.That(center.y, Is.EqualTo(8f).Within(0.001f));
            Assert.That(length, Is.EqualTo(10f).Within(0.001f));
        }

        [TestCase(1f, 0f, 0f)]
        [TestCase(0f, 1f, 90f)]
        [TestCase(-1f, 0f, 180f)]
        [TestCase(1f, 1f, 45f)]
        [TestCase(0f, -1f, -90f)]
        [TestCase(-1f, -1f, -135f)]
        [TestCase(1f, -1f, -45f)]
        public void Compute_ReturnsAngleFromPositiveX(float x, float y, float expectedAngle)
        {
            GrappleChainMath.Compute(Vector2.zero, new Vector2(x, y), out _, out var angle, out _);

            Assert.That(angle, Is.EqualTo(expectedAngle).Within(0.001f));
        }

        [Test]
        public void ComputePivotOffset_AccountsForNonCenterPivot()
        {
            var offset = GrappleChainMath.ComputePivotOffset(
                new Vector2(75f, 10f),
                new Vector2(100f, 40f),
                new Vector2(8f, 6f));

            Assert.That(offset.x, Is.EqualTo(2f).Within(0.001f));
            Assert.That(offset.y, Is.EqualTo(-1.5f).Within(0.001f));
        }

        [Test]
        public void Compute_ZeroLengthReturnsZeroDistance()
        {
            GrappleChainMath.Compute(new Vector2(3f, -2f), new Vector2(3f, -2f), out var center, out var angle, out var length);

            Assert.That(center, Is.EqualTo(new Vector2(3f, -2f)));
            Assert.That(angle, Is.EqualTo(0f).Within(0.001f));
            Assert.That(length, Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Visual_CreatesPointFilteredTiledPlaceholderWithoutHook()
        {
            var gameObject = new GameObject("GrappleChainVisualTest");
            try
            {
                var visual = gameObject.AddComponent<GrappleChainVisual>();
                visual.EnsureRenderer();
                var renderer = gameObject.GetComponentInChildren<SpriteRenderer>();

                Assert.That(renderer, Is.Not.Null);
                Assert.That(renderer.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
                Assert.That(renderer.tileMode, Is.EqualTo(SpriteTileMode.Continuous));
                Assert.That(renderer.sprite, Is.Not.Null);
                Assert.That(renderer.sprite.texture.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(renderer.sprite.texture.wrapMode, Is.EqualTo(TextureWrapMode.Repeat));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }
    }
}
