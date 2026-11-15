using System.Reflection;
using Game.Editor.Import;
using Game.Visual;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests
{
    public sealed class SpriteAnimationTests
    {
        GameObject host;
        SpriteAnimSet set;
        SpriteAnimPlayer player;
        Sprite[] frames;

        [SetUp]
        public void SetUp()
        {
            host = new GameObject("SpriteAnimationTest");
            var renderer = host.AddComponent<SpriteRenderer>();
            player = host.AddComponent<SpriteAnimPlayer>();
            SetPrivate(player, "spriteRenderer", renderer);
            set = ScriptableObject.CreateInstance<SpriteAnimSet>();
            frames = new Sprite[5];
            for (int i = 0; i < frames.Length; i++)
            {
                var texture = new Texture2D(1, 1);
                frames[i] = Sprite.Create(texture, new Rect(0, 0, 1, 1), Vector2.zero);
            }
            set.clips = new[] { new SpriteAnimClip { name = "Walk", frames = frames, fps = 10f, loop = true, eventFrame = -1 } };
            SetPrivate(player, "set", set);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            foreach (Sprite frame in frames)
            {
                if (frame != null)
                {
                    Texture2D texture = frame.texture;
                    Object.DestroyImmediate(frame);
                    Object.DestroyImmediate(texture);
                }
            }
            Object.DestroyImmediate(set);
        }

        [Test]
        public void Find_IsCaseInsensitiveAndReturnsNullWhenMissing()
        {
            Assert.AreSame(set.clips[0], set.Find("wAlK"));
            Assert.IsNull(set.Find("missing"));
        }

        [Test]
        public void DurationPlayback_SpreadsFiveFramesAcrossRequestedDuration()
        {
            player.Play("walk", 0.5f);
            player.Advance(0.099f);
            Assert.AreSame(frames[0], GetRenderer().sprite);
            player.Advance(0.002f);
            Assert.AreSame(frames[1], GetRenderer().sprite);
        }

        [TestCase(0f)]
        [TestCase(-0.5f)]
        public void NonPositiveDurationPlayback_UsesClipFrameRate(float duration)
        {
            player.Play("walk", duration);
            player.Advance(0.11f);
            Assert.AreSame(frames[1], GetRenderer().sprite);
        }

        [Test]
        public void LoopPlayback_ReturnsToFirstFrame()
        {
            player.Play("walk");
            player.Advance(0.41f);
            Assert.AreSame(frames[4], GetRenderer().sprite);
            player.Advance(0.1f);
            Assert.AreSame(frames[0], GetRenderer().sprite);
        }

        [Test]
        public void NonLoopPlayback_HoldsLastFrameAndRaisesFinished()
        {
            set.clips[0].loop = false;
            string finished = null;
            player.ClipFinished += name => finished = name;
            player.Play("walk");
            player.Advance(0.51f);
            Assert.AreSame(frames[4], GetRenderer().sprite);
            Assert.AreEqual("Walk", finished);
            player.Advance(0.25f);
            Assert.AreSame(frames[4], GetRenderer().sprite);
        }

        [Test]
        public void FrameEvent_FiresWhenAdvanceCrossesMarkedFrame()
        {
            set.clips[0].eventFrame = 2;
            int eventCount = 0;
            player.FrameEvent += name => eventCount++;
            player.Play("walk");
            player.Advance(0.31f);
            Assert.AreEqual(1, eventCount);
        }

        [Test]
        public void MissingSet_PlayDoesNotThrow()
        {
            Sprite placeholder = frames[0];
            GetRenderer().sprite = placeholder;
            SetPrivate(player, "set", null);
            Assert.DoesNotThrow(() => player.Play("walk"));
            Assert.DoesNotThrow(() => player.Play("walk", 0.5f));
            Assert.AreSame(placeholder, GetRenderer().sprite, "set yokken yer tutucu sprite korunmalı");
        }

        [Test]
        public void SetFlip_DoesNotChangeTransformScale()
        {
            host.transform.localScale = new Vector3(-2f, 3f, 1f);
            player.SetFlip(true);
            Assert.IsTrue(GetRenderer().flipX);
            Assert.AreEqual(new Vector3(-2f, 3f, 1f), host.transform.localScale);
        }

        [Test]
        public void NaturalComparer_SortsNumberedFramesNaturally()
        {
            Assert.Less(NaturalStringComparer.Instance.Compare("frame-2", "frame-10"), 0);
        }

        [Test]
        public void SidecarParser_ReadsGridAndUsesDefaultPivot()
        {
            Assert.IsTrue(SpriteSheetSidecar.TryParse("{\"ppu\":16,\"frameWidth\":32,\"frameHeight\":32,\"frames\":6}", out var sidecar));
            Assert.AreEqual(16, sidecar.ppu);
            Assert.AreEqual(32, sidecar.frameWidth);
            Assert.AreEqual(0.5f, sidecar.pivot[0]);
            Assert.AreEqual(0f, sidecar.pivot[1]);
        }

        [Test]
        public void SidecarParser_UsesPerFramePivotOverrides()
        {
            const string json = "{\"ppu\":24,\"frameWidth\":66,\"frameHeight\":48,\"frames\":12,\"pivot\":[0.5,0],\"framePivots\":[{\"frame\":5,\"pivot\":[0.5,0.14583333]}]}";
            Assert.IsTrue(SpriteSheetSidecar.TryParse(json, out SpriteSheetSidecar sidecar));
            Assert.AreEqual(new Vector2(0.5f, 0.14583333f), sidecar.PivotForFrame(5, 0.5f, 0f));
            Assert.AreEqual(new Vector2(0.5f, 0f), sidecar.PivotForFrame(4, 0.5f, 0f));
        }

        [TestCase(2640, 192, 4096)]
        [TestCase(2048, 1900, 2048)]
        [TestCase(8193, 512, 8192)]
        public void PixelImporter_ChoosesPowerOfTwoThatCoversTextureWithinLimit(int width, int height, int expected)
        {
            Assert.AreEqual(expected, PixelSpriteImporter.RequiredMaxTextureSize(width, height));
        }

        [TestCase(960, 144, 160, 144, 6, 5, 800, 0)]
        [TestCase(64, 64, 32, 32, 2, 2, 0, 0)]
        public void SpriteSheetGrid_CalculatesHorizontalAndMultiRowFrameRects(int textureWidth, int textureHeight,
            int frameWidth, int frameHeight, int columns, int frameIndex, int expectedX, int expectedY)
        {
            Assert.IsTrue(PixelSpriteImporter.SpriteSheetGrid.TryGetFrameRect(textureWidth, textureHeight,
                frameWidth, frameHeight, columns, frameIndex, out Rect rect));
            Assert.AreEqual(new Rect(expectedX, expectedY, frameWidth, frameHeight), rect);
        }

        [Test]
        public void SpriteSheetGrid_CalculatesColumnsFromTextureWidthWhenMissing()
        {
            Assert.AreEqual(6, PixelSpriteImporter.SpriteSheetGrid.CalculateColumns(960, 160, 0));
        }

        [Test]
        public void SpriteSheetGrid_SkipsFramesThatExtendPastTextureBounds()
        {
            Assert.IsFalse(PixelSpriteImporter.SpriteSheetGrid.TryGetFrameRect(8, 1, 1, 1, 8, 8, out _));
        }

        [Test]
        public void SidecarPath_PrefersPngSuffixAndFallsBackToLegacyName()
        {
            const string assetPath = "Assets/sheets/hero.png";
            Assert.AreEqual("Assets/sheets/hero.png.slice.json", SpriteSheetSidecar.ResolvePath(
                assetPath, path => path == "Assets/sheets/hero.png.slice.json"));
            Assert.AreEqual("Assets/sheets/hero.slice.json", SpriteSheetSidecar.ResolvePath(
                assetPath, path => path == "Assets/sheets/hero.slice.json"));
        }

        [Test]
        public void AnimSetJsonParser_ReadsOptionalInclusiveFrameRangeAndScale()
        {
            const string json = "[{\"set\":\"Player\",\"visualScale\":1.24,\"clips\":[{\"name\":\"fall\",\"source\":\"jump.png\",\"frames\":[3,4]}]}]";
            Assert.IsTrue(AnimSetJsonParser.TryParse(json, out AnimSetRow[] rows));
            Assert.AreEqual(1.24f, rows[0].visualScale, 0.001f);
            CollectionAssert.AreEqual(new[] { 3, 4 }, rows[0].clips[0].frames);
            Assert.IsTrue(AnimSetJsonParser.TryParse("[{\"set\":\"Old\",\"clips\":[{\"name\":\"idle\",\"source\":\"idle.png\"}]}]", out rows));
            Assert.IsNull(rows[0].clips[0].frames, "frames alanı bulunmayan eski girdiler değişmeden kalmalı");
            Assert.AreEqual(1f, rows[0].visualScale, 0.001f, "visualScale bulunmayan eski setler varsayılan ölçeği kullanmalı");
        }

        [Test]
        public void SelectFrames_ClipsAnOverlongRangeToAvailableFrames()
        {
            Sprite[] selected = SpriteAnimSetBuilder.SelectFrames(frames, new[] { 3, 8 });

            Assert.AreEqual(2, selected.Length);
            Assert.AreSame(frames[3], selected[0]);
            Assert.AreSame(frames[4], selected[1]);
        }

        SpriteRenderer GetRenderer() => host.GetComponent<SpriteRenderer>();

        static void SetPrivate(object target, string fieldName, object value)
        {
            typeof(SpriteAnimPlayer).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
