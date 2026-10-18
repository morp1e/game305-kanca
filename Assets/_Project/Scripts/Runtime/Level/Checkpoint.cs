using UnityEngine;

namespace Game.Level
{
    /// <summary>Oyuncu içinden geçince yeniden doğma noktası olur. Trigger collider ister. SÖZLEŞME İSKELETİ (Lane 1). Alan: spawnOffset.</summary>
    [RequireComponent(typeof(Collider2D))]
    public class Checkpoint : MonoBehaviour
    {
        [SerializeField] Vector2 spawnOffset = new Vector2(0f, 1f);
    }
}
