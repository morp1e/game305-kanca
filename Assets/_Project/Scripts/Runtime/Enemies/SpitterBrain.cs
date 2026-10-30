using UnityEngine;
namespace Game.Enemies
{
    /// <summary>Görüş hattındaki oyuncuya tek mermi atan düşman.</summary>
    public class SpitterBrain : EnemyBrain
    {
        [SerializeField] Projectile projectilePrefab;
        protected override void TickState()
        {
            switch (State)
            {
                case EnemyState.Patrol: Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                if (CanSeePlayer()) ChangeState(EnemyState.Notice);
                break;
                case EnemyState.Notice: FaceTarget();
                Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                ChangeState(EnemyState.Telegraph);
                break;
                case EnemyState.Telegraph: FaceTarget();
                Body.linearVelocity = new Vector2(0f, Body.linearVelocity.y);
                if (StateTime >= Tuning.telegraphDuration)  {
                    Fire();
                    ChangeState(EnemyState.Recover);
                }
                break;
                case EnemyState.Recover: if (StateTime >= Tuning.attackCooldown) ChangeState(CanSeePlayer() ? EnemyState.Notice : EnemyState.Patrol);
                break;
                case EnemyState.Chase: ChangeState(EnemyState.Notice);
                break;
            }
        }
        void Fire()
        {
            if (projectilePrefab == null || Tuning.projectile == null || !HasTarget) return;
            Vector2 origin = transform.position;
            Vector2 direction = (TargetPosition - origin).normalized;
            Projectile projectile = Instantiate(projectilePrefab, origin + direction * .6f, Quaternion.identity);
            projectile.Initialize(Tuning.projectile, direction, this);
        }
    }
}
