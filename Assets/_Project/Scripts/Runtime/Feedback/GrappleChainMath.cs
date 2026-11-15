using UnityEngine;

namespace Game.Feedback
{
    /// <summary>Zincirin iki ucu arasındaki dünya konumunu hesaplar.</summary>
    public static class GrappleChainMath
    {
        public static void Compute(Vector2 start, Vector2 end, out Vector2 center, out float angleDeg, out float length)
        {
            var delta = end - start;
            center = (start + end) * 0.5f;
            angleDeg = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            length = delta.magnitude;
        }

        /// <summary>Sprite pivotinin rendered rect merkezine göre konumunu hesaplar.</summary>
        public static Vector2 ComputePivotOffset(Vector2 pivotPixels, Vector2 rectSizePixels, Vector2 renderedSize)
        {
            if (rectSizePixels.x <= 0f || rectSizePixels.y <= 0f) return Vector2.zero;

            var pivotFraction = new Vector2(
                pivotPixels.x / rectSizePixels.x,
                pivotPixels.y / rectSizePixels.y);
            return Vector2.Scale(pivotFraction - new Vector2(0.5f, 0.5f), renderedSize);
        }
    }
}
