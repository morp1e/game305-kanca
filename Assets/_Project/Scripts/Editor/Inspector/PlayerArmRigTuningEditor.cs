using Game.Player;
using UnityEditor;

namespace Game.Editor.Inspector
{
    [CustomEditor(typeof(PlayerArmRig)), CanEditMultipleObjects]
    public sealed class PlayerArmRigTuningEditor : InlineTuningEditor { }
}
