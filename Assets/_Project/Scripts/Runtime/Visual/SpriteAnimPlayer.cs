using System;
using UnityEngine;

namespace Game.Visual
{
    public sealed class SpriteAnimPlayer : MonoBehaviour
    {
        [SerializeField, Tooltip("Animasyon karelerinin yazılacağı renderer.")]
        SpriteRenderer spriteRenderer;
        [SerializeField, Tooltip("Bu görselin animasyon verisi.")]
        SpriteAnimSet set;

        SpriteAnimClip currentClip;
        float elapsed;
        float frameDuration;
        float playbackDuration;
        bool durationOverride;
        int currentFrame = -1;

        public event Action<string> ClipFinished;
        public event Action<string> FrameEvent;

        public float GetClipDuration(string clipName)
        {
            SpriteAnimClip clip = set != null ? set.Find(clipName) : null;
            return clip == null || clip.frames == null || clip.frames.Length == 0
                ? 0f
                : clip.frames.Length / Mathf.Max(0.01f, clip.fps);
        }

        void Awake()
        {
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (set != null)
            {
                transform.localScale = Vector3.one * Mathf.Max(0.01f, set.visualScale);
                if (spriteRenderer != null) spriteRenderer.transform.localPosition += (Vector3)set.visualOffset;
            }
        }

        void Update() => Advance(Time.deltaTime);

        public void Play(string clipName, bool restart = false)
        {
            PlayInternal(clipName, 0f, false, restart);
        }

        public void Play(string clipName, float durationSeconds)
        {
            PlayInternal(clipName, durationSeconds, true, true);
        }

        public void Stop()
        {
            currentClip = null;
            elapsed = 0f;
            currentFrame = -1;
            durationOverride = false;
        }

        void PlayInternal(string clipName, float durationSeconds, bool useDuration, bool restart)
        {
            if (set == null || spriteRenderer == null) return;
            SpriteAnimClip clip = set.Find(clipName);
            if (clip == null || clip.frames == null || clip.frames.Length == 0) return;
            if (!restart && currentClip == clip) return;

            currentClip = clip;
            elapsed = 0f;
            currentFrame = -1;
            durationOverride = useDuration && durationSeconds > 0f;
            playbackDuration = Mathf.Max(0.0001f, durationSeconds);
            frameDuration = 1f / Mathf.Max(0.01f, clip.fps);
            SetFrame(0, true);
        }

        // Süreyi açıkça vermek testlerde ve harici zaman sürücülerinde deterministik ilerleme sağlar.
        public void Advance(float deltaTime)
        {
            if (currentClip == null || currentClip.frames == null || currentClip.frames.Length == 0 || deltaTime <= 0f) return;
            float previousElapsed = elapsed;
            elapsed += deltaTime;
            float step = durationOverride ? playbackDuration / currentClip.frames.Length : frameDuration;
            int frame = Mathf.FloorToInt(elapsed / step);
            RaiseCrossedFrameEvents(previousElapsed, elapsed, step);
            if (currentClip.loop)
            {
                int nextFrame = frame % currentClip.frames.Length;
                if (nextFrame != currentFrame) SetFrame(nextFrame, false);
                return;
            }

            if (frame >= currentClip.frames.Length)
            {
                bool finished = elapsed >= step * currentClip.frames.Length;
                if (currentFrame != currentClip.frames.Length - 1) SetFrame(currentClip.frames.Length - 1, false);
                if (finished)
                {
                    string finishedName = currentClip.name;
                    currentClip = null;
                    ClipFinished?.Invoke(finishedName);
                }
                return;
            }

            if (frame != currentFrame) SetFrame(frame, false);
        }

        void RaiseCrossedFrameEvents(float fromElapsed, float toElapsed, float step)
        {
            int eventFrame = currentClip.eventFrame;
            int frameCount = currentClip.frames.Length;
            if (eventFrame < 0 || eventFrame >= frameCount) return;

            int previousAbsoluteFrame = Mathf.FloorToInt(fromElapsed / step);
            int currentAbsoluteFrame = Mathf.FloorToInt(toElapsed / step);
            if (!currentClip.loop) currentAbsoluteFrame = Mathf.Min(currentAbsoluteFrame, frameCount - 1);
            int occurrence = eventFrame;
            if (occurrence <= previousAbsoluteFrame)
            {
                if (!currentClip.loop) return;
                occurrence += ((previousAbsoluteFrame - occurrence) / frameCount + 1) * frameCount;
            }
            while (occurrence <= currentAbsoluteFrame)
            {
                FrameEvent?.Invoke(currentClip.name);
                if (!currentClip.loop) break;
                occurrence += frameCount;
            }
        }

        void SetFrame(int frame, bool notifyEvent)
        {
            currentFrame = frame;
            spriteRenderer.sprite = currentClip.frames[frame];
            if (notifyEvent && currentClip.eventFrame == frame) FrameEvent?.Invoke(currentClip.name);
        }

        public void SetFlip(bool faceLeft)
        {
            if (spriteRenderer == null) return;
            bool flip = set != null && set.spritesFaceLeft ? !faceLeft : faceLeft;
            spriteRenderer.flipX = flip;
        }
    }
}
