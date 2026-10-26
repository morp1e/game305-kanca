using Game.Player;
using UnityEditor;

namespace Game.Editor.Inspector
{
    [CustomEditor(typeof(PlayerMotor)), CanEditMultipleObjects]
    public sealed class PlayerMotorTuningEditor : InlineTuningEditor { }
}
