using UnityEngine;

namespace Game.Hook
{
    /// <summary>Kanca çekişi ve çıkışına ait saf vektör hesapları.</summary>
    public static class PullMath
    {
        public static float GetPullSpeed(float pullSpeed, float curveValue, Vector2 incomingVelocity,
            Vector2 direction, bool keepFasterIncomingSpeed)
        {
            var speed = pullSpeed * curveValue;
            if (!keepFasterIncomingSpeed) return speed;
            var incoming = Vector2.Dot(incomingVelocity, direction.normalized);
            return Mathf.Max(speed, incoming);
        }

        public static Vector2 GetExitVelocity(Vector2 direction, float pullSpeed, float exitSpeedMult, float exitUpMult)
        {
            var velocity = direction.normalized * pullSpeed * exitSpeedMult;
            if (direction.y > 0f) velocity.y *= exitUpMult;
            return velocity;
        }

        public static Vector2 ProjectOntoTangent(Vector2 velocity, Vector2 surfaceNormal)
        {
            var normal = surfaceNormal.normalized;
            return velocity - Vector2.Dot(velocity, normal) * normal;
        }
    }
}
