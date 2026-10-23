using Game.Editor.Sandbox;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public sealed class OzencSandboxSetupTests
    {
        [Test]
        public void TileSheetGrid_UsesTwentyByNineVisualRowMajorCells()
        {
            var rects = OzencSandboxSetup.GetGridRects(360, 162, 18);
            Assert.That(rects, Has.Length.EqualTo(180));
            Assert.That(rects[0], Is.EqualTo(new Rect(0, 144, 18, 18)));
            Assert.That(rects[19], Is.EqualTo(new Rect(342, 144, 18, 18)));
            Assert.That(rects[20], Is.EqualTo(new Rect(0, 126, 18, 18)));
            Assert.That(rects[179], Is.EqualTo(new Rect(342, 0, 18, 18)));
        }

        [Test]
        public void TransparencyCheck_SkipsOnlyFullyTransparentCells()
        {
            var pixels = new Color32[4];
            Assert.That(OzencSandboxSetup.IsCellOpaque(pixels, 2, new Rect(0, 0, 2, 2)), Is.False);
            pixels[2] = new Color32(255, 255, 255, 1);
            Assert.That(OzencSandboxSetup.IsCellOpaque(pixels, 2, new Rect(0, 0, 2, 2)), Is.True);
        }
    }
}
