using UnityEngine;

namespace Game.Player
{
    // Celeste Player.cs (MIT, (c) 2018 Noel Berry) uyarlaması; bkz. tools/reference/celeste/LICENSE
    /// <summary>Unity nesnesi gerektirmeyen hareket adımları.</summary>
    public static class MovementMath
    {
        public static float Approach(float current, float target, float amount)
        {
            if (current < target) return Mathf.Min(current + amount, target);
            return Mathf.Max(current - amount, target);
        }

        public static float StepHorizontal(float velocity, float input, bool grounded, float deltaTime,
            float maxSpeed, float acceleration, float deceleration, float airMultiplier, float overSpeedDecel,
            bool preserveLaunchMomentum = false)
        {
            float control = grounded ? 1f : airMultiplier;
            float accel = acceleration * control * deltaTime;
            float reduce = deceleration * control * deltaTime;
            // Mathf.Sign(0) 1 döndürür; girdi yokken yön 0 olmalı, yoksa karakter kendi kendine sağa koşar.
            float direction = input > 0f ? 1f : input < 0f ? -1f : 0f;

            if (Mathf.Abs(velocity) > maxSpeed && direction != 0f && direction == Mathf.Sign(velocity))
                return Approach(velocity, maxSpeed * direction, reduce);

            // Hook launch momentum intentionally decays slowly without input instead of stopping at once.
            if (Mathf.Abs(velocity) > maxSpeed && direction == 0f && preserveLaunchMomentum)
            {
                return Approach(velocity, 0f, deceleration * control * deltaTime);
            }

            return Approach(velocity, direction * maxSpeed, accel);
        }

        public static float StepGravity(float velocityY, float deltaTime, bool jumpHeld,
            float gravity, float halfGravityThreshold, float maxFallSpeed, float fastMaxFallSpeed = 0f,
            float fastMaxAccel = 0f, float currentFallCap = 0f, bool fastFall = false)
        {
            float scale = jumpHeld && Mathf.Abs(velocityY) < halfGravityThreshold ? 0.5f : 1f;
            float cap = currentFallCap > 0f ? currentFallCap : maxFallSpeed;
            if (fastMaxFallSpeed > 0f && fastMaxAccel > 0f)
                cap = Approach(cap, fastFall ? fastMaxFallSpeed : maxFallSpeed, fastMaxAccel * deltaTime);
            return Mathf.Max(velocityY - gravity * scale * deltaTime, -cap);
        }

        public static float ApplyVariableJump(float velocityY, bool jumpHeld, float varJumpSpeed, float remainingTime)
        {
            return !jumpHeld && remainingTime > 0f ? velocityY : (jumpHeld && remainingTime > 0f ? Mathf.Max(velocityY, varJumpSpeed) : velocityY);
        }

        public static float WallSlideCap(float maxFall, float slideStartMax, float timer, float slideTime) =>
            Mathf.Lerp(maxFall, slideStartMax, Mathf.Clamp01(timer / slideTime));

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
