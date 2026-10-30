using Game.Player;
using UnityEditor;

namespace Game.Editor.Inspector
{
    [CustomEditor(typeof(PlayerHealth)), CanEditMultipleObjects]
    public sealed class PlayerHealthTuningEditor : InlineTuningEditor { }
}
