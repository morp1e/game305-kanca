using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Tek metin taşıyan olay kanalı. Örnek: boss dövüşü başladığında boss'un görünen adı.</summary>
    [CreateAssetMenu(menuName = "Kanca/Events/String Event", fileName = "Event_")]
    public class StringEvent : ScriptableObject
    {
        event Action<string> listeners;

        public void Raise(string value) => listeners?.Invoke(value);
        public void Register(Action<string> listener) => listeners += listener;
        public void Unregister(Action<string> listener) => listeners -= listener;

        void OnDisable() => listeners = null;
    }
}
