using UnityEngine;
namespace Game.Enemies
{
    /// <summary>Kenarda dönen, oyuncuya hücum eden düşman.</summary>
    public class WalkerBrain : EnemyBrain
    {
        protected override void TickState()
        {
            switch (State)
            {
                case EnemyState.Patrol: GroundOrWallAhead();
                SetHorizontal(Tuning.moveSpeed);
                break;
                case EnemyState.Notice: FaceTarget();
                SetHorizontal(0f);
                ChangeState(EnemyState.Telegraph);
                break;
                case EnemyState.Telegraph: FaceTarget();
                SetHorizontal(0f);
                if (StateTime >= Tuning.telegraphDuration) ChangeState(EnemyState.Attack);
                break;
                case EnemyState.Attack: FaceTarget();
                SetHorizontal(Tuning.chaseSpeed);
                if (StateTime >= Tuning.attackDuration) ChangeState(EnemyState.Recover);
                break;
                case EnemyState.Recover: SetHorizontal(0f);
                if (StateTime >= Tuning.recoverDuration) ChangeState(EnemyState.Chase);
                break;
                case EnemyState.Chase: FaceTarget();
                SetHorizontal(Tuning.moveSpeed);
                if (!CanSeePlayer()) ChangeState(EnemyState.Patrol);
                else ChangeState(EnemyState.Notice);
                break;
            }
        }
    }
}
