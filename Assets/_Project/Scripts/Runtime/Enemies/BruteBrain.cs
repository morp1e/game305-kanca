using UnityEngine;
namespace Game.Enemies
{
    /// <summary>Ağır düşman. Kalkanı yalnız ön taraftan gelen vuruşları durdurur.</summary>
    public class BruteBrain : EnemyBrain
    {
        float attackHitboxRemaining;
        protected override void TickState()
        {
            switch (State)
            {
                case EnemyState.Patrol: GroundOrWallAhead();
                SetHorizontal(Tuning.moveSpeed);
                if (CanSeePlayer()) ChangeState(EnemyState.Notice);
                break;
                case EnemyState.Notice: FaceTarget();
                SetHorizontal(0f);
                ChangeState(EnemyState.Telegraph);
                break;
                case EnemyState.Telegraph: FaceTarget();
                SetHorizontal(0f);
                if (StateTime >= Tuning.telegraphDuration)  {
                    attackHitboxRemaining = Tuning.attackDuration;
                    ChangeState(EnemyState.Attack);
                }
                break;
                case EnemyState.Attack: SetHorizontal(0f);
                attackHitboxRemaining -= Time.fixedDeltaTime;
                HitPlayerInFront();
                if (attackHitboxRemaining <= 0f) ChangeState(EnemyState.Recover);
                break;
                case EnemyState.Recover: SetHorizontal(0f);
                if (StateTime >= Tuning.recoverDuration) ChangeState(EnemyState.Patrol);
                break;
                case EnemyState.Chase: FaceTarget();
                SetHorizontal(Tuning.moveSpeed);
                if (CanSeePlayer()) ChangeState(EnemyState.Notice);
                break;
            }
        }
        void HitPlayerInFront()
        {
            if (!HasTarget) return;
            Vector2 center = (Vector2)transform.position + Vector2.right * Facing * Tuning.attackBoxForward;
            if (Physics2D.OverlapBox(center, Tuning.attackBoxSize, 0f, LayerMask.GetMask("Player")) == null) return;
            var damageable = Target.GetComponent<Game.Combat.IDamageable>();
            damageable?.TakeHit(new Game.Combat.HitInfo(Tuning.attackDamage, Vector2.right * Facing, 0f, this));
        }
    }
}
