using System;
using UnityEngine;

namespace Game.Combat
{
    public class SwordVoiceEvents : MonoBehaviour
    {
        public event Action HitLanded;
        public event Action SwingMissed;
        public void Hit() => HitLanded?.Invoke();
        public void Miss() => SwingMissed?.Invoke();
    }
}
