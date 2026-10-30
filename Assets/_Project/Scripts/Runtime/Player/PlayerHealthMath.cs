using UnityEngine;

namespace Game.Player
{
    public static class PlayerHealthMath
    {
        public static float ApplyDamage(float current, float damage) => Mathf.Max(0f, current - Mathf.Max(0f, damage));
        public static float ClampHealth(float current, float max) => Mathf.Clamp(current, 0f, Mathf.Max(0f, max));
        public static bool IsInvulnerable(float now, float until) => now < until;

        public static Vector2 KnockbackDirection(float sourceDirectionX, int facing)
        {
            float horizontal = Mathf.Abs(sourceDirectionX) > Mathf.Epsilon ? Mathf.Sign(sourceDirectionX) : -(facing == 0 ? 1 : Mathf.Sign(facing));
            return new Vector2(horizontal, 1f).normalized;
        }
    }
}
