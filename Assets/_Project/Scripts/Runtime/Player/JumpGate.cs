namespace Game.Player
{
    /// <summary>Coyote, input buffer ve tek kullanımlık fırlatma zıplaması zamanlaması.</summary>
    public sealed class JumpGate
    {
        float coyoteRemaining;
        float bufferRemaining;
        float launchRemaining;

        public bool LaunchWindowActive => launchRemaining > 0f;

        public void Tick(float deltaTime, bool grounded, bool jumpPressed, float coyoteTime, float bufferTime)
        {
            if (grounded) coyoteRemaining = coyoteTime;
            else coyoteRemaining = Decrease(coyoteRemaining, deltaTime);

            bufferRemaining = Decrease(bufferRemaining, deltaTime);
            if (jumpPressed) bufferRemaining = bufferTime;
            launchRemaining = Decrease(launchRemaining, deltaTime);
        }

        public void OpenLaunchWindow(float seconds)
        {
            launchRemaining = seconds;
        }

        public bool TryConsumeJump(out bool fromLaunchWindow)
        {
            fromLaunchWindow = false;
            if (bufferRemaining <= 0f) return false;

            if (launchRemaining > 0f)
            {
                launchRemaining = 0f;
                bufferRemaining = 0f;
                coyoteRemaining = 0f;
                fromLaunchWindow = true;
                return true;
            }

            if (coyoteRemaining <= 0f) return false;
            bufferRemaining = 0f;
            coyoteRemaining = 0f;
            return true;
        }

        static float Decrease(float value, float amount)
        {
            return value > 0f ? (value - amount > 0f ? value - amount : 0f) : 0f;
        }
    }
}
