using System.Reflection;
using Game.Combat;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class SwordTargetingTests
    {
        GameObject target;
        GameObject wall;
        Sprite sprite;

        [TearDown]
        public void Cleanup()
        {
            if (target != null) Object.DestroyImmediate(target);
            if (wall != null) Object.DestroyImmediate(wall);
            if (sprite != null) Object.DestroyImmediate(sprite);
        }

        [Test]
        public void AimAssistTurnsTowardClosestDamageableInsideCone()
        {
            target = MakeTarget(new Vector2(2f, 0.15f));
            Physics2D.SyncTransforms();
            Vector2 result = SwordAttack.ApplyAimAssist(Vector2.zero, Vector2.right, 12f, 2.5f);
            Assert.That(Vector2.Angle(Vector2.right, result), Is.GreaterThan(0f).And.LessThan(12f));
        }

        [Test]
        public void AimAssistIgnoresTargetsOutsideCone()
        {
            target = MakeTarget(new Vector2(1.7f, 1f));
            Physics2D.SyncTransforms();
            Vector2 result = SwordAttack.ApplyAimAssist(Vector2.zero, Vector2.right, 12f, 2.5f);
            Assert.That(Vector2.Angle(Vector2.right, result), Is.LessThan(0.001f));
        }

        [Test]
        public void AimAssistDoesNotTurnTowardDamageableBehindGroundWall()
        {
            target = MakeTarget(new Vector2(2f, 0.15f));
            Physics2D.SyncTransforms();
            Vector2 clearResult = SwordAttack.ApplyAimAssist(Vector2.zero, Vector2.right, 12f, 2.5f,
                LayerMask.GetMask("Enemy"), LayerMask.GetMask("Ground"));
            Assert.That(Vector2.Angle(Vector2.right, clearResult), Is.GreaterThan(0f).And.LessThan(12f), "duvar yokken koni içindeki hedefe dönmeli");
            wall = new GameObject("AimOccluder") { layer = LayerMask.NameToLayer("Ground") };
            wall.transform.position = new Vector2(1f, 0.075f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.25f, 2f);
            Physics2D.SyncTransforms();
            Vector2 result = SwordAttack.ApplyAimAssist(Vector2.zero, Vector2.right, 12f, 2.5f,
                LayerMask.GetMask("Enemy"), LayerMask.GetMask("Ground"));
            Assert.That(Vector2.Angle(Vector2.right, result), Is.LessThan(0.001f));
        }

        [Test]
        public void ZeroAimAssistDegreesDisablesTargeting()
        {
            target = MakeTarget(new Vector2(2f, 0.15f));
            Physics2D.SyncTransforms();
            Vector2 result = SwordAttack.ApplyAimAssist(Vector2.zero, Vector2.right, 0f, 2.5f);
            Assert.AreEqual(Vector2.right, result);
        }

        [Test]
        public void SwordVisualUsesAssignedFramesAndMapsAsepriteTagNames()
        {
            var go = new GameObject("SwordVisualTest");
            var visual = go.AddComponent<SwordVisual>();
            sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            visual.SetStepSprites(1, new[] { sprite });
            Assert.IsTrue(visual.UsesSpriteStep(1));
            Assert.IsFalse(visual.UsesProceduralStep(1));
            Assert.IsTrue(visual.UsesProceduralStep(0));
            Assert.AreEqual(0, SwordVisual.StepFromTag("slash1"));
            Assert.AreEqual(1, SwordVisual.StepFromTag("slash2"));
            Assert.AreEqual(2, SwordVisual.StepFromTag("slash3"));
            Assert.AreEqual(2, SwordVisual.StepFromAnimationName("slash3"));
            Assert.AreEqual(-1, SwordVisual.StepFromTag("unknown"));
            Object.DestroyImmediate(go);
        }

        [Test]
        public void SwordHitSparksDoNotMoveTheSmearPivot()
        {
            var go = new GameObject("SwordVisualPivotTest");
            go.transform.position = new Vector2(1f, 2f);
            var visual = go.AddComponent<SwordVisual>();
            var sparkObject = new GameObject("HitSparks");
            sparkObject.transform.SetParent(go.transform, false);
            var spark = sparkObject.AddComponent<ParticleSystem>();
            typeof(SwordVisual).GetField("spark", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(visual, spark);

            typeof(SwordVisual).GetMethod("OnHit", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(visual, new object[] { new Vector2(4f, 5f), Vector2.right, false });

            Assert.That(go.transform.position, Is.EqualTo(new Vector3(1f, 2f, 0f)));
            Assert.That(spark.transform.position, Is.EqualTo(new Vector3(4f, 5f, 0f)));
            Object.DestroyImmediate(go);
        }

        GameObject MakeTarget(Vector2 position)
        {
            var go = new GameObject("DamageTarget") { layer = LayerMask.NameToLayer("Enemy") };
            go.transform.position = position;
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<TrainingDummy>();
            return target = go;
        }
    }
}
