using System.Collections;
using System.Reflection;
using Game.Combat;
using Game.Hook;
using Game.Input;
using Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public class SwordAndPullablePhysicsTests
    {
        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        GameObject player, visual, target, hookVisual, cameraObject;
        PlayerInputReader input;
        PlayerMotor motor;
        GrappleHook hook;
        GrappleHookTuning hookTuning;
        SwordAttack attack;
        SwordTuning swordTuning;
        TrainingDummyTuning dummyTuning;

        static void SetField(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);

        [SetUp]
        public void BuildPlayer()
        {
            cameraObject = new GameObject("AimCamera"); cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true;
            hookVisual = new GameObject("HookVisual");
            player = new GameObject("Player") { layer = LayerMask.NameToLayer("Player") }; player.SetActive(false);
            player.AddComponent<Rigidbody2D>().gravityScale = 0f;
            player.AddComponent<CapsuleCollider2D>().size = new Vector2(.8f, 1.8f);
            input = player.AddComponent<PlayerInputReader>(); motor = player.AddComponent<PlayerMotor>(); hook = player.AddComponent<GrappleHook>();
            visual = new GameObject("HookSprite");
            var movement = ScriptableObject.CreateInstance<PlayerMovementTuning>();
            hookTuning = ScriptableObject.CreateInstance<GrappleHookTuning>(); hookTuning.throwSpeed = 30f; hookTuning.maxRange = 15f;
            swordTuning = ScriptableObject.CreateInstance<SwordTuning>();
            dummyTuning = ScriptableObject.CreateInstance<TrainingDummyTuning>();
            attack = player.AddComponent<SwordAttack>();
            SetField(motor, "tuning", movement); SetField(motor, "input", input);
            SetField(hook, "tuning", hookTuning); SetField(hook, "input", input); SetField(hook, "body", motor); SetField(hook, "hookVisual", hookVisual.transform); SetField(hook, "aimCamera", camera);
            SetField(attack, "tuning", swordTuning); SetField(attack, "input", input); SetField(attack, "body", motor); SetField(attack, "aimCamera", camera);
            player.SetActive(true);
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var go in new[] { player, target, visual, hookVisual, cameraObject }) if (go != null) Object.Destroy(go);
            if (hookTuning != null) Object.Destroy(hookTuning);
            if (swordTuning != null) Object.Destroy(swordTuning);
            if (dummyTuning != null) Object.Destroy(dummyTuning);
        }

        [UnityTest]
        public IEnumerator SwordHitsEnemyAheadOnceAndKnocksItBack()
        {
            target = MakeDummy(new Vector2(1.5f, 0f));
            input.SetPointerWorldOverride(new Vector2(5f, 0f));
            yield return new WaitForFixedUpdate();
            var dummy = target.GetComponent<TrainingDummy>();
            input.TriggerAttackForTest();
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            Assert.That(dummy.Health, Is.EqualTo(2f).Within(.01f), "tek savuruş aynı hedefe birden fazla vurdu");
            Assert.That(target.GetComponent<Rigidbody2D>().linearVelocity.x, Is.GreaterThan(0f), "isabet geri itiş uygulamadı");
        }

        [UnityTest]
        public IEnumerator SwordDoesNotHitEnemyBehindPlayer()
        {
            target = MakeDummy(new Vector2(-1.5f, 0f));
            input.SetPointerWorldOverride(new Vector2(5f, 0f));
            yield return new WaitForFixedUpdate();
            var dummy = target.GetComponent<TrainingDummy>();
            input.TriggerAttackForTest();
            for (int i = 0; i < 25; i++) yield return new WaitForFixedUpdate();
            Assert.That(dummy.Health, Is.EqualTo(3f));
        }

        [UnityTest]
        public IEnumerator SwordHitsTargetNearEndOfVisibleSweep()
        {
            float angle = -46f * Mathf.Deg2Rad;
            target = MakeDummy(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 1.05f);
            input.SetPointerWorldOverride(new Vector2(5f, 0f));
            yield return new WaitForFixedUpdate();
            var dummy = target.GetComponent<TrainingDummy>();
            input.TriggerAttackForTest();
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            Assert.That(dummy.Health, Is.EqualTo(2f).Within(.01f), "görünen yay sonundaki hedef vurulmadı");
        }

        [UnityTest]
        public IEnumerator LightCrateIsReeledTowardPlayer()
        {
            target = MakePullable("Crate", new Vector2(3f, 0f), HookWeightClass.Light, false);
            input.SetPointerWorldOverride(new Vector2(8f, 0f));
            yield return new WaitForFixedUpdate(); FireHook();
            for (int i = 0; i < 240 && hook.State != HookState.InHand; i++) yield return new WaitForFixedUpdate();
            Assert.That(target.transform.position.x, Is.LessThan(3f));
            Assert.That(motor.ExternalControl, Is.False);
        }

        [UnityTest]
        public IEnumerator HeavyDummyPullsPlayerAndLaunches()
        {
            target = MakePullable("HeavyDummy", new Vector2(5f, 0f), HookWeightClass.Heavy, true);
            input.SetPointerWorldOverride(new Vector2(8f, 0f));
            yield return new WaitForFixedUpdate(); FireHook();
            bool launched = false;
            hook.StateChanged += state => { if (state == HookState.InHand && motor.Velocity.x > 0f) launched = true; };
            for (int i = 0; i < 160 && !launched; i++) yield return new WaitForFixedUpdate();
            Assert.That(launched, Is.True);
            Assert.That(player.transform.position.x, Is.GreaterThan(0f));
        }

        [UnityTest]
        public IEnumerator RecallDuringReelingReturnsHook()
        {
            target = MakePullable("Crate", new Vector2(8f, 0f), HookWeightClass.Light, false);
            input.SetPointerWorldOverride(new Vector2(10f, 0f));
            yield return new WaitForFixedUpdate(); FireHook();
            for (int i = 0; i < 50 && hook.State != HookState.Reeling; i++) yield return new WaitForFixedUpdate();
            Assert.That(hook.State, Is.EqualTo(HookState.Reeling));
            SetField(input, "throwPressedAt", Time.realtimeSinceStartupAsDouble);
            for (int i = 0; i < 100 && hook.State != HookState.InHand; i++) yield return new WaitForFixedUpdate();
            Assert.That(hook.State, Is.EqualTo(HookState.InHand));
        }

        GameObject MakeDummy(Vector2 position)
        {
            var go = new GameObject("TrainingDummy") { layer = LayerMask.NameToLayer("Enemy") }; go.transform.position = position;
            go.AddComponent<Rigidbody2D>().gravityScale = 0f; go.AddComponent<BoxCollider2D>().size = Vector2.one;
            go.AddComponent<SpriteRenderer>(); var dummy = go.AddComponent<TrainingDummy>(); SetField(dummy, "tuning", dummyTuning); return go;
        }

        GameObject MakePullable(string name, Vector2 position, HookWeightClass weight, bool damageable)
        {
            var go = new GameObject(name) { layer = damageable ? LayerMask.NameToLayer("Enemy") : 0 }; go.transform.position = position;
            var rb = go.AddComponent<Rigidbody2D>(); rb.gravityScale = 0f; rb.bodyType = weight == HookWeightClass.Heavy ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
            go.AddComponent<BoxCollider2D>().size = Vector2.one; var pullable = go.AddComponent<HookPullable>();
            SetField(pullable, "weightClass", weight); SetField(pullable, "body", rb);
            if (damageable) { var dummy = go.AddComponent<TrainingDummy>(); SetField(dummy, "tuning", dummyTuning); } return go;
        }

        void FireHook() => typeof(GrappleHook).GetMethod("FireHook", Private).Invoke(hook, null);
    }
}
