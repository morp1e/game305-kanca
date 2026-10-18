using System.Collections.Generic;
using UnityEngine;

namespace Game.Hook
{
    /// <summary>Ekrandaki hedefleri skorlar ve hedef değişimini kararlı tutar.</summary>
    public static class AimAssist
    {
        public struct Candidate
        {
            public int Id { get; }
            public Vector2 ScreenPosition { get; }
            public bool HasLineOfSight { get; }
            public float Range { get; }

            public Candidate(int id, Vector2 screenPosition, bool hasLineOfSight, float range)
            {
                Id = id;
                ScreenPosition = screenPosition;
                HasLineOfSight = hasLineOfSight;
                Range = range;
            }
        }

        public static int Select(IReadOnlyList<Candidate> candidates, Vector2 pointerScreen,
            float snapRadiusPx, float maxRange, float switchMargin, int currentId)
        {
            var bestId = -1;
            var bestScore = 0f;
            var currentScore = 0f;
            for (var i = 0; i < candidates.Count; i++)
            {
                var candidate = candidates[i];
                var score = Score(candidate, pointerScreen, snapRadiusPx, maxRange);
                if (candidate.Id == currentId) currentScore = score;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestId = candidate.Id;
                }
            }

            if (bestId < 0) return -1;
            if (currentId >= 0 && currentScore > 0f && bestId != currentId && bestScore < currentScore + switchMargin)
                return currentId;
            return bestId;
        }

        public static float Score(Candidate candidate, Vector2 pointerScreen, float snapRadiusPx, float maxRange)
        {
            if (!candidate.HasLineOfSight || snapRadiusPx <= 0f || maxRange <= 0f || candidate.Range > maxRange)
                return 0f;

            var centeredness = 1f - Mathf.Clamp01(Vector2.Distance(pointerScreen, candidate.ScreenPosition) / snapRadiusPx);
            if (centeredness <= 0f) return 0f;
            var rangeTerm = 0.1f * (1f - Mathf.Clamp01(candidate.Range / maxRange));
            return centeredness + rangeTerm;
        }
    }
}
