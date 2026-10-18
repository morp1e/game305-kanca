using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Tek float taşıyan olay kanalı. Örnek: kamera sarsıntısı için trauma miktarı.</summary>
    [CreateAssetMenu(menuName = "Kanca/Events/Float Event", fileName = "Event_")]
    public class FloatEvent : ScriptableObject
    {
        event Action<float> listeners;

        public void Raise(float value) => listeners?.Invoke(value);
        public void Register(Action<float> listener) => listeners += listener;
        public void Unregister(Action<float> listener) => listeners -= listener;

        void OnDisable() => listeners = null;
    }
}
