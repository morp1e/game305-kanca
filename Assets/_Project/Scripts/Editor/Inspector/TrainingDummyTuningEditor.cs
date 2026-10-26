using Game.Combat;
using UnityEditor;

namespace Game.Editor.Inspector
{
    [CustomEditor(typeof(TrainingDummy)), CanEditMultipleObjects]
    public sealed class TrainingDummyTuningEditor : InlineTuningEditor { }
}
