using Game.Core;
using Unity.Cinemachine;
using UnityEngine;

namespace Game.CameraRig
{
    /// <summary>
    /// Trauma tabanlı kamera sarsıntısı (Eiserloh, GDC 2016). traumaEvent'e gelen değerler trauma'ya eklenir.
    /// SÖZLEŞME İSKELETİ: gövde Lane D tarafından yazılacak. Alan adları (tuning, traumaEvent) değişmez.
    /// Cinemachine kamerasının olduğu objeye eklenir.
    /// </summary>
    public class CameraShake : CinemachineExtension
    {
        [SerializeField] CameraTuning tuning;
        [SerializeField] FloatEvent traumaEvent;

        public float Trauma { get; private set; }

        static readonly Vector2 XSeed = new Vector2(13.17f, 71.31f);
        static readonly Vector2 YSeed = new Vector2(47.73f, 19.91f);
        static readonly Vector2 AngleSeed = new Vector2(83.29f, 37.61f);

        Vector2 kickDirection;
        float kickStartedAt;
        float kickDuration;

        protected override void OnEnable()
        {
            base.OnEnable();
            if (traumaEvent != null)
                traumaEvent.Register(AddTrauma);
        }

        void OnDisable()
        {
            if (traumaEvent != null)
                traumaEvent.Unregister(AddTrauma);
        }

        void Update()
        {
            if (tuning != null)
                Trauma = TraumaMath.Decay(Trauma, tuning.TraumaDecay, Time.deltaTime);
        }

        public void AddTrauma(float amount) => Trauma = Mathf.Clamp01(Trauma + Mathf.Max(0f, amount));

        public void Kick(Vector2 dir)
        {
            kickDirection = dir.sqrMagnitude > 0f ? dir.normalized : Vector2.zero;
            kickStartedAt = Time.time;
            kickDuration = tuning != null ? tuning.DirectionalKickDuration : 0f;
        }

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize || tuning == null)
                return;

            float now = Time.time;
            float frequency = tuning.NoiseFrequency;
            Vector2 noise = new Vector2(
                Noise(now * frequency, XSeed),
                Noise(now * frequency, YSeed));
            Vector2 offset = TraumaMath.Offset(noise, Trauma, tuning.ShakeExponent, tuning.MaxShakeOffset);

            float kickAge = now - kickStartedAt;
            if (kickDuration > 0f && kickAge >= 0f && kickAge < kickDuration)
            {
                float kickWeight = 1f - kickAge / kickDuration;
                offset += kickDirection * (tuning.DirectionalKickStrength * kickWeight);
            }

            if (tuning.SnapShakeToPixels)
                offset = new Vector2(SnapMath.Snap(offset.x, 1f / 16f), SnapMath.Snap(offset.y, 1f / 16f));

            state.PositionCorrection += new Vector3(offset.x, offset.y, 0f);
            float angle = Noise(now * frequency, AngleSeed) * TraumaMath.ShakeAmount(Trauma, tuning.ShakeExponent) * tuning.MaxShakeAngle;
            state.OrientationCorrection *= Quaternion.AngleAxis(angle, Vector3.forward);
        }

        static float Noise(float time, Vector2 seed) => (Mathf.PerlinNoise(seed.x + time, seed.y) - 0.5f) * 2f;
    }
}
