using UnityEngine;
namespace Game.Enemies
{
    /// <summary>Kalkanın saldırganı önden kapsayıp kapsamadığını hesaplar.</summary>
    public static class ShieldMath
    {
        public static bool IsBlocked(Vector2 enemyFacing, Vector2 enemyPos, Vector2 attackerPos, float arcDegrees)
        {
            return EnemyMath.IsBlocked(enemyFacing, enemyPos, attackerPos, arcDegrees);
        }
    }
}
