using Game.Flow;
using Game.Player;
using UnityEngine;

namespace Game.Level
{
    /// <summary>Oyuncu içinden geçince sıradaki sahneye geçer (katalogdaki sıra). Trigger collider ister.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelExit : MonoBehaviour
    {
        bool used;

        void Awake() => GetComponent<Collider2D>().isTrigger = true;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (used || other.attachedRigidbody == null) return;
            if (other.attachedRigidbody.GetComponentInParent<PlayerMotor>() == null) return;
            used = true;
            SceneFlow.Ensure().LoadNext();
        }
    }
}
