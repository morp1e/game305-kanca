using UnityEngine;

namespace Game.Player
{
    /// <summary>Kol nişanlama için saf, sahneden bağımsız matematik.</summary>
    public static class ArmAimMath
    {
        public static float Angle(Vector2 shoulder, Vector2 target) => Mathf.Atan2(target.y - shoulder.y, target.x - shoulder.x) * Mathf.Rad2Deg;

        public static float SnapAngle(float angle, float stepDegrees)
        {
            if (stepDegrees <= 0f) return Mathf.Repeat(angle + 180f, 360f) - 180f;
            return Mathf.Repeat(Mathf.Round(angle / stepDegrees) * stepDegrees + 180f, 360f) - 180f;
        }

        public static int Facing(Vector2 bodyCenter, Vector2 target, float deadZone, int previousFacing)
        {
            float dx = target.x - bodyCenter.x;
            if (Mathf.Abs(dx) <= Mathf.Max(0f, deadZone)) return previousFacing < 0 ? -1 : 1;
            return dx < 0f ? -1 : 1;
        }

        /// <summary>X aynalaması altında yerel sprite açısını dünya açısına çevirir.</summary>
        public static float MirrorAngle(float angle) => SnapAngle(180f - angle, 0f);

        public static float ShortestAngle(float from, float to) => Mathf.DeltaAngle(from, to);
    }
}
