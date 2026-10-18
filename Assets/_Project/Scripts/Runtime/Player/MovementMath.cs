using UnityEngine;

namespace Game.Player
{
    /// <summary>Unity nesnesi gerektirmeyen hareket adımları.</summary>
    public static class MovementMath
    {
        public static float Approach(float current, float target, float amount)
        {
            if (current < target) return Mathf.Min(current + amount, target);
            return Mathf.Max(current - amount, target);
        }

        public static float StepHorizontal(float velocity, float input, bool grounded, float deltaTime,
            float maxSpeed, float acceleration, float deceleration, float airMultiplier, float overSpeedDecel)
        {
            float control = grounded ? 1f : airMultiplier;
            float accel = acceleration * control * deltaTime;
            float reduce = deceleration * control * deltaTime;
            // Mathf.Sign(0) 1 döndürür; girdi yokken yön 0 olmalı, yoksa karakter kendi kendine sağa koşar.
            float direction = input > 0f ? 1f : input < 0f ? -1f : 0f;

            if (Mathf.Abs(velocity) > maxSpeed)
            {
                if (direction != 0f && direction == Mathf.Sign(velocity))
                    return Approach(velocity, direction * maxSpeed, overSpeedDecel * deltaTime);
                if (direction != 0f && direction != Mathf.Sign(velocity))
                    return Approach(velocity, 0f, reduce);
                return Approach(velocity, 0f, reduce);
            }

            if (direction == 0f) return Approach(velocity, 0f, reduce);
            if (velocity != 0f && Mathf.Sign(velocity) != direction)
                return Approach(velocity, 0f, reduce);
            return Approach(velocity, direction * maxSpeed, accel);
        }

        public static float StepGravity(float velocityY, float deltaTime, bool jumpHeld,
            float gravity, float halfGravityThreshold, float maxFallSpeed)
        {
            float scale = jumpHeld && Mathf.Abs(velocityY) < halfGravityThreshold ? 0.5f : 1f;
            return Mathf.Max(velocityY - gravity * scale * deltaTime, -maxFallSpeed);
        }

        public static float CutVariableJump(float velocityY, bool jumpHeld, float remainingTime)
        {
            return !jumpHeld && remainingTime > 0f && velocityY > 0f ? 0f : velocityY;
        }

        public static Vector2 ApplyJump(Vector2 velocity, bool fromLaunchWindow, float jumpSpeed,
            float launchBonus, float horizontalBoost, float horizontalInput)
        {
            velocity.y = fromLaunchWindow ? Mathf.Max(velocity.y, jumpSpeed) + launchBonus : jumpSpeed;
            if (!fromLaunchWindow && horizontalInput != 0f)
                velocity.x += Mathf.Sign(horizontalInput) * horizontalBoost;
            return velocity;
        }
    }
}
