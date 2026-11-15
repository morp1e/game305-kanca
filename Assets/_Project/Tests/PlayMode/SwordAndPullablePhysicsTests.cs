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
            Time.timeScale = 1f;
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
            foreach (var service in Object.FindObjectsByType<HitstopService>(FindObjectsSortMode.None)) Object.DestroyImmediate(service);
            Time.timeScale = 1f;
            if (hookTuning != null) Object.Destroy(hookTuning);
            if (swordTuning != null) Object.Destroy(swordTuning);
            if (dummyTuning != null) Object.Destroy(dummyTuning);
        }

        [UnityTest]
        public IEnumerator SwordHitsEnemyAheadOnceAndKnocksItBack()
        {
            target = MakeDummy(new Vector2(1.5f, 0f));
            motor.ExternalControl = true;
            Vector2 playerVelocity = new Vector2(2f, 1f);
            motor.Velocity = playerVelocity;
            input.SetPointerWorldOverride(new Vector2(5f, 0f));
            yield return new WaitForFixedUpdate();
            var dummy = target.GetComponent<TrainingDummy>();
            bool sawHitDuringStop = false;
            Vector2 velocityAfterStop = Vector2.zero;
            attack.HitOccurred += (point, direction, killed) =>
            {
                sawHitDuringStop = HitstopService.Instance != null && HitstopService.Instance.IsActive && target.GetComponent<Rigidbody2D>().linearVelocity == Vector2.zero;
            };
            HitstopService.Instance.Ended += () => velocityAfterStop = target.GetComponent<Rigidbody2D>().linearVelocity;
            input.TriggerAttackForTest();
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            Assert.That(dummy.Health, Is.EqualTo(2f).Within(.01f), "tek savuruş aynı hedefe birden fazla vurdu");
            Assert.IsTrue(sawHitDuringStop, "vuruş ortak hitstop sırasında bildirilmeli");
            Assert.That(velocityAfterStop.x, Is.GreaterThan(0f), "geri itme hitstop bittikten sonra uygulanmalı");
            Assert.That(target.GetComponent<Rigidbody2D>().linearVelocity.x, Is.GreaterThan(0f), "isabet geri itiş uygulamadı");
            Assert.AreEqual(playerVelocity, motor.Velocity, "oyuncu hızı hitstop bitince aynı kalmalı");
        }

        [UnityTest]
        public IEnumerator SwordSwingLeavesPlayerVelocityUnchanged()
        {
            motor.ExternalControl = true;
            Vector2 startingVelocity = new Vector2(3f, -2f);
            motor.Velocity = startingVelocity;
            input.SetPointerWorldOverride(new Vector2(5f, 0f));
            input.TriggerAttackForTest();
            for (int i = 0; i < 45; i++) yield return new WaitForFixedUpdate();
            Assert.AreEqual(startingVelocity, motor.Velocity);
        }

        [UnityTest]
        public IEnumerator HitstopPreservesVelocityChangedByAnotherSystem()
        {
            target = MakeDummy(new Vector2(1.5f, 0f));
            swordTuning.comboSteps[0].windup = 0f;
            swordTuning.comboSteps[0].active = 0.12f;
            swordTuning.comboSteps[0].hitstop = 0.12f;
            motor.ExternalControl = true;
            Vector2 changedVelocity = new Vector2(-4f, 2.5f);
            bool hitDuringStop = false;
            input.SetPointerWorldOverride(new Vector2(5f, 0f));
            attack.HitOccurred += (point, direction, killed) =>
            {
                if (HitstopService.Instance != null && HitstopService.Instance.IsActive)
                { hitDuringStop = true; motor.Velocity = changedVelocity; }
            };
            input.TriggerAttackForTest();
            for (int i = 0; i < 50 && !hitDuringStop; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(hitDuringStop, "isabet sırasında hitstop etkin olmalı");
            for (int i = 0; i < 50 && HitstopService.Instance.IsActive; i++)
                yield return new WaitForFixedUpdate();
            Assert.AreEqual(changedVelocity, motor.Velocity, "hitstop sırasında başka sistemin yazdığı hız ezilmemeli");
        }

        [UnityTest]
        public IEnumerator JumpCancelsSwordRecovery()
        {
            swordTuning.comboSteps[0].windup = 0f;
            swordTuning.comboSteps[0].active = 0.01f;
            swordTuning.comboSteps[0].recovery = 1f;
            swordTuning.comboStartInterval = 0f;
            input.TriggerAttackForTest();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(attack.IsSwinging);
            input.TriggerJumpForTest();
            yield return new WaitForFixedUpdate();
            Assert.IsFalse(attack.IsSwinging, "zıplama toparlanmayı kesmeli");
        }

        [UnityTest]
        public IEnumerator HookThrowCancelsSwordRecovery()
        {
            swordTuning.comboSteps[0].windup = 0f;
            swordTuning.comboSteps[0].active = 0.01f;
            swordTuning.comboSteps[0].recovery = 1f;
            swordTuning.comboStartInterval = 0f;
            input.TriggerAttackForTest();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();
            Assert.IsTrue(attack.IsSwinging);
            FireHook();
            Assert.IsFalse(attack.IsSwinging, "kanca fırlatması toparlanmayı kesmeli");
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
        public IEnumerator SwordHitsDamageableDefaultLayerTarget()
        {
            target = MakeDummy(new Vector2(1.5f, 0f), LayerMask.NameToLayer("Default"));
            swordTuning.comboSteps[0].windup = 0f;
            swordTuning.comboSteps[0].active = 0.02f;
            input.SetPointerWorldOverride(new Vector2(5f, 0f));
            yield return new WaitForFixedUpdate();
            var dummy = target.GetComponent<TrainingDummy>();
            bool hit = false;
            attack.HitOccurred += (point, direction, killed) => hit = true;
            input.TriggerAttackForTest();
            for (int i = 0; i < 40 && !hit; i++) yield return new WaitForFixedUpdate();
            Assert.IsTrue(hit, "varsayılan katmandaki IDamageable hedef vurulmalı");
            Assert.That(dummy.Health, Is.EqualTo(2f).Within(0.01f));
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

        GameObject MakeDummy(Vector2 position, int layer = -1)
        {
            var go = new GameObject("TrainingDummy") { layer = layer < 0 ? LayerMask.NameToLayer("Enemy") : layer }; go.transform.position = position;
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
