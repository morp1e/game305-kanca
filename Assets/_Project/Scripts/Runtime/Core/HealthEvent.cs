using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Can değişimi kanalı (mevcut, en fazla). Oyuncu canı ve boss canı ayrı asset'lerdir.
    /// Son değeri saklar: sonradan bağlanan arayüz Register anında güncel değeri alır.
    /// </summary>
    [CreateAssetMenu(menuName = "Kanca/Events/Health Event", fileName = "Event_")]
    public class HealthEvent : ScriptableObject
    {
        event Action<float, float> listeners;

        public bool HasValue { get; private set; }
        public float Current { get; private set; }
        public float Max { get; private set; }

        public void Raise(float current, float max)
        {
            HasValue = true;
            Current = current;
            Max = max;
            listeners?.Invoke(current, max);
        }

        /// <summary>Kanal daha önce yayın yaptıysa dinleyici hemen son değerle çağrılır.</summary>
        public void Register(Action<float, float> listener)
        {
            listeners += listener;
            if (HasValue) listener?.Invoke(Current, Max);
        }

        public void Unregister(Action<float, float> listener) => listeners -= listener;

        // Domain reload kapalı: oyunlar arasında eski dinleyiciler ve değer kalmasın.
        void OnDisable()
        {
            listeners = null;
            HasValue = false;
            Current = Max = 0f;
        }
    }
}
