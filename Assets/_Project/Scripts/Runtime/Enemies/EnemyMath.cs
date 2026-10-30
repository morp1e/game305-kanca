using UnityEngine;
namespace Game.Enemies
{
    /// <summary>Düşmanların saf, test edilebilir karar hesapları.</summary>
    public static class EnemyMath
    {
        public static bool ShouldTurnAtEdge(bool hasGroundAhead) => !hasGroundAhead;
        public static bool ShouldTurnAtWall(bool wallAhead) => wallAhead;
        public static bool IsBlocked(Vector2 facing, Vector2 enemyPosition, Vector2 attackerPosition, float arcDegrees)
        {
            if (arcDegrees <= 0f) return false;
            Vector2 toAttacker = attackerPosition - enemyPosition;
            if (toAttacker.sqrMagnitude <= Mathf.Epsilon) return true;
            return Vector2.Angle(facing.normalized, toAttacker.normalized) <= arcDegrees * .5f;
        }
        public static Vector2 DeflectDirection(Vector2 hitDirection) => hitDirection.sqrMagnitude > 0f ? hitDirection.normalized : Vector2.right;
        public static float DeflectSpeed(float speed, float multiplier) => Mathf.Max(0f, speed) * Mathf.Max(0f, multiplier);
        public static bool CanTransition(EnemyState state, EnemyState next) => !(state == EnemyState.Telegraph && next == EnemyState.Hurt);
    }
}
