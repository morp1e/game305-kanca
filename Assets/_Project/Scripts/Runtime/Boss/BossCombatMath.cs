using UnityEngine;

namespace Game.Boss
{
    /// <summary>Boss vuruş yönü, telegraph süresi ve faz eşiği hesapları.</summary>
    public static class BossCombatMath
    {
        public static bool IsFromFront(float sourceX, float bossX, int facing) => (sourceX - bossX) * facing > 0f;
        public static float Telegraph(float duration, float scale, float minimum) => Mathf.Max(minimum, duration * scale);
        public static bool ShouldQueuePhase(float health, float threshold, int phaseIndex, int phaseCount)
        {
            return phaseIndex + 1 < phaseCount && health < threshold;
        }
    }
}
