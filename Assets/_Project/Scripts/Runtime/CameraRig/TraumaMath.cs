using UnityEngine;

namespace Game.CameraRig
{
    /// <summary>Saf trauma ve sarsıntı hesapları.</summary>
    public static class TraumaMath
    {
        public static float Decay(float trauma, float decayPerSecond, float deltaTime) =>
            Mathf.Clamp01(trauma - Mathf.Max(0f, decayPerSecond) * Mathf.Max(0f, deltaTime));

        public static float ShakeAmount(float trauma, float exponent) =>
            Mathf.Pow(Mathf.Clamp01(trauma), Mathf.Max(0f, exponent));

        public static Vector2 Offset(Vector2 noise, float trauma, float exponent, float maxOffset) =>
            noise * (ShakeAmount(trauma, exponent) * Mathf.Max(0f, maxOffset));
    }
}
