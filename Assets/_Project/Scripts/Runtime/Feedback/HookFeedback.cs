using Game.Core;
using Game.Hook;
using UnityEngine;

namespace Game.Feedback
{
    /// <summary>Kancanın görsel geri bildirimi: nişan çizgisi, kılıç izi ve çekiş itkisi.</summary>
    public class HookFeedback : MonoBehaviour
    {
        [SerializeField] SwordHook hook;
        [SerializeField] Transform sword;
        [SerializeField] LineRenderer aimLine;
        [SerializeField] TrailRenderer trail;
        [SerializeField] Vector2Event kickEvent;
        [SerializeField, Tooltip("Nişan çizgisinin rengi. Ö")]
        Color aimColor = Color.yellow;
        [SerializeField, Tooltip("İz rengi. Ö")]
        Color trailColor = new Color(0.35f, 0.85f, 1f, 1f);
        [SerializeField, Tooltip("Kılıç izinin süresi, saniye. Ö")]
        float trailTime = 0.12f;
        [SerializeField, Tooltip("Nişan çizgisinin kalınlığı, 1/16 unit. Ö")]
        float aimWidth = 1f / 16f;
        [SerializeField, Tooltip("Anchor nabzının en büyük ölçeği. Ö")]
        float pulseScale = 1.15f;
        [SerializeField, Tooltip("Anchor nabzının frekansı, Hz. Ö")]
        float pulseFrequency = 6f;
        [SerializeField, Tooltip("Kesikli doku genişliği, piksel. Ö")]
        int dashTextureWidth = 8;
        [SerializeField, Tooltip("Kesikli doku içindeki çizgi uzunluğu, piksel. Ö")]
        int dashLength = 4;

        HookAnchor pulsingAnchor;
        Vector3 pulsingAnchorScale;
        Texture2D dashTexture;
        Material generatedMaterial;

        void Awake()
        {
            if (aimLine == null) aimLine = CreateAimLine();
            if (trail == null) trail = CreateTrail();
            ConfigureAimLine();
            ConfigureTrail();
            ApplyTrailState();
        }

        void OnEnable()
        {
            if (hook != null) hook.StateChanged += OnStateChanged;
            ApplyTrailState();
        }

        void OnDisable()
        {
            if (hook != null) hook.StateChanged -= OnStateChanged;
            RestorePulsingAnchor();
        }

        void OnDestroy()
        {
            if (dashTexture != null) Destroy(dashTexture);
            if (generatedMaterial != null) Destroy(generatedMaterial);
        }

        void Update()
        {
            UpdateAimLine();
            UpdateAnchorPulse();
            if (trail != null && sword != null)
            {
                trail.transform.position = sword.position;
                trail.transform.rotation = sword.rotation;
            }
        }

        void OnStateChanged(HookState state)
        {
            if (state == HookState.Thrown && trail != null) trail.Clear();
            ApplyTrailState();
            // Yön saplanılan anchor'dan alınır; nişan hedefi çekiş sırasında değişebilir ya da boş olabilir.
            if (state == HookState.Pulling && kickEvent != null && hook.ActiveAnchor != null)
            {
                var direction = (hook.ActiveAnchor.Point - (Vector2)transform.position).normalized;
                kickEvent.Raise(direction);
            }
        }

        LineRenderer CreateAimLine()
        {
            var child = new GameObject("AimLine");
            child.transform.SetParent(transform, false);
            return child.AddComponent<LineRenderer>();
        }

        TrailRenderer CreateTrail()
        {
            var child = new GameObject("SwordTrail");
            child.transform.SetParent(transform, false);
            return child.AddComponent<TrailRenderer>();
        }

        void ConfigureAimLine()
        {
            aimLine.positionCount = 2;
            aimLine.useWorldSpace = true;
            aimLine.startWidth = aimWidth;
            aimLine.endWidth = aimWidth;
            aimLine.textureMode = LineTextureMode.Tile;
            aimLine.sortingLayerName = "UI_World";
            aimLine.startColor = aimColor;
            aimLine.endColor = aimColor;
            dashTexture = HookFeedbackMath.CreateDashTexture(dashTextureWidth, dashLength);
            var shader = Shader.Find("Sprites/Default");
            if (shader != null)
            {
                generatedMaterial = new Material(shader);
                generatedMaterial.mainTexture = dashTexture;
                aimLine.material = generatedMaterial;
            }
            aimLine.enabled = false;
        }

        void ConfigureTrail()
        {
            trail.time = trailTime;
            trail.widthMultiplier = SwordThickness();
            trail.widthCurve = HookFeedbackMath.TrailWidth;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(trailColor, 0f), new GradientColorKey(trailColor, 1f) },
                new[] { new GradientAlphaKey(trailColor.a, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.emitting = false;
        }

        float SwordThickness()
        {
            if (sword != null)
            {
                var sprite = sword.GetComponentInChildren<SpriteRenderer>();
                if (sprite != null) return sprite.bounds.size.y;
            }
            return aimWidth;
        }

        void ApplyTrailState()
        {
            if (trail != null) trail.emitting = hook != null && HookFeedbackMath.ShouldEmitTrail(hook.State);
        }

        void UpdateAimLine()
        {
            if (aimLine == null || hook == null || hook.State != HookState.InHand || sword == null || hook.AimTarget == null)
            {
                if (aimLine != null) aimLine.enabled = false;
                return;
            }

            aimLine.enabled = true;
            aimLine.startColor = aimColor;
            aimLine.endColor = aimColor;
            aimLine.SetPosition(0, sword.position);
            aimLine.SetPosition(1, hook.AimTarget.Point);
        }

        void UpdateAnchorPulse()
        {
            var target = hook != null ? hook.AimTarget : null;
            if (target != pulsingAnchor)
            {
                RestorePulsingAnchor();
                if (target != null)
                {
                    pulsingAnchor = target;
                    pulsingAnchorScale = target.transform.localScale;
                }
            }

            if (pulsingAnchor == null) return;
            var pulse = HookFeedbackMath.PulseMultiplier(Time.time, pulseFrequency, pulseScale);
            pulsingAnchor.transform.localScale = pulsingAnchorScale * pulse;
        }

        void RestorePulsingAnchor()
        {
            if (pulsingAnchor != null) pulsingAnchor.transform.localScale = pulsingAnchorScale;
            pulsingAnchor = null;
        }
    }
}
