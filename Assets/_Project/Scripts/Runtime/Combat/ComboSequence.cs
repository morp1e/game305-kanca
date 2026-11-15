using UnityEngine;

namespace Game.Combat
{
    /// <summary>Unity objesinden bağımsız kombo adımı ve pencere hesabı.</summary>
    public sealed class ComboSequence
    {
        int nextStep;
        float lastStart = float.NegativeInfinity;
        bool lastStartWasFinisher;
        public int NextStep => nextStep;
        public float LastStart => lastStart;

        public int Start(float now, ComboStep[] steps, float interval, float finisherWindow)
        {
            int count = steps == null ? 0 : steps.Length;
            if (count == 0) return -1;
            if (!CanStart(now, interval, finisherWindow)) return -1;
            if (!lastStartWasFinisher && nextStep > 0 && now - lastStart > Mathf.Max(0f, finisherWindow)) nextStep = 0;
            int started = Mathf.Clamp(nextStep, 0, count - 1);
            nextStep = (started + 1) % count;
            lastStart = now;
            lastStartWasFinisher = started == count - 1;
            return started;
        }

        public bool CanStart(float now, float interval, float finisherWindow)
        {
            float requiredInterval = lastStartWasFinisher ? Mathf.Max(interval, finisherWindow) : interval;
            return now - lastStart >= Mathf.Max(0f, requiredInterval) - 0.0001f;
        }

        public void Reset() { nextStep = 0; lastStart = float.NegativeInfinity; lastStartWasFinisher = false; }

        public static int Phase(float time, ComboStep step)
        {
            if (time < Mathf.Max(0f, step.windup)) return 0;
            if (time < Mathf.Max(0f, step.windup) + Mathf.Max(0.001f, step.active)) return 1;
            if (time < Mathf.Max(0f, step.windup) + Mathf.Max(0.001f, step.active) + Mathf.Max(0f, step.recovery)) return 2;
            return 3;
        }
    }
}
