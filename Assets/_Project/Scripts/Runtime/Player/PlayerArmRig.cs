using Game.Input;
using UnityEngine;

namespace Game.Player
{
    /// <summary>Gövdeye göre iki kolu fareye yöneltir. Kol sprite'ları +X yönünde çizilir.</summary>
    public class PlayerArmRig : MonoBehaviour
    {
        [SerializeField] PlayerRigTuning tuning;
        [SerializeField] Transform bodySprite;
        [SerializeField] Transform hookArmPivot;
        [SerializeField] Transform swordArmPivot;
        [SerializeField] PlayerInputReader input;
        [SerializeField] Camera aimCamera;

        float? swordOverride;
        float hookAngle;
        float swordAngle;
        bool initialized;

        public int Facing { get; private set; } = 1;
        public Transform HookHandTip => hookArmPivot != null ? hookArmPivot.Find("HandTip") : null;
        public Transform SwordHandTip => swordArmPivot != null ? swordArmPivot.Find("HandTip") : null;

        void Update()
        {
            if (tuning == null || input == null || aimCamera == null || hookArmPivot == null || swordArmPivot == null) return;
            Vector2 cursor = input.PointerWorld(aimCamera);
            Vector2 center = bodySprite != null ? (Vector2)bodySprite.position : (Vector2)transform.position;
            Facing = ArmAimMath.Facing(center, cursor, tuning.facingDeadZone, Facing);
            hookArmPivot.position = Shoulder(center, tuning.backShoulderOffset);
            swordArmPivot.position = Shoulder(center, tuning.frontShoulderOffset);
            // Sola bakarken Y aynalanır: dönüş dünya açısıyla kalır, sprite baş aşağı görünmez.
            hookArmPivot.localScale = new Vector3(1f, Facing, 1f);
            swordArmPivot.localScale = new Vector3(1f, Facing, 1f);

            float hookWorld = ArmAimMath.SnapAngle(ArmAimMath.Angle(hookArmPivot.position, cursor), tuning.angleSnapDegrees);
            float swordWorld = swordOverride ?? ArmAimMath.Angle(swordArmPivot.position, cursor);
            if (!swordOverride.HasValue && Mathf.Abs(cursor.x - center.x) <= tuning.facingDeadZone)
                swordWorld = Facing > 0 ? tuning.swordRestAngle : ArmAimMath.MirrorAngle(tuning.swordRestAngle);
            swordWorld = ArmAimMath.SnapAngle(swordWorld, tuning.angleSnapDegrees);
            hookAngle = LocalAngle(hookWorld);
            swordAngle = LocalAngle(swordWorld);
            if (initialized)
            {
                float alpha = tuning.armRotationSmoothing <= 0f ? 1f : 1f - Mathf.Exp(-tuning.armRotationSmoothing * Time.deltaTime);
                hookAngle = Mathf.LerpAngle(hookArmPivot.eulerAngles.z, hookAngle, alpha);
                swordAngle = Mathf.LerpAngle(swordArmPivot.eulerAngles.z, swordAngle, alpha);
            }
            hookArmPivot.rotation = Quaternion.Euler(0f, 0f, hookAngle);
            swordArmPivot.rotation = Quaternion.Euler(0f, 0f, swordAngle);
            initialized = true;
            SetSorting();
        }

        public void SetSwordArmOverride(float? angle) => swordOverride = angle;

        Vector2 Shoulder(Vector2 center, Vector2 offset) => center + new Vector2(offset.x * Facing, offset.y);
        static float LocalAngle(float worldAngle) => worldAngle;

        void SetSorting()
        {
            var hookRenderer = hookArmPivot.GetComponentInChildren<SpriteRenderer>();
            var swordRenderer = swordArmPivot.GetComponentInChildren<SpriteRenderer>();
            var bodyRenderer = bodySprite != null ? bodySprite.GetComponent<SpriteRenderer>() : null;
            if (Facing > 0) { Sort(hookRenderer, 0); Sort(bodyRenderer, 1); Sort(swordRenderer, 2); }
            else { Sort(swordRenderer, 0); Sort(bodyRenderer, 1); Sort(hookRenderer, 2); }
        }

        static void Sort(SpriteRenderer renderer, int order) { if (renderer != null) renderer.sortingOrder = order; }
    }
}
