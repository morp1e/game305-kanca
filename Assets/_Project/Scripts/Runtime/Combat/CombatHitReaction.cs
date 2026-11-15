using System.Collections;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>Gövde kökünü oynatmadan yalnız sprite katmanını flaşlatıp hitstop'ta sarsar.</summary>
    public sealed class CombatHitReaction : MonoBehaviour
    {
        [SerializeField] SwordTuning tuning;
        SpriteRenderer[] sprites;
        Vector3[] appliedOffsets;
        bool[] canShake;
        Color[] colors;
        Coroutine routine;

        void Awake()
        {
            sprites = GetComponentsInChildren<SpriteRenderer>(true);
            appliedOffsets = new Vector3[sprites.Length];
            canShake = new bool[sprites.Length];
            colors = new Color[sprites.Length];
            Rigidbody2D body = GetComponent<Rigidbody2D>();
            for (int i = 0; i < sprites.Length; i++)
            {
                Transform spriteTransform = sprites[i].transform;
                canShake[i] = spriteTransform != transform &&
                              (body == null || spriteTransform != body.transform) &&
                              spriteTransform.GetComponent<Rigidbody2D>() == null;
                colors[i] = sprites[i].color;
            }
        }

        public void Play(float hitstop, float flashDurationOverride = -1f, Color? flashColorOverride = null)
        {
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(React(hitstop, flashDurationOverride, flashColorOverride));
        }

        IEnumerator React(float hitstop, float flashDurationOverride, Color? flashColorOverride)
        {
            Color flashColor = flashColorOverride ?? Color.white;
            for (int i = 0; i < sprites.Length; i++) if (sprites[i] != null) sprites[i].color = flashColor;
            float flashDuration = flashDurationOverride >= 0f ? flashDurationOverride : tuning != null ? tuning.hitFlashDuration : 0f;
            float shakeDistance = tuning != null ? tuning.hitShakeDistance : 0f;
            float elapsed = 0f;
            while (elapsed < Mathf.Max(flashDuration, hitstop))
            {
                bool shaking = HitstopService.Instance != null && HitstopService.Instance.IsActive;
                for (int i = 0; i < sprites.Length; i++)
                {
                    if (sprites[i] == null) continue;
                    RemoveOffset(i);
                    if (shaking && canShake[i])
                    {
                        appliedOffsets[i] = (Vector3)Random.insideUnitCircle * shakeDistance;
                        sprites[i].transform.localPosition += appliedOffsets[i];
                    }
                }
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            Restore();
            routine = null;
        }

        void Restore()
        {
            for (int i = 0; i < sprites.Length; i++) if (sprites[i] != null)
            { sprites[i].color = colors[i]; RemoveOffset(i); }
        }

        void RemoveOffset(int index)
        {
            if (appliedOffsets[index] == Vector3.zero) return;
            sprites[index].transform.localPosition -= appliedOffsets[index];
            appliedOffsets[index] = Vector3.zero;
        }

        void OnDisable() { if (routine != null) StopCoroutine(routine); Restore(); }
    }
}
