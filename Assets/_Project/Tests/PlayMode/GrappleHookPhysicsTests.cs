using System.Collections;
using System.Reflection;
using Game.Hook;
using Game.Input;
using Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public class GrappleHookPhysicsTests
    {
        GameObject floor, wall, player, hookVisual, cameraObject;
        PlayerInputReader input;
        PlayerMotor motor;
        GrappleHook hook;
        GrappleHookTuning tuning;

        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, Private).SetValue(target, value);

        [SetUp]
        public void Build()
        {
            floor = new GameObject("Floor") { layer = LayerMask.NameToLayer("Ground") };
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);

            wall = new GameObject("GroundWall") { layer = LayerMask.NameToLayer("Ground") };
            wall.transform.position = new Vector3(6f, 4f, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, 8f);

            cameraObject = new GameObject("Camera");
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var cam = cameraObject.AddComponent<Camera>();
            cam.orthographic = true;

            hookVisual = new GameObject("GrappleHookVisual");
            player = new GameObject("Player") { layer = LayerMask.NameToLayer("Player") };
            player.SetActive(false);
            player.transform.position = new Vector3(0f, 1f, 0f);
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<CapsuleCollider2D>().size = new Vector2(0.8f, 1.8f);
            input = player.AddComponent<PlayerInputReader>();
            motor = player.AddComponent<PlayerMotor>();
            hook = player.AddComponent<GrappleHook>();
            tuning = ScriptableObject.CreateInstance<GrappleHookTuning>();
            SetField(motor, "tuning", ScriptableObject.CreateInstance<PlayerMovementTuning>());
            SetField(motor, "input", input);
            SetField(hook, "tuning", tuning);
            SetField(hook, "input", input);
            SetField(hook, "body", motor);
            SetField(hook, "hookVisual", hookVisual.transform);
            SetField(hook, "aimCamera", cam);
            player.SetActive(true);
        }

        [TearDown]
        public void Clean()
        {
            foreach (var go in new[] { player, hookVisual, wall, floor, cameraObject }) Object.Destroy(go);
            Object.Destroy(tuning);
        }

        void PointAt(Vector2 point)
        {
            var screen = ((Camera)cameraObject.GetComponent<Camera>()).WorldToScreenPoint(point);
            SetField(input, "<PointerScreen>k__BackingField", (Vector2)screen);
        }

        void FireAt(Vector2 point)
        {
            PointAt(point);
            typeof(GrappleHook).GetMethod("FireHook", Private).Invoke(hook, null);
        }

        void PressFireAgain()
        {
            SetField(input, "throwPressedAt", Time.realtimeSinceStartupAsDouble);
        }

        [UnityTest]
        public IEnumerator GroundWall_SticksPullsAndLaunchesAtSurface()
        {
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            FireAt(new Vector2(12f, 1f));
            bool stuck = false, pulling = false, launched = false;
            Vector2 launchVelocity = Vector2.zero;
            hook.StateChanged += state =>
            {
                if (state == HookState.Stuck) stuck = true;
                if (state == HookState.Pulling) pulling = true;
                if (state == HookState.InHand && pulling) { launched = true; launchVelocity = motor.Velocity; }
            };
            for (int i = 0; i < 120 && !launched; i++) yield return new WaitForFixedUpdate();
            Assert.That(stuck, Is.True, "kanca Ground duvarına saplanmadı");
            Assert.That(pulling, Is.True, "çekiş başlamadı");
            Assert.That(launched, Is.True, "duvar teması fırlamayla bitmedi");
            Assert.That(motor.Position.x, Is.LessThan(5.2f), "oyuncu duvara ulaşıp fırlamadı");
            Assert.That(launchVelocity.x, Is.GreaterThan(5f), "fırlama son çekiş yönünü korumadı");
            Assert.That(motor.ExternalControl, Is.False);
        }

        [UnityTest]
        public IEnumerator NonHookableGroundCollider_ReturnsWithoutPulling()
        {
            wall.AddComponent<NonHookable>();
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            FireAt(new Vector2(12f, 1f));
            bool pulled = false;
            hook.StateChanged += state => { if (state == HookState.Pulling) pulled = true; };
            for (int i = 0; i < 100 && hook.State != HookState.InHand; i++) yield return new WaitForFixedUpdate();
            Assert.That(hook.State, Is.EqualTo(HookState.InHand));
            Assert.That(pulled, Is.False);
            Assert.That(motor.ExternalControl, Is.False);
        }

        [UnityTest]
        public IEnumerator EmptySpace_ReturnsAfterMaxRange()
        {
            tuning.maxRange = 4f;
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            FireAt(new Vector2(12f, 1f));
            bool returned = false;
            hook.StateChanged += state => { if (state == HookState.Returning) returned = true; };
            for (int i = 0; i < 100 && hook.State != HookState.InHand; i++) yield return new WaitForFixedUpdate();
            Assert.That(returned, Is.True, "menzil sonunda geri dönüş başlamadı");
            Assert.That(hook.State, Is.EqualTo(HookState.InHand));
            Assert.That(motor.ExternalControl, Is.False);
        }

        [UnityTest]
        public IEnumerator FireAgainWhileStuck_RecallsHook()
        {
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            FireAt(new Vector2(12f, 1f));
            for (int i = 0; i < 100 && hook.State != HookState.Stuck; i++) yield return new WaitForFixedUpdate();
            Assert.That(hook.State, Is.EqualTo(HookState.Stuck));
            PressFireAgain();
            for (int i = 0; i < 100 && hook.State != HookState.InHand; i++) yield return new WaitForFixedUpdate();
            Assert.That(hook.State, Is.EqualTo(HookState.InHand));
            Assert.That(motor.ExternalControl, Is.False);
        }

        [UnityTest]
        public IEnumerator RigCursorLeftSetsFacingLeftWhileMovingRight()
        {
            var rigObject = new GameObject("ArmRig");
            var bodySprite = new GameObject("Body").transform; bodySprite.SetParent(player.transform, false);
            var hookPivot = MakeArm("HookArm", player.transform);
            var swordPivot = MakeArm("SwordArm", player.transform);
            var armRig = rigObject.AddComponent<PlayerArmRig>();
            SetField(armRig, "tuning", ScriptableObject.CreateInstance<PlayerRigTuning>());
            SetField(armRig, "bodySprite", bodySprite);
            SetField(armRig, "hookArmPivot", hookPivot);
            SetField(armRig, "swordArmPivot", swordPivot);
            SetField(armRig, "input", input);
            SetField(armRig, "aimCamera", cameraObject.GetComponent<Camera>());
            SetField(motor, "armRig", armRig);
            input.SetMoveXOverride(1f);
            PointAt(new Vector2(-4f, 1f));
            for (int i = 0; i < 3; i++) yield return null;
            Assert.That(input.MoveX, Is.EqualTo(1f));
            Assert.That(motor.Facing, Is.EqualTo(-1));
            Object.Destroy(rigObject);
        }

        [UnityTest]
        public IEnumerator RigHookLaunchesFromHandTip()
        {
            var rigObject = new GameObject("ArmRig");
            var bodySprite = new GameObject("Body").transform; bodySprite.SetParent(player.transform, false);
            var hookPivot = MakeArm("HookArm", player.transform);
            var swordPivot = MakeArm("SwordArm", player.transform);
            hookPivot.localPosition = new Vector3(.4f, .5f, 0f);
            hookPivot.Find("HandTip").localPosition = new Vector3(.8f, 0f, 0f);
            var armRig = rigObject.AddComponent<PlayerArmRig>();
            SetField(armRig, "tuning", ScriptableObject.CreateInstance<PlayerRigTuning>());
            SetField(armRig, "bodySprite", bodySprite);
            SetField(armRig, "hookArmPivot", hookPivot);
            SetField(armRig, "swordArmPivot", swordPivot);
            SetField(armRig, "input", input);
            SetField(armRig, "aimCamera", cameraObject.GetComponent<Camera>());
            SetField(hook, "armRig", armRig);
            PointAt(new Vector2(8f, 1f));
            yield return null;
            Vector3 expected = hookPivot.Find("HandTip").position;
            typeof(GrappleHook).GetMethod("FireHook", Private).Invoke(hook, null);
            Assert.That(Vector2.Distance(hookVisual.transform.position, expected), Is.LessThan(.001f));
            Object.Destroy(rigObject);
        }

        static Transform MakeArm(string name, Transform parent)
        {
            var pivot = new GameObject(name).transform; pivot.SetParent(parent, false);
            var sprite = new GameObject("Sprite"); sprite.transform.SetParent(pivot, false); sprite.AddComponent<SpriteRenderer>();
            var tip = new GameObject("HandTip"); tip.transform.SetParent(pivot, false); tip.transform.localPosition = Vector3.right;
            return pivot;
        }
    }
}
