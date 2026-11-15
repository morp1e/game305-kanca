using System;
using System.Collections;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine;

namespace Game.Combat
{
    public sealed class SwordVisual : MonoBehaviour
    {
        [Serializable] public struct SlashVisual { public Sprite[] frames; }
        [SerializeField, Tooltip("Üç adıma atanacak animasyon kareleri; doluysa procedural hilal kapanır.")] SlashVisual[] spriteSteps = new SlashVisual[3];
        [SerializeField, Tooltip("Opsiyonel Animator; slash1, slash2 ve slash3 state/tag adlarını kullanır.")] Animator animator;
        [SerializeField, Tooltip("Adım başına Aseprite animasyon klibi; klip adı slash1, slash2 veya slash3 olur.")] AnimationClip[] clipSteps = new AnimationClip[3];
        PlayableGraph clipGraph;
        AnimationPlayableOutput clipOutput;
        [SerializeField] SpriteRenderer spriteRenderer;
        [SerializeField] MeshFilter meshFilter;
        [SerializeField] MeshRenderer meshRenderer;
        [SerializeField] ParticleSystem spark;
        [SerializeField] SwordTuning tuning;
        [SerializeField] Material sparkMaterial;
        Mesh mesh;
        Material generatedMaterial;
        Material generatedSparkMaterial;
        Coroutine spriteRoutine;
        Coroutine proceduralRoutine;
        SwordAttack attack;

        void Awake()
        {
            if (meshFilter == null) meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null) meshRenderer = GetComponent<MeshRenderer>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            mesh = new Mesh { name = "SwordProceduralSmear" };
            if (meshFilter != null) meshFilter.sharedMesh = mesh;
            if (meshRenderer != null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) { generatedMaterial = new Material(shader); meshRenderer.sharedMaterial = generatedMaterial; }
                meshRenderer.sortingOrder = 3;
            }
            if (spriteRenderer != null) spriteRenderer.sortingOrder = 3;
            if (UsesAnyClip())
            {
                if (animator == null) animator = GetComponentInChildren<Animator>();
                if (animator == null)
                {
                    GameObject animationTarget = spriteRenderer != null ? spriteRenderer.gameObject : gameObject;
                    animator = animationTarget.AddComponent<Animator>();
                }
                clipGraph = PlayableGraph.Create("SwordVisualClips");
                clipOutput = AnimationPlayableOutput.Create(clipGraph, "SwordVisualClip", animator);
            }
            if (spark == null) spark = GetComponentInChildren<ParticleSystem>();
            if (spark == null)
            {
                var sparkObject = new GameObject("HitSparks");
                sparkObject.transform.SetParent(transform, false);
                spark = sparkObject.AddComponent<ParticleSystem>();
            }
            var main = spark.main;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            if (tuning != null) { main.startLifetime = tuning.sparkLifetime; main.startSpeed = tuning.sparkSpeed; main.startSize = tuning.sparkSize; main.maxParticles = Mathf.Max(1, tuning.finisherSparkCount); }
            var emission = spark.emission; emission.enabled = false;
            var sparkRenderer = spark.GetComponent<ParticleSystemRenderer>();
            if (sparkRenderer != null)
            {
                sparkRenderer.sortingOrder = 4;
                if (sparkMaterial == null && tuning != null) sparkMaterial = tuning.sparkMaterial;
                sparkRenderer.sharedMaterial = sparkMaterial != null ? sparkMaterial : CreateSparkMaterial();
            }
            attack = GetComponentInParent<SwordAttack>();
            if (attack != null) { attack.SlashStarted += BeginSlash; attack.HitOccurred += OnHit; }
        }

        void OnDestroy()
        { if (attack != null) { attack.SlashStarted -= BeginSlash; attack.HitOccurred -= OnHit; } if (clipGraph.IsValid()) clipGraph.Destroy(); if (mesh != null) Destroy(mesh); if (generatedMaterial != null) Destroy(generatedMaterial); if (generatedSparkMaterial != null) Destroy(generatedSparkMaterial); }

        Material CreateSparkMaterial()
        {
            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Lit-Default");
            if (shader == null) return null;
            generatedSparkMaterial = new Material(shader) { name = "SwordSparkRuntime" };
            return generatedSparkMaterial;
        }

        public bool UsesSpriteStep(int step) => spriteSteps != null && step >= 0 && step < spriteSteps.Length && spriteSteps[step].frames != null && spriteSteps[step].frames.Length > 0;
        public bool UsesClipStep(int step) => clipSteps != null && step >= 0 && step < clipSteps.Length && clipSteps[step] != null;
        bool UsesAnyClip() { if (clipSteps == null) return false; for (int i = 0; i < clipSteps.Length; i++) if (clipSteps[i] != null) return true; return false; }
        public bool UsesProceduralStep(int step) => !UsesSpriteStep(step) && !UsesClipStep(step) && (animator == null || animator.runtimeAnimatorController == null || !animator.HasState(0, Animator.StringToHash("slash" + (step + 1))));
        public void SetStepSprites(int step, Sprite[] frames)
        { if (step < 0 || step >= 3) return; if (spriteSteps == null || spriteSteps.Length != 3) spriteSteps = new SlashVisual[3]; spriteSteps[step].frames = frames; }
        public static int StepFromTag(string tag) => tag == "slash1" ? 0 : tag == "slash2" ? 1 : tag == "slash3" ? 2 : -1;
        public static int StepFromAnimationName(string name) => StepFromTag(name);

        public void BeginSlash(int step, Vector2 origin, float aimAngle, float arc, float windup, float activeDuration)
        {
            transform.position = origin;
            if (proceduralRoutine != null) StopCoroutine(proceduralRoutine);
            StopSprites();
            ClearMesh();
            proceduralRoutine = StartCoroutine(RenderAfterWindup(step, aimAngle, arc, windup, activeDuration));
        }

        IEnumerator RenderAfterWindup(int step, float aim, float arc, float windup, float active)
        {
            if (windup > 0f) yield return new WaitForSeconds(windup);
            if (UsesSpriteStep(step))
            {
                StartSprites(step);
                yield return new WaitForSeconds(active);
                StopSprites();
            }
            else if (UsesClipStep(step) && clipGraph.IsValid())
            {
                transform.rotation = Quaternion.Euler(0f, 0f, aim);
                if (spriteRenderer != null) spriteRenderer.enabled = true;
                AnimationClipPlayable playable = AnimationClipPlayable.Create(clipGraph, clipSteps[step]);
                clipOutput.SetSourcePlayable(playable);
                clipGraph.Play();
                yield return new WaitForSeconds(active);
                clipGraph.Stop();
                clipGraph.DestroySubgraph(playable);
            }
            else if (animator != null && animator.runtimeAnimatorController != null && animator.HasState(0, Animator.StringToHash("slash" + (step + 1))))
            {
                transform.rotation = Quaternion.Euler(0f, 0f, aim);
                if (spriteRenderer != null) spriteRenderer.enabled = true;
                animator.Play("slash" + (step + 1), 0, 0f);
                yield return new WaitForSeconds(active);
            }
            else
            {
                transform.rotation = Quaternion.identity;
                if (meshRenderer != null) meshRenderer.enabled = true;
                yield return Procedural(aim, arc, active);
            }
            ClearMesh(); proceduralRoutine = null;
        }

        IEnumerator Procedural(float aimAngle, float arc, float duration)
        {
            float elapsed = 0f;
            float visible = Mathf.Max(0.001f, duration);
            while (elapsed < visible)
            {
                float progress = Mathf.Clamp01(elapsed / visible);
                float openArc = arc * Mathf.SmoothStep(0.15f, 1f, progress);
                float fadeStart = Mathf.Clamp01(1f - (tuning != null ? tuning.smearFadeSeconds : 0.05f) / visible);
                float fade = progress < fadeStart ? 1f : 1f - (progress - fadeStart) / Mathf.Max(0.001f, 1f - fadeStart);
                BuildMesh(aimAngle, openArc, fade);
                elapsed += Time.deltaTime;
                yield return null;
            }
            ClearMesh();
        }

        void BuildMesh(float aim, float arc, float alpha)
        {
            if (mesh == null || tuning == null) return;
            int segments = Mathf.Max(3, Mathf.CeilToInt(arc / 8f));
            var vertices = new Vector3[(segments + 1) * 2];
            var colors = new Color[vertices.Length];
            var triangles = new int[segments * 6];
            float inner = Mathf.Max(0.05f, tuning.bladeLength - tuning.smearThickness);
            float outer = tuning.bladeLength + tuning.smearThickness;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (aim - arc * 0.5f + arc * i / segments) * Mathf.Deg2Rad;
                Vector3 direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                vertices[i * 2] = direction * inner; vertices[i * 2 + 1] = direction * outer;
                Color color = tuning.smearColor; color.a *= alpha; colors[i * 2] = colors[i * 2 + 1] = color;
                if (i < segments)
                { int t = i * 6, v = i * 2; triangles[t] = v; triangles[t + 1] = v + 1; triangles[t + 2] = v + 2; triangles[t + 3] = v + 1; triangles[t + 4] = v + 3; triangles[t + 5] = v + 2; }
            }
            mesh.Clear(); mesh.vertices = vertices; mesh.colors = colors; mesh.triangles = triangles; mesh.RecalculateBounds();
        }

        void StartSprites(int step)
        {
            StopSprites();
            if (spriteRenderer == null) return;
            if (meshRenderer != null) meshRenderer.enabled = false;
            spriteRoutine = StartCoroutine(AnimateSprites(step));
        }
        IEnumerator AnimateSprites(int step)
        {
            Sprite[] frames = spriteSteps[step].frames;
            for (int i = 0; i < frames.Length; i++)
            { spriteRenderer.sprite = frames[i]; yield return new WaitForSeconds(tuning != null ? tuning.spriteFrameSeconds : 0f); }
            spriteRenderer.sprite = null; spriteRoutine = null;
        }
        void StopSprites() { if (spriteRoutine != null) StopCoroutine(spriteRoutine); spriteRoutine = null; if (spriteRenderer != null) spriteRenderer.sprite = null; }
        void OnHit(Vector2 point, Vector2 direction, bool killed)
        { transform.position = point; if (spark != null) { spark.transform.position = point; spark.transform.right = direction; spark.Emit(killed ? (tuning != null ? tuning.finisherSparkCount : 12) : (tuning != null ? tuning.sparkCount : 7)); } }
        void ClearMesh() { if (mesh != null) mesh.Clear(); if (meshRenderer != null) meshRenderer.enabled = false; }
    }
}
