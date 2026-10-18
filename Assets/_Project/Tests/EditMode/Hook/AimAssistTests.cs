using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Game.Hook;

namespace Game.Tests.EditMode.Hook
{
    public class AimAssistTests
    {
        [Test]
        public void ChoosesCandidateNearestPointerCenter()
        {
            var candidates = new List<AimAssist.Candidate>
            {
                new AimAssist.Candidate(1, new Vector2(30f, 0f), true, 2f),
                new AimAssist.Candidate(2, new Vector2(5f, 0f), true, 8f),
            };
            Assert.That(AimAssist.Select(candidates, Vector2.zero, 64f, 10f, 0.15f, -1), Is.EqualTo(2));
        }

        [Test]
        public void CandidateWithoutLineOfSightIsRejected()
        {
            var candidates = new List<AimAssist.Candidate>
            {
                new AimAssist.Candidate(1, Vector2.zero, false, 1f),
            };
            Assert.That(AimAssist.Select(candidates, Vector2.zero, 64f, 10f, 0.15f, -1), Is.EqualTo(-1));
        }

        [Test]
        public void HysteresisKeepsCurrentTargetForSmallScoreChange()
        {
            var candidates = new List<AimAssist.Candidate>
            {
                new AimAssist.Candidate(1, new Vector2(12f, 0f), true, 2f),
                new AimAssist.Candidate(2, new Vector2(10f, 0f), true, 2f),
            };
            Assert.That(AimAssist.Select(candidates, Vector2.zero, 64f, 10f, 0.15f, 1), Is.EqualTo(1));
        }

        [Test]
        public void ZeroScoreCandidateIsNotSelected()
        {
            var candidates = new List<AimAssist.Candidate>
            {
                new AimAssist.Candidate(1, new Vector2(100f, 0f), true, 1f),
            };
            Assert.That(AimAssist.Select(candidates, Vector2.zero, 64f, 10f, 0.15f, -1), Is.EqualTo(-1));
        }

        [Test]
        public void CandidateOutsideMaximumRangeIsRejected()
        {
            var candidate = new AimAssist.Candidate(1, Vector2.zero, true, 11f);
            Assert.That(AimAssist.Score(candidate, Vector2.zero, 64f, 10f), Is.EqualTo(0f));
        }
    }
}
