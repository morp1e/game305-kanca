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
    }
}
