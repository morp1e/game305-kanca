using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Tek Vector2 taşıyan olay kanalı. Örnek: çekiş yönünde kamera itkisi.</summary>
    [CreateAssetMenu(menuName = "Kanca/Events/Vector2 Event", fileName = "Event_")]
    public class Vector2Event : ScriptableObject
    {
        event Action<Vector2> listeners;

        public void Raise(Vector2 value) => listeners?.Invoke(value);
        public void Register(Action<Vector2> listener) => listeners += listener;
        public void Unregister(Action<Vector2> listener) => listeners -= listener;

        void OnDisable() => listeners = null;
    }
}
