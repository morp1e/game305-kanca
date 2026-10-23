using System.Collections;
using System.Reflection;
using Game.Input;
using Game.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Game.Tests
{
    // Oyuncu gövdesini gerçek fizik adımlarıyla çalıştırır. Derleme kontrolünün göremediği
    // "zemine basıyor mu, içinden geçiyor mu" sorularını cevaplar.
    public class PlayerPhysicsTests
    {
        GameObject floor, player;
        PlayerMotor motor;

        static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        [SetUp]
        public void Build()
        {
            floor = new GameObject("Floor") { layer = LayerMask.NameToLayer("Ground") };
            floor.transform.position = new Vector3(0f, -0.5f, 0f);
            floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 1f);

            player = new GameObject("Player") { layer = LayerMask.NameToLayer("Player") };
            player.SetActive(false);
            player.transform.position = new Vector3(0f, 3f, 0f);
            var rb = player.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            player.AddComponent<CapsuleCollider2D>().size = new Vector2(0.8f, 1.8f);
            var input = player.AddComponent<PlayerInputReader>();
            motor = player.AddComponent<PlayerMotor>();
            SetField(motor, "tuning", ScriptableObject.CreateInstance<PlayerMovementTuning>());
            SetField(motor, "input", input);
            player.SetActive(true);
        }

        [TearDown]
        public void Clean()
        {
            Object.Destroy(player);
            Object.Destroy(floor);
        }

        [UnityTest]
        public IEnumerator FallsAndRestsOnFloor()
        {
            for (int i = 0; i < 300; i++)
            {
                yield return new WaitForFixedUpdate();
                if (i % 50 == 0)
                    Debug.Log($"[PhysicsTest] adim {i} y={player.transform.position.y:F3} vy={motor.Velocity.y:F2} grounded={motor.IsGrounded}");
            }

            // Zeminin üstü y=0, kapsülün yarı boyu 0.9.
            Assert.That(player.transform.position.y, Is.EqualTo(0.9f).Within(0.05f), "oyuncu zemine oturmadı");
            Assert.That(motor.IsGrounded, Is.True, "zemindeyken IsGrounded false");
        }

        [UnityTest]
        public IEnumerator FastFallDoesNotTunnelThroughFloor()
        {
            motor.Velocity = new Vector2(0f, -60f);
            for (int i = 0; i < 60; i++) yield return new WaitForFixedUpdate();
            Assert.That(player.transform.position.y, Is.GreaterThan(0.8f), "oyuncu zeminin içinden geçti");
        }

        [UnityTest]
        public IEnumerator ExternalVelocityMovesBodyHorizontally()
        {
            for (int i = 0; i < 60; i++) yield return new WaitForFixedUpdate();
            motor.ExternalControl = true;
            motor.Velocity = new Vector2(10f, 0f);
            float startX = player.transform.position.x;
            for (int i = 0; i < 25; i++) yield return new WaitForFixedUpdate();
            Assert.That(player.transform.position.x - startX, Is.EqualTo(5f).Within(0.5f));
        }

        [UnityTest]
        public IEnumerator CelesteJumpHeightMatchesConvertedJumpSpeedAndGravity()
        {
            for (int i = 0; i < 100; i++) yield return new WaitForFixedUpdate();
            float start = player.transform.position.y;
            var input = player.GetComponent<PlayerInputReader>();
            input.SetJumpHeldOverride(true);
            input.TriggerJumpForTest();
            float peak = start;
            for (int i = 0; i < 100; i++)
            {
                yield return new WaitForFixedUpdate();
                peak = Mathf.Max(peak, player.transform.position.y);
            }
            float height = peak - start;
            float heldVarJumpReference = 13.125f * 0.2f +
                (13.125f * 13.125f - 5f * 5f) / (2f * 112.5f) +
                (5f * 5f) / 112.5f;
            Debug.Log($"[CelesteJump] observed={height:F3} tiles; held-var-jump reference={heldVarJumpReference:F3} tiles");
            Assert.That(height, Is.InRange(3.2f, 3.9f));
        }

        [UnityTest]
        public IEnumerator ReleasingJumpEarlyProducesLowerJump()
        {
            for (int i = 0; i < 100; i++) yield return new WaitForFixedUpdate();
            var input = player.GetComponent<PlayerInputReader>();
            float start = player.transform.position.y;
            input.SetJumpHeldOverride(true);
            input.TriggerJumpForTest();
            float heldPeak = start;
            for (int i = 0; i < 100; i++)
            {
                yield return new WaitForFixedUpdate();
                heldPeak = Mathf.Max(heldPeak, player.transform.position.y);
            }
            player.transform.position = new Vector3(0f, 0.9f, 0f);
            motor.Velocity = Vector2.zero;
            for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            start = player.transform.position.y;
            input.SetJumpHeldOverride(false);
            input.TriggerJumpForTest();
            float releasedPeak = start;
            for (int i = 0; i < 100; i++)
            {
                yield return new WaitForFixedUpdate();
                releasedPeak = Mathf.Max(releasedPeak, player.transform.position.y);
            }
            Assert.That(heldPeak - start, Is.GreaterThan(releasedPeak - start + 0.05f));
        }

        [UnityTest]
        public IEnumerator WallJumpPushesAwayAndUp()
        {
            var wall = new GameObject("Wall") { layer = LayerMask.NameToLayer("Ground") };
            wall.transform.position = new Vector3(1.1f, 2f, 0f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 5f);
            player.transform.position = new Vector3(0f, 2f, 0f);
            motor.Velocity = new Vector2(0f, -1f);
            var input = player.GetComponent<PlayerInputReader>();
            input.SetMoveXOverride(1f);
            for (int i = 0; i < 2; i++) yield return new WaitForFixedUpdate();
            Debug.Log($"[WallJump] before press position={player.transform.position} velocity={motor.Velocity} grounded={motor.IsGrounded}");
            Assert.IsFalse(motor.IsGrounded, "wall jump case must start the press in the air");
            input.TriggerJumpForTest();
            input.SetJumpHeldOverride(true);
            yield return new WaitForFixedUpdate();
            Debug.Log($"[WallJump] after press position={player.transform.position} velocity={motor.Velocity} grounded={motor.IsGrounded}");
            Assert.That(motor.Velocity.x, Is.LessThan(0f));
            Assert.That(motor.Velocity.y, Is.GreaterThan(0f));
            Object.Destroy(wall);
        }

        [UnityTest]
        public IEnumerator CoyoteJumpWorksPointZeroEightSecondsAfterWalkingOffLedge()
        {
            floor.GetComponent<BoxCollider2D>().size = new Vector2(2f, 1f);
            for (int i = 0; i < 80; i++) yield return new WaitForFixedUpdate();
            var input = player.GetComponent<PlayerInputReader>();
            input.SetMoveXOverride(1f);
            for (int i = 0; i < 40 && motor.IsGrounded; i++) yield return new WaitForFixedUpdate();
            Assert.IsFalse(motor.IsGrounded, "player should have walked off the ledge");
            // 50 Hz: the off-edge tick (0.02 s), two waits (0.04 s), and consume tick (0.02 s)
            // total 0.08 s, within Celeste's 0.1 s grace period without extending it.
            for (int i = 0; i < 2; i++) yield return new WaitForFixedUpdate();
            Debug.Log($"[Coyote] press point position={player.transform.position} velocity={motor.Velocity} grounded={motor.IsGrounded}");
            input.TriggerJumpForTest();
            yield return new WaitForFixedUpdate();
            Debug.Log($"[Coyote] after press velocity={motor.Velocity} grounded={motor.IsGrounded}");
            Assert.That(motor.Velocity.y, Is.GreaterThan(0f), "coyote jump should fire within 0.08 s");
        }
    }
}
