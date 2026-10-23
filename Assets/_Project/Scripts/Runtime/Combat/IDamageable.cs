using UnityEngine;

namespace Game.Combat
{
    public interface IDamageable
    {
        void TakeHit(HitInfo hit);
    }
}
