using Game.Editor.Boss;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class BossFactoryTests
    {
        [Test]
        public void EnsureVisualChildMigratesRootRendererOnlyOnce()
        {
            var root = new GameObject("LegacyBoss");
            try
            {
                var legacyRenderer = root.AddComponent<SpriteRenderer>();
                legacyRenderer.color = Color.cyan;

                Transform first = BossFactory.EnsureVisualChild(root);
                Transform second = BossFactory.EnsureVisualChild(root);

                Assert.AreSame(first, second);
                Assert.AreEqual("Visual", first.name);
                Assert.AreEqual(1, root.transform.childCount);
                Assert.IsNull(root.GetComponent<SpriteRenderer>());
                Assert.AreEqual(Color.cyan, first.GetComponent<SpriteRenderer>().color);
                Assert.AreEqual(1, first.GetComponents<SpriteRenderer>().Length);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
