using UnityEngine;

namespace Game.CameraRig
{
    /// <summary>Başlangıç pozuna göre parallax konumu.</summary>
    public static class ParallaxMath
    {
        public static Vector3 Position(Vector3 layerStart, Vector3 cameraStart, Vector3 cameraPosition, Vector2 factor)
        {
            Vector3 movement = cameraPosition - cameraStart;
            return layerStart + new Vector3(movement.x * factor.x, movement.y * factor.y, 0f);
        }
    }
}
