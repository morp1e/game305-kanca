using System;
using System.Collections.Generic;

namespace Game.Boss
{
    /// <summary>Saldırı listesini menzil ve arka arkaya kullanım sınırına göre seçer.</summary>
    public static class BossAttackSelector
    {
        public static BossAttackEntry Select(IList<BossAttackEntry> entries, float range, string previousId, int previousCount, Random random)
        {
            var valid = new List<BossAttackEntry>();
            float total = 0f;
            for (int i = 0; i < entries.Count; i++)
            {
                BossAttackEntry entry = entries[i];
                if (entry.attack == null || entry.weight <= 0f || range < entry.minRange || range > entry.maxRange) continue;
                if (entry.attack.id == previousId && previousCount >= 2) continue;
                valid.Add(entry);
                total += entry.weight;
            }
            if (valid.Count == 0) return default;
            double roll = random.NextDouble() * total;
            for (int i = 0; i < valid.Count; i++)
            {
                roll -= valid[i].weight;
                if (roll <= 0d) return valid[i];
            }
            return valid[valid.Count - 1];
        }
    }
}
