using Game.Core;
using Game.Hook;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Feedback
{
    /// <summary>Kancanın görsel geri bildirimi: nişan çizgisi, kılıç izi ve çekiş itkisi.</summary>
    public class HookFeedback : MonoBehaviour
    {
        [SerializeField] GrappleHook hook;
        [SerializeField, FormerlySerializedAs("sword")] Transform hookVisual;
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
        [SerializeField, Tooltip("Kesikli doku genişliği, piksel. Ö")]
        int dashTextureWidth = 8;
        [SerializeField, Tooltip("Kesikli doku içindeki çizgi uzunluğu, piksel. Ö")]
        int dashLength = 4;

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
        }

        void OnDestroy()
        {
            if (dashTexture != null) Destroy(dashTexture);
            if (generatedMaterial != null) Destroy(generatedMaterial);
        }

        void Update()
        {
            UpdateAimLine();
            if (trail != null && hookVisual != null)
            {
                trail.transform.position = hookVisual.position;
                trail.transform.rotation = hookVisual.rotation;
            }
        }

        void OnStateChanged(HookState state)
        {
            if (state == HookState.Thrown && trail != null) trail.Clear();
            ApplyTrailState();
            // Yön saplanılan yüzey noktasından alınır.
            if (state == HookState.Pulling && kickEvent != null)
            {
                var direction = (hook.ActivePoint - (Vector2)transform.position).normalized;
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
            trail.widthMultiplier = HookThickness();
            trail.widthCurve = HookFeedbackMath.TrailWidth;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(trailColor, 0f), new GradientColorKey(trailColor, 1f) },
                new[] { new GradientAlphaKey(trailColor.a, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.emitting = false;
        }

        float HookThickness()
        {
            if (hookVisual != null)
            {
                var sprite = hookVisual.GetComponentInChildren<SpriteRenderer>();
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
            if (aimLine == null || hook == null || hook.State != HookState.InHand || hookVisual == null)
            {
                if (aimLine != null) aimLine.enabled = false;
                return;
            }

            aimLine.enabled = true;
            aimLine.startColor = aimColor;
            aimLine.endColor = aimColor;
            aimLine.SetPosition(0, hookVisual.position);
            aimLine.SetPosition(1, hook.AimPoint);
        }
    }
}
