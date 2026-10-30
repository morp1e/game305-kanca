using UnityEngine;

namespace Game.Level
{
    [RequireComponent(typeof(PolygonCollider2D))]
    public sealed class LevelBounds : MonoBehaviour
    {
        public PolygonCollider2D Collider => GetComponent<PolygonCollider2D>();
    }
}
