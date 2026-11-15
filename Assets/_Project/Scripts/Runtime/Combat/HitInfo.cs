using UnityEngine;

namespace Game.Combat
{
    public struct HitInfo
    {
        public float Damage;
        public Vector2 Direction;
        public float Knockback;
        public Object Source;
        public float StunDuration;
        public float Hitstop;

        public HitInfo(float damage, Vector2 direction, float knockback, Object source, float stunDuration = 0f, float hitstop = 0f)
        {
            Damage = damage;
            Direction = direction;
            Knockback = knockback;
            Source = source;
            StunDuration = stunDuration;
            Hitstop = hitstop;
        }
    }
}
