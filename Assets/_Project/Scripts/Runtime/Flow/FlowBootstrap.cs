using UnityEngine;

namespace Game.Flow
{
    /// <summary>
    /// Bulunduğu sahnede SceneFlow'un kurulu olmasını sağlar. Oyun içi arayüz prefab'ında durur:
    /// bölüm doğrudan açılsa da "oyun bitti" kanalı dinlenir, süre ve ölüm sayılır.
    /// </summary>
    public sealed class FlowBootstrap : MonoBehaviour
    {
        void Awake() => SceneFlow.Ensure();
    }
}
