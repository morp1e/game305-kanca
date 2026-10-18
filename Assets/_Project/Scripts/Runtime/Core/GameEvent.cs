using System;
using UnityEngine;

namespace Game.Core
{
    /// <summary>Parametresiz olay kanalı. Sistemler birbirine değil bu asset'e bağlanır.</summary>
    [CreateAssetMenu(menuName = "Kanca/Events/Game Event", fileName = "Event_")]
    public class GameEvent : ScriptableObject
    {
        event Action listeners;

        public void Raise() => listeners?.Invoke();
        public void Register(Action listener) => listeners += listener;
        public void Unregister(Action listener) => listeners -= listener;

        // Domain reload kapalı: oyunlar arasında eski dinleyiciler kalmasın.
        void OnDisable() => listeners = null;
    }
}
