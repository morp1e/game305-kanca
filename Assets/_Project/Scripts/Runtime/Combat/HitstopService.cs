using System;
using UnityEngine;

namespace Game.Combat
{
    /// <summary>Sahne çapında tek hitstop saati; unscaled süre kullanır.</summary>
    public sealed class HitstopService : MonoBehaviour
    {
        static HitstopService instance;
        float remaining;
        float restoreScale = 1f;
        float requestedStopScale = 0.05f;
        bool active;
        bool slowActive;
        float slowRemaining;
        float slowRestoreScale = 1f;
        public static HitstopService Instance => instance;
        public bool IsActive => active;
        public float ScaleForPauseResume => active ? Mathf.Min(restoreScale, requestedStopScale) : slowActive ? Mathf.Min(slowRestoreScale, killSlowScale) : Time.timeScale;
        public event Action Ended;

        void Awake()
        {
            if (instance != null && instance != this) { Destroy(this); return; }
            instance = this;
        }

        public static HitstopService Ensure(GameObject host)
        {
            if (instance != null) return instance;
            instance = FindFirstObjectByType<HitstopService>();
            if (instance == null) instance = host.AddComponent<HitstopService>();
            return instance;
        }

        public bool Request(float duration, float slowDuration = 0f, float slowScale = 0.35f, float stopScale = 0.05f)
        {
            if (duration <= 0f || Time.timeScale <= 0f) return false;
            if (!active)
            {
                restoreScale = slowActive ? slowRestoreScale : Time.timeScale;
                active = true;
                requestedStopScale = Mathf.Clamp(stopScale, 0.01f, 0.2f);
                Time.timeScale = Mathf.Min(restoreScale, requestedStopScale);
            }
            remaining = Mathf.Max(remaining, duration);
            if (slowDuration > killSlowRemaining)
            {
                killSlowRemaining = slowDuration;
                killSlowScale = Mathf.Clamp(slowScale, 0.01f, 1f);
            }
            return true;
        }

        float killSlowRemaining;
        float killSlowScale = 0.35f;

        void Update()
        {
            // PauseMenu owns zero. It may pause during hitstop; wait until it resumes.
            if (Time.timeScale <= 0f) return;
            if (active)
            {
                remaining -= Time.unscaledDeltaTime;
                if (remaining > 0f) return;
                active = false;
                Time.timeScale = restoreScale;
                Ended?.Invoke();
            }
            if (killSlowRemaining > 0f)
            {
                slowActive = true;
                slowRestoreScale = restoreScale;
                slowRemaining = Mathf.Max(slowRemaining, killSlowRemaining);
                Time.timeScale = Mathf.Min(slowRestoreScale, killSlowScale);
                killSlowRemaining = 0f;
            }
            else if (!active && slowActive)
            {
                Time.timeScale = Mathf.Min(slowRestoreScale, killSlowScale);
                slowRemaining -= Time.unscaledDeltaTime;
                if (slowRemaining <= 0f)
                {
                    slowActive = false;
                    Time.timeScale = slowRestoreScale;
                }
            }
        }

        void OnDestroy()
        {
            if (instance != this) return;
            Time.timeScale = 1f;
            active = false;
            slowActive = false;
            remaining = 0f;
            slowRemaining = 0f;
            killSlowRemaining = 0f;
            instance = null;
        }
    }
}
