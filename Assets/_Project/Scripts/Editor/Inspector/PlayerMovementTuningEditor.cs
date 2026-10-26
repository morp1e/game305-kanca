using Game.Player;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.Inspector
{
    [CustomEditor(typeof(PlayerMovementTuning))]
    public sealed class PlayerMovementTuningEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            var tuning = (PlayerMovementTuning)target;
            float dt = Time.fixedDeltaTime;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Hesaplanan", EditorStyles.boldLabel);
            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.FloatField("Tam basılı zıplama yüksekliği (karo = unit)", Height(tuning, dt, true));
                EditorGUILayout.FloatField("Kısa dokunuş zıplama yüksekliği (karo = unit)", Height(tuning, dt, false));
                EditorGUILayout.FloatField("Tepeye çıkış süresi (s)", JumpPreviewMath.SimulateTimeToApex(tuning.JumpSpeed, tuning.Gravity,
                    tuning.HalfGravThreshold, tuning.MaxFallSpeed, tuning.VarJumpTime, tuning.CoyoteTime,
                    tuning.JumpBufferTime, dt));
                EditorGUILayout.FloatField("Koşu tepe hızına ulaşma süresi (s)", tuning.RunAccel > 0f ? tuning.MaxRunSpeed / tuning.RunAccel : 0f);
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4f);
            DrawDefaultInspector();
        }

        static float Height(PlayerMovementTuning tuning, float dt, bool held) =>
            JumpPreviewMath.SimulateHeight(tuning.JumpSpeed, tuning.Gravity, tuning.HalfGravThreshold,
                tuning.MaxFallSpeed, tuning.VarJumpTime, tuning.CoyoteTime, tuning.JumpBufferTime, dt, held);
    }
}
