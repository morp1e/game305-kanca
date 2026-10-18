using System;
using System.Collections.Generic;

namespace Game.Dialogue
{
    /// <summary>Cooldown ve tekrar bilgisini bir konuşan kılıç için tutar.</summary>
    public class LineSelector
    {
        readonly Dictionary<SwordCue, float> cueReadyAt = new Dictionary<SwordCue, float>();
        readonly Func<float> randomValue;
        float globalReadyAt;
        string previousLine;

        public LineSelector(Func<float> randomValue = null)
        {
            this.randomValue = randomValue ?? (() => (float)UnityEngine.Random.value);
        }

        public string Select(SwordLineSet set, SwordCue cue, float time)
        {
            if (set == null || time < globalReadyAt) return null;
            var entry = set.GetCue(cue);
            if (entry == null || entry.lines == null || entry.lines.Length == 0) return null;
            if (cueReadyAt.TryGetValue(cue, out var readyAt) && time < readyAt) return null;
            if (randomValue() >= UnityEngine.Mathf.Clamp01(entry.chance)) return null;

            var available = new List<string>(entry.lines.Length);
            for (var i = 0; i < entry.lines.Length; i++)
            {
                var line = entry.lines[i];
                if (string.IsNullOrWhiteSpace(line) || (entry.lines.Length > 1 && line == previousLine)) continue;
                available.Add(line);
            }
            if (available.Count == 0) return null;

            var index = (int)(UnityEngine.Mathf.Clamp01(randomValue()) * available.Count);
            if (index >= available.Count) index = available.Count - 1;
            var selected = available[index];
            previousLine = selected;
            globalReadyAt = time + Math.Max(0f, set.globalCooldown);
            cueReadyAt[cue] = time + Math.Max(0f, entry.cooldown);
            return selected;
        }
    }
}
