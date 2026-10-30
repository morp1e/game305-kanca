using Game.Core;
using UnityEngine;

namespace Game.Flow
{
    /// <summary>Bir oyunun süresi ve ölüm sayısı. SceneFlow ile aynı kalıcı objede durur.</summary>
    public sealed class RunStats : MonoBehaviour
    {
        [SerializeField] GameEvent playerDeath;

        float startedAt;
        float stoppedElapsed;

        public bool Running { get; private set; }
        public int Deaths { get; private set; }
        public float Elapsed => Running ? Time.unscaledTime - startedAt : stoppedElapsed;

        public string FormattedTime
        {
            get
            {
                int seconds = Mathf.FloorToInt(Elapsed);
                return $"{seconds / 60:00}:{seconds % 60:00}";
            }
        }

        void OnEnable()
        {
            if (playerDeath != null) playerDeath.Register(CountDeath);
        }

        void OnDisable()
        {
            if (playerDeath != null) playerDeath.Unregister(CountDeath);
        }

        void CountDeath()
        {
            if (Running) Deaths++;
        }

        public void Begin()
        {
            Deaths = 0;
            stoppedElapsed = 0f;
            startedAt = Time.unscaledTime;
            Running = true;
        }

        /// <summary>Bölüm doğrudan açıldıysa (editörde Play) sayaç o an başlar.</summary>
        public void BeginIfIdle()
        {
            if (!Running && stoppedElapsed <= 0f) Begin();
        }

        public void Stop()
        {
            if (!Running) return;
            stoppedElapsed = Elapsed;
            Running = false;
        }

        public void ResetRun()
        {
            Deaths = 0;
            stoppedElapsed = 0f;
            Running = false;
        }
    }
}
