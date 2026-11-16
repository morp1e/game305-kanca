using Game.Core;
using Game.Hook;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Feedback
{
    /// <summary>Kancanın görsel geri bildirimi: nişan çizgisi, kanca izi ve çekiş itkisi.</summary>
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
        [SerializeField, Tooltip("Kanca izinin süresi, saniye. Ö")]
        float trailTime = 0.3f;
        [SerializeField, Tooltip("Kancanın uçuş süresini görünür kılmak için gereken en kısa iz süresi, saniye. Ö")]
        float minimumTrailTime = 0.3f;
        [SerializeField, Tooltip("Nişan çizgisinin kalınlığı, 1/16 unit. Ö")]
        float aimWidth = 1f / 16f;
        [SerializeField, Tooltip("Kesikli doku genişliği, piksel. Ö")]
        int dashTextureWidth = 8;
        [SerializeField, Tooltip("Kesikli doku içindeki çizgi uzunluğu, piksel. Ö")]
        int dashLength = 4;

        Texture2D dashTexture;
        Material generatedMaterial;
        Material generatedTrailMaterial;
        bool trailWasEmitting;

        void Awake()
        {
            if (aimLine == null) aimLine = CreateAimLine();
            if (trail == null) trail = CreateTrail();
            ConfigureAimLine();
            ConfigureTrail();
            ApplyTrailState();
            SetHookVisualVisible(hook != null && hook.State != HookState.InHand);
        }

        void OnEnable()
        {
            if (hook != null) hook.StateChanged += OnStateChanged;
            ApplyTrailState();
            SetHookVisualVisible(hook != null && hook.State != HookState.InHand);
        }

        void OnDisable()
        {
            if (hook != null) hook.StateChanged -= OnStateChanged;
            if (trail != null)
            {
                trail.emitting = false;
                trail.Clear();
                trailWasEmitting = false;
            }
        }

        void OnDestroy()
        {
            if (dashTexture != null) Destroy(dashTexture);
            if (generatedMaterial != null) Destroy(generatedMaterial);
            if (generatedTrailMaterial != null) Destroy(generatedTrailMaterial);
        }

        void Update()
        {
            UpdateAimLine();
            UpdateTrailPosition();
            ApplyTrailState();
        }

        void UpdateTrailPosition()
        {
            if (trail != null && hookVisual != null)
                trail.transform.position = hookVisual.position;
        }

        void OnStateChanged(HookState state)
        {
            ApplyTrailState(state);
            SetHookVisualVisible(state != HookState.InHand);
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
            var child = new GameObject("HookTrail");
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
            var shader = FindSpriteShader();
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
            if (trail.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
                if (shader != null)
                {
                    generatedTrailMaterial = new Material(shader) { name = "HookTrailRuntime" };
                    trail.sharedMaterial = generatedTrailMaterial;
                }
            }
            trail.time = Mathf.Max(trailTime, minimumTrailTime);
            trail.widthMultiplier = HookThickness();
            trail.widthCurve = HookFeedbackMath.TrailWidth;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(trailColor, 0f), new GradientColorKey(trailColor, 1f) },
                new[] { new GradientAlphaKey(trailColor.a, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.emitting = false;
        }

        static Shader FindSpriteShader()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            return shader;
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
            ApplyTrailState(hook != null ? hook.State : HookState.InHand);
        }

        void ApplyTrailState(HookState state)
        {
            bool shouldEmit = state == HookState.Thrown || state == HookState.Pulling ||
                state == HookState.Returning;
            if (trail != null && shouldEmit != trailWasEmitting)
            {
                trail.emitting = false;
                trail.Clear();
                UpdateTrailPosition();
                trail.emitting = shouldEmit;
                trailWasEmitting = shouldEmit;
            }
            else if (trail != null && !shouldEmit)
            {
                trail.emitting = false;
                trail.Clear();
            }
        }

        void SetHookVisualVisible(bool visible)
        {
            if (hookVisual != null && hookVisual.gameObject.activeSelf != visible)
                hookVisual.gameObject.SetActive(visible);
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
