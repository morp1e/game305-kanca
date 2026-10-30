using UnityEngine;
namespace Game.Enemies
{
    /// <summary>Ev noktasında süzülen ve oyuncunun konumuna dalan düşman.</summary>
    public class FlyerBrain : EnemyBrain
    {
        Vector2 diveTarget;
        protected override void TickState()
        {
            switch (State)
            {
                case EnemyState.Patrol: Vector2 hover = Home + Vector2.up * Mathf.Sin(Time.time * Tuning.hoverFrequency) * Tuning.hoverAmplitude;
                Body.linearVelocity = (hover - (Vector2)transform.position) * Tuning.moveSpeed;
                if (CanSeePlayer()) ChangeState(EnemyState.Notice);
                break;
                case EnemyState.Notice: FaceTarget();
                Body.linearVelocity = Vector2.zero;
                ChangeState(EnemyState.Telegraph);
                break;
                case EnemyState.Telegraph: FaceTarget();
                Body.linearVelocity = Vector2.zero;
                if (StateTime >= Tuning.telegraphDuration)  {
                    diveTarget = TargetPosition;
                    ChangeState(EnemyState.Attack);
                }
                break;
                case EnemyState.Attack: Vector2 direction = (diveTarget - (Vector2)transform.position).normalized;
                Body.linearVelocity = direction * Tuning.chaseSpeed;
                Facing = direction.x >= 0f ? 1 : -1;
                if (StateTime >= Tuning.attackDuration) ChangeState(EnemyState.Recover);
                break;
                case EnemyState.Recover: Body.linearVelocity = ((Vector2)transform.position - Home).sqrMagnitude > .1f ? (Home - (Vector2)transform.position).normalized * Tuning.moveSpeed : Vector2.zero;
                if (Vector2.Distance(transform.position, Home) < .2f) ChangeState(EnemyState.Patrol);
                break;
                case EnemyState.Chase: ChangeState(EnemyState.Patrol);
                break;
            }
        }
    }
}
