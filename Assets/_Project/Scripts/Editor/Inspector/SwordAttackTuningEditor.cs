using Game.Combat;
using UnityEditor;

namespace Game.Editor.Inspector
{
    [CustomEditor(typeof(SwordAttack)), CanEditMultipleObjects]
    public sealed class SwordAttackTuningEditor : InlineTuningEditor { }
}
