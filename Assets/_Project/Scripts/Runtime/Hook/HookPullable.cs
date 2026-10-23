using UnityEngine;

namespace Game.Hook
{
    public enum HookWeightClass { Light, Heavy }

    public class HookPullable : MonoBehaviour
    {
        [SerializeField] HookWeightClass weightClass;
        [SerializeField] float pullSpeedOverride;
        [SerializeField] Rigidbody2D body;
        public HookWeightClass WeightClass => weightClass;
        public float PullSpeedOverride => pullSpeedOverride;
        public Rigidbody2D Body => body != null ? body : GetComponentInParent<Rigidbody2D>();
    }
}
