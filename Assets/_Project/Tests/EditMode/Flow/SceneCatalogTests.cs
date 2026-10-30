using Game.Flow;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public class SceneCatalogTests
    {
        SceneCatalog catalog;

        [SetUp]
        public void Build()
        {
            catalog = ScriptableObject.CreateInstance<SceneCatalog>();
            catalog.levels = new[] { "A", "B" };
            catalog.endScene = "End";
        }

        [TearDown]
        public void Clean() => Object.DestroyImmediate(catalog);

        [Test]
        public void NextAfterLevel_ReturnsFollowingLevel() => Assert.That(catalog.NextAfter("A"), Is.EqualTo("B"));

        [Test]
        public void NextAfterLastLevel_ReturnsEndScene() => Assert.That(catalog.NextAfter("B"), Is.EqualTo("End"));

        [Test]
        public void NextAfterUnknownScene_ReturnsEmpty() => Assert.That(catalog.NextAfter("Title"), Is.Empty);

        [Test]
        public void IsLevel_OnlyForCatalogLevels()
        {
            Assert.That(catalog.IsLevel("A"), Is.True);
            Assert.That(catalog.IsLevel("End"), Is.False);
        }
    }
}
