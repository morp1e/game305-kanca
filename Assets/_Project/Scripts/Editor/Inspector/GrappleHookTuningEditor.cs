using Game.Hook;
using UnityEditor;

namespace Game.Editor.Inspector
{
    [CustomEditor(typeof(GrappleHook)), CanEditMultipleObjects]
    public sealed class GrappleHookComponentEditor : InlineTuningEditor { }
}
