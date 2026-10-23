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
        GameObject floor, wall, player, hookVisual, cameraObject;
        PlayerInputReader input;
        GrappleHook hook;
        HookFeedback feedback;
        TrailRenderer trail;
        Vector2Event kickEvent;
        int kickCount;

        const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
        static void SetField(object target, string name, object value) => target.GetType().GetField(name, Private).SetValue(target, value);

        [SetUp]
        public void Build()
        {
            floor = new GameObject("Floor") { layer = LayerMask.NameToLayer("Ground") };
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 1f);
            wall = new GameObject("GroundWall") { layer = LayerMask.NameToLayer("Ground") };
            wall.transform.position = new Vector3(6f, 4f, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(1f, 8f);

            cameraObject = new GameObject("Camera"); cameraObject.transform.position = new Vector3(0f, 0f, -10f);
            var cam = cameraObject.AddComponent<Camera>(); cam.orthographic = true;
            hookVisual = new GameObject("GrappleHookVisual");
            kickEvent = ScriptableObject.CreateInstance<Vector2Event>(); kickEvent.Register(_ => kickCount++);
            player = new GameObject("Player") { layer = LayerMask.NameToLayer("Player") };
            player.SetActive(false); player.transform.position = new Vector3(0f, 1f, 0f);
            player.AddComponent<Rigidbody2D>(); player.AddComponent<CapsuleCollider2D>().size = new Vector2(0.8f, 1.8f);
            input = player.AddComponent<PlayerInputReader>();
            var motor = player.AddComponent<PlayerMotor>(); hook = player.AddComponent<GrappleHook>(); feedback = player.AddComponent<HookFeedback>();
            SetField(motor, "tuning", ScriptableObject.CreateInstance<PlayerMovementTuning>()); SetField(motor, "input", input);
            SetField(hook, "tuning", ScriptableObject.CreateInstance<GrappleHookTuning>()); SetField(hook, "input", input);
            SetField(hook, "body", motor); SetField(hook, "hookVisual", hookVisual.transform); SetField(hook, "aimCamera", cam);
            SetField(feedback, "hook", hook); SetField(feedback, "hookVisual", hookVisual.transform); SetField(feedback, "kickEvent", kickEvent);
            player.SetActive(true); trail = feedback.GetComponentInChildren<TrailRenderer>();
        }

        [TearDown]
        public void Clean()
        {
            foreach (var go in new[] { player, hookVisual, wall, floor, cameraObject }) Object.Destroy(go);
            if (kickEvent != null) Object.Destroy(kickEvent);
        }

        [UnityTest]
        public IEnumerator SurfacePull_UpdatesTrailAndRaisesOneKick()
        {
            for (var i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            var screen = (Vector2)cameraObject.GetComponent<Camera>().WorldToScreenPoint(new Vector2(12f, 1f));
            SetField(input, "<PointerScreen>k__BackingField", screen);
            bool sawThrown = false;
            hook.StateChanged += state => { if (state == HookState.Thrown) sawThrown = trail.emitting; };
            typeof(GrappleHook).GetMethod("FireHook", Private).Invoke(hook, null);
            for (var i = 0; i < 100 && hook.State != HookState.InHand; i++) yield return new WaitForFixedUpdate();
            Assert.That(sawThrown, Is.True, "Thrown durumunda iz açık olmalı");
            Assert.That(kickCount, Is.EqualTo(1), "yüzey çekiş itkisi bir kez yükseltilmeli");
            Assert.That(hook.State, Is.EqualTo(HookState.InHand));
            Assert.That(trail.emitting, Is.False, "InHand durumunda iz kapalı olmalı");
        }
    }
}
