using UnityEngine;

namespace Game.CameraRig
{
    /// <summary>Izgara oturtma hesapları.</summary>
    public static class SnapMath
    {
        public static float Snap(float value, float gridSize) =>
            gridSize <= 0f ? value : Mathf.Round(value / gridSize) * gridSize;

        public static Vector3 Snap(Vector3 value, float gridSize) => new Vector3(
            Snap(value.x, gridSize), Snap(value.y, gridSize), Snap(value.z, gridSize));
    }
}
