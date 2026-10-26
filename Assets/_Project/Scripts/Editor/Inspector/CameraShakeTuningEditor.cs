using Game.CameraRig;
using UnityEditor;

namespace Game.Editor.Inspector
{
    [CustomEditor(typeof(CameraShake)), CanEditMultipleObjects]
    public sealed class CameraShakeTuningEditor : InlineTuningEditor { }
}
