using UnityEngine;
using Game.Hook;

namespace Game.Feedback
{
    /// <summary>Kanca geri bildiriminin Unity nesnesinden bağımsız hesapları.</summary>
    public static class HookFeedbackMath
    {
        public static bool ShouldEmitTrail(HookState state) =>
            state == HookState.Thrown || state == HookState.Returning;

        public static float PulseMultiplier(float time, float frequency, float maximumScale)
        {
            var phase = (Mathf.Sin(time * frequency * Mathf.PI * 2f) + 1f) * 0.5f;
            return Mathf.Lerp(1f, maximumScale, phase);
        }

        public static Texture2D CreateDashTexture(int width, int dashLength)
        {
            width = Mathf.Max(1, width);
            dashLength = Mathf.Clamp(dashLength, 1, width);
            var texture = new Texture2D(width, 1, TextureFormat.RGBA32, false)
            {
                name = "HookAimDash",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Repeat
            };
            var pixels = new Color[width];
            for (var x = 0; x < width; x++) pixels[x] = x < dashLength ? Color.white : Color.clear;
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        public static AnimationCurve TrailWidth => new AnimationCurve(
            new Keyframe(0f, 1f), new Keyframe(1f, 0f));
    }
}
