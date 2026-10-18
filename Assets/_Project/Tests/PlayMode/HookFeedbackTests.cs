using System.Collections;
using System.Reflection;
using Game.Core;
using Game.Feedback;
using Game.Hook;
using Game.Input;
using Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    public class HookFeedbackTests
    {
        GameObject floor, player, sword, anchor, cameraObject;
        PlayerMotor motor;
        SwordHook hook;
        HookFeedback feedback;
        TrailRenderer trail;
        Vector2Event kickEvent;
        int kickCount;

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
            var anchorComponent = anchor.AddComponent<HookAnchor>();

            cameraObject = new GameObject("Camera");
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var cam = cameraObject.AddComponent<Camera>();
            cam.orthographic = true;

            sword = new GameObject("Sword");
            kickEvent = ScriptableObject.CreateInstance<Vector2Event>();
            kickEvent.Register(_ => kickCount++);

            player = new GameObject("Player") { layer = LayerMask.NameToLayer("Player") };
            player.SetActive(false);
            player.transform.position = new Vector3(0f, 1f, 0f);
            player.AddComponent<Rigidbody2D>();
            player.AddComponent<CapsuleCollider2D>().size = new Vector2(0.8f, 1.8f);
            var input = player.AddComponent<PlayerInputReader>();
            motor = player.AddComponent<PlayerMotor>();
            hook = player.AddComponent<SwordHook>();
            feedback = player.AddComponent<HookFeedback>();
            SetField(motor, "tuning", ScriptableObject.CreateInstance<PlayerMovementTuning>());
            SetField(motor, "input", input);
            SetField(hook, "tuning", ScriptableObject.CreateInstance<SwordHookTuning>());
            SetField(hook, "input", input);
            SetField(hook, "body", motor);
            SetField(hook, "sword", sword.transform);
            SetField(hook, "aimCamera", cam);
            SetField(feedback, "hook", hook);
            SetField(feedback, "sword", sword.transform);
            SetField(feedback, "kickEvent", kickEvent);
            player.SetActive(true);
            trail = feedback.GetComponentInChildren<TrailRenderer>();
        }

        [TearDown]
        public void Clean()
        {
            foreach (var go in new[] { player, sword, anchor, floor, cameraObject }) Object.Destroy(go);
            if (kickEvent != null) Object.Destroy(kickEvent);
        }

        void ThrowAt(HookAnchor target)
        {
            typeof(SwordHook).GetProperty(nameof(SwordHook.AimTarget)).SetValue(hook, target);
            typeof(SwordHook).GetMethod("ThrowSword", Private).Invoke(hook, null);
        }

        [UnityTest]
        public IEnumerator ThrowPullAndReturn_UpdatesTrailAndRaisesOneKick()
        {
            for (var i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            Assert.That(motor.IsGrounded, Is.True);

            bool sawThrown = false;
            bool sawPulling = false;
            hook.StateChanged += state =>
            {
                if (state == HookState.Thrown) sawThrown = trail.emitting;
                if (state == HookState.Pulling) sawPulling = true;
            };

            ThrowAt(anchor.GetComponent<HookAnchor>());
            Assert.That(trail.emitting, Is.True, "Thrown durumunda iz açık olmalı");
            for (var i = 0; i < 100 && hook.State != HookState.InHand; i++) yield return new WaitForFixedUpdate();

            Assert.That(sawThrown, Is.True);
            Assert.That(sawPulling, Is.True, "kanca çekiş durumuna geçmedi");
            Assert.That(kickCount, Is.EqualTo(1), "çekiş itkisi bir kez yükseltilmeli");
            Assert.That(hook.State, Is.EqualTo(HookState.InHand));
            Assert.That(trail.emitting, Is.False, "InHand durumunda iz kapalı olmalı");
        }
    }
}
