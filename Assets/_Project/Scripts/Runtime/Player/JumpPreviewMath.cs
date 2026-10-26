namespace Game.Player
{
    /// <summary>Oyuncu zıplamasının çarpışmasız dikey tahmini.</summary>
    public static class JumpPreviewMath
    {
        public static float SimulateHeight(float jumpSpeed, float gravity, float halfGravityThreshold,
            float maxFallSpeed, float variableJumpTime, float coyoteTime, float jumpBufferTime,
            float fixedDeltaTime, bool jumpHeld)
        {
            if (fixedDeltaTime <= 0f || jumpSpeed <= 0f || gravity < 0f || maxFallSpeed < 0f)
                return 0f;

            var jumpGate = new JumpGate();
            jumpGate.Tick(fixedDeltaTime, true, true, coyoteTime, jumpBufferTime);
            if (!jumpGate.TryConsumeJump(out bool fromLaunchWindow) || fromLaunchWindow) return 0f;

            float velocityY = jumpSpeed;
            float remaining = jumpHeld ? variableJumpTime : 0f;
            float height = 0f;
            float peak = 0f;
            int maxSteps = (int)(30f / fixedDeltaTime);

            for (int i = 0; i < maxSteps && (velocityY > 0f || height > 0f); i++)
            {
                velocityY = MovementMath.StepGravity(velocityY, fixedDeltaTime, jumpHeld,
                    gravity, halfGravityThreshold, maxFallSpeed);
                if (remaining > 0f)
                {
                    velocityY = MovementMath.ApplyVariableJump(velocityY, jumpHeld, jumpSpeed, remaining);
                    if (jumpHeld) remaining = UnityEngine.Mathf.Max(0f, remaining - fixedDeltaTime);
                }

                height += velocityY * fixedDeltaTime;
                peak = UnityEngine.Mathf.Max(peak, height);
            }

            return peak;
        }

        public static float SimulateTimeToApex(float jumpSpeed, float gravity, float halfGravityThreshold,
            float maxFallSpeed, float variableJumpTime, float coyoteTime, float jumpBufferTime,
            float fixedDeltaTime)
        {
            if (fixedDeltaTime <= 0f || jumpSpeed <= 0f || gravity <= 0f) return 0f;
            var jumpGate = new JumpGate();
            jumpGate.Tick(fixedDeltaTime, true, true, coyoteTime, jumpBufferTime);
            if (!jumpGate.TryConsumeJump(out bool fromLaunchWindow) || fromLaunchWindow) return 0f;
            float velocityY = jumpSpeed;
            float remaining = variableJumpTime;
            float elapsed = 0f;
            int maxSteps = (int)(30f / fixedDeltaTime);
            for (int i = 0; i < maxSteps && velocityY > 0f; i++)
            {
                velocityY = MovementMath.StepGravity(velocityY, fixedDeltaTime, true,
                    gravity, halfGravityThreshold, maxFallSpeed);
                if (remaining > 0f)
                {
                    velocityY = MovementMath.ApplyVariableJump(velocityY, true, jumpSpeed, remaining);
                    remaining = UnityEngine.Mathf.Max(0f, remaining - fixedDeltaTime);
                }
                elapsed += fixedDeltaTime;
            }
            return elapsed;
        }
    }
}
