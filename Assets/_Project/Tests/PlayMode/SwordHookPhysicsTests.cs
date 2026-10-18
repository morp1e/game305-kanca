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
    // Kılıç-kanca döngüsünü gerçek fizikle uçtan uca çalıştırır: fırlat, saplan, çekil, fırla.
    // Fare girdisi taklit edilmez; fırlatma nişan hedefi verilerek doğrudan tetiklenir.
    public class SwordHookPhysicsTests
    {
        GameObject floor, player, sword, anchor, cameraObject;
        PlayerMotor motor;
        SwordHook hook;

        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, Private).SetValue(target, value);

        [SetUp]
        public void Build()
        {
            floor = new GameObject("Floor") { layer = LayerMask.NameToLayer("Ground") };
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);

            anchor = new GameObject("Anchor") { layer = LayerMask.NameToLayer("Anchor") };
            anchor.transform.position = new Vector3(6f, 5f, 0f);
            var anchorCollider = anchor.AddComponent<CircleCollider2D>();
            anchorCollider.isTrigger = true;
            anchorCollider.radius = 0.5f;
            anchor.AddComponent<HookAnchor>();

            cameraObject = new GameObject("Camera");
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var cam = cameraObject.AddComponent<Camera>();
            cam.orthographic = true;

            sword = new GameObject("Sword");

            player = new GameObject("Player") { layer = LayerMask.NameToLayer("Player") };
            player.SetActive(false);
            player.transform.position = new Vector3(0f, 1f, 0f);
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<CapsuleCollider2D>().size = new Vector2(0.8f, 1.8f);
            var input = player.AddComponent<PlayerInputReader>();
            motor = player.AddComponent<PlayerMotor>();
            hook = player.AddComponent<SwordHook>();
            SetField(motor, "tuning", ScriptableObject.CreateInstance<PlayerMovementTuning>());
            SetField(motor, "input", input);
            SetField(hook, "tuning", ScriptableObject.CreateInstance<SwordHookTuning>());
            SetField(hook, "input", input);
            SetField(hook, "body", motor);
            SetField(hook, "sword", sword.transform);
            SetField(hook, "aimCamera", cam);
            player.SetActive(true);
        }

        [TearDown]
        public void Clean()
        {
            foreach (var go in new[] { player, sword, anchor, floor, cameraObject }) Object.Destroy(go);
        }

        void ThrowAt(HookAnchor target)
        {
            typeof(SwordHook).GetProperty(nameof(SwordHook.AimTarget)).SetValue(hook, target);
            typeof(SwordHook).GetMethod("ThrowSword", Private).Invoke(hook, null);
        }

        [UnityTest]
        public IEnumerator ThrowStickPullLaunch_FullCycle()
        {
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            Assert.That(motor.IsGrounded, Is.True);

            bool stuck = false, pulled = false, launched = false;
            hook.StateChanged += s =>
            {
                if (s == HookState.Stuck) stuck = true;
                if (s == HookState.Pulling) pulled = true;
                if (s == HookState.InHand && pulled) launched = true;
            };

            ThrowAt(anchor.GetComponent<HookAnchor>());
            Vector2 launchVelocity = Vector2.zero;
            for (int i = 0; i < 100 && !launched; i++)
            {
                yield return new WaitForFixedUpdate();
                if (launched) launchVelocity = motor.Velocity;
            }

            Assert.That(stuck, Is.True, "kılıç anchor'a saplanmadı");
            Assert.That(pulled, Is.True, "çekiş başlamadı");
            Assert.That(launched, Is.True, "fırlama olmadı, kılıç ele dönmedi");
            Assert.That(Vector2.Distance(motor.Position, anchor.transform.position), Is.LessThan(1.5f), "oyuncu anchor'a varmadı");
            Assert.That(launchVelocity.x, Is.GreaterThan(5f), "fırlama yatay ivme vermedi");
            Assert.That(launchVelocity.y, Is.GreaterThan(5f), "fırlama yukarı ivme vermedi");
            Assert.That(motor.ExternalControl, Is.False, "fırlamadan sonra kontrol oyuncuya dönmedi");
        }

        [UnityTest]
        public IEnumerator ThrowAtEmptySpace_ReturnsToHand()
        {
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            ThrowAt(null);
            bool returned = false;
            hook.StateChanged += s => { if (s == HookState.InHand) returned = true; };
            for (int i = 0; i < 150 && !returned; i++) yield return new WaitForFixedUpdate();
            Assert.That(returned, Is.True, "boşa atılan kılıç geri dönmedi");
            Assert.That(motor.IsGrounded, Is.True, "boşa atış oyuncuyu etkiledi");
        }
    }
}
